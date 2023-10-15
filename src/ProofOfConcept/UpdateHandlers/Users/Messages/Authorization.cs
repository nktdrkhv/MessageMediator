using MessageMediator.ProofOfConcept.Entities;
using MessageMediator.ProofOfConcept.Extensions;
using MessageMediator.ProofOfConcept.Persistance;
using MessageMediator.ProofOfConcept.Enums;
using Telegram.Bot.Types;
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
            string.IsNullOrWhiteSpace(args) &&
            _context.Invitations.SingleOrDefault(
                i => i.Code == args
                     && i.RedeemAt == null) is Invitation invitation)
        {
            var user = await _context.GetOrCreateLocalUserAsync(cntr.Sender()!);
            invitation.Redeemed();
            switch (invitation.Target)
            {
                case InvitationTarget.SourceRole:
                    var sourceChat = await _context.GetOrCreateLocalChatAsync(cntr.Update.Chat);
                    await _context.Entry(sourceChat).Collection(lc => lc.SourcingFor!).LoadAsync();
                    await _context.Entry(invitation).Reference(i => i.Trigger.Source).LoadAsync();
                    if (sourceChat.SourcingFor!.All(s => s.Id != invitation.Trigger.SourceId))
                    {
                        sourceChat.SourcingFor!.Add(invitation.Trigger.Source);
                        _context.LocalChats.Add(sourceChat);
                    }
                    break;
                case InvitationTarget.WorkerRole:
                    var workerChat = await _context.GetOrCreateLocalChatAsync(cntr.Update.Chat);
                    if (_context.Workers.SingleOrDefault(
                            w => w.TriggerId == invitation.TriggerId &&
                                 w.ChatId == workerChat.Id) == null)
                    {
                        var worker = new Worker
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
                    if (_context.Supervisors.SingleOrDefault(
                            s => s.TriggerId == invitation.TriggerId &&
                                 s.ChatId == supervisorChat.Id) == null)
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