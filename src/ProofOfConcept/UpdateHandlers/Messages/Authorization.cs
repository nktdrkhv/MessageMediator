using MessageMediator.ProofOfConcept.Entities;
using MessageMediator.ProofOfConcept.Extensions;
using MessageMediator.ProofOfConcept.Persistance;
using MessageMediator.ProofOfConcept.Enums;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramUpdater.FilterAttributes.Attributes;
using TelegramUpdater.Filters;
using TelegramUpdater.Helpers;
using TelegramUpdater.UpdateContainer;
using TelegramUpdater.UpdateHandlers.Scoped;
using TelegramUpdater.UpdateHandlers.Scoped.ReadyToUse;

namespace MessageMediator.ProofOfConcept.UpdateHandlers.Messages;

[Order(100), Command(prefix: '/', argumentsMode: ArgumentsMode.Idc, "start"), ChatType(ChatTypeFlags.Private)]
public sealed class Authorization : MessageHandler
{
    private readonly BotDbContext _context;

    public Authorization(BotDbContext context) => _context = context;

    protected override async Task HandleAsync(IContainer<Message> cntr)
    {
        if (cntr.TryParseCommandArgs(out string? args) &&
            args != null &&
            _context.Invitations
                .Where(i => i.Code == args && i.RedeemAt == null)
                .SingleOrDefault() is Invitation invitation)
        {
            var user = await _context.GetOrCreateLocalUserAsync(cntr.Sender()!);
            invitation.RedeemAt = DateTime.UtcNow;
            switch (invitation.Target)
            {
                case InvitationTarget.SourceRole:
                    var sourceChat = await _context.GetOrCreateLocalChatAsync(cntr.Update.Chat);
                    await _context.Entry(sourceChat)
                        .Collection(lc => lc.SourcingFor!)
                        .LoadAsync();
                    await _context.Entry(invitation)
                        .Reference(i => i.Trigger.Source)
                        .LoadAsync();
                    if (!sourceChat.SourcingFor!.Any(s => s.Id == invitation.Trigger.SourceId))
                    {
                        sourceChat.SourcingFor!.Add(invitation.Trigger.Source);
                        _context.LocalChats.Add(sourceChat);
                    }
                    break;
                case InvitationTarget.WorkerRole:
                    var workerChat = await _context.GetOrCreateLocalChatAsync(cntr.Update.Chat);
                    if (_context.Workers
                            .Where(w =>
                                w.TriggerId == invitation.TriggerId &&
                                w.ChatId == workerChat.Id)
                            .SingleOrDefault() == null)
                    {
                        var worker = new Worker()
                        {
                            TriggerId = invitation.TriggerId,
                            Chat = workerChat,
                            Alias = invitation.NewAlias
                        };
                        _context.Workers.Add(worker);
                    }
                    break;
                case InvitationTarget.SupervisorRole:
                    var supervisorChat = await _context.GetOrCreateLocalChatAsync(cntr.Update.Chat);
                    if (_context.Supervisors
                            .Where(s =>
                                s.TriggerId == invitation.TriggerId &&
                                s.ChatId == supervisorChat.Id)
                            .SingleOrDefault() == null)
                    {
                        var supervisor = new Supervisor()
                        {
                            TriggerId = invitation.TriggerId,
                            Chat = supervisorChat,
                            Alias = invitation.NewAlias
                        };
                        _context.Supervisors.Add(supervisor);
                    }
                    break;
            }
            await _context.SaveChangesAsync();
        }
        else
        {
            await _context.GetOrCreateLocalUserAsync(cntr.Sender()!);
            await cntr.ResponseAsync("Мы любим любопытных");
        }
        StopPropagation();
    }
}