using MessageMediator.ProofOfConcept.Configuration;
using MessageMediator.ProofOfConcept.Entities;
using MessageMediator.ProofOfConcept.Enums;
using MessageMediator.ProofOfConcept.Extensions;
using MessageMediator.ProofOfConcept.Persistance;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramUpdater.FilterAttributes.Attributes;
using TelegramUpdater.Filters;
using TelegramUpdater.Helpers;
using TelegramUpdater.UpdateContainer;
using TelegramUpdater.UpdateHandlers.Scoped;
using TelegramUpdater.UpdateHandlers.Scoped.ReadyToUse;

namespace MessageMediator.ProofOfConcept.UpdateHandlers.Messages;

[Order(100)]
[Command('/', ArgumentsMode.Idc, "start")]
[ChatType(ChatTypeFlags.Private)]
public sealed class Authorization : MessageHandler
{
    private readonly BotConfiguration _conf;
    private readonly BotDbContext _context;

    public Authorization(BotDbContext context, IOptions<BotConfiguration> options)
    {
        _context = context;
        _conf = options.Value;
    }

    protected override async Task HandleAsync(IContainer<Message> cntr)
    {
        if (cntr.TryParseCommandArgs(out string? args) &&
            string.IsNullOrWhiteSpace(args) &&
            _context.Invitations.SingleOrDefault(
                i => i.Code == args
                     && i.RedeemAt == null) is Invitation invitation)
        {
            LocalUser user = await _context.GetOrCreateLocalUserAsync(cntr.Sender()!);
            invitation.Redeemed();
            switch (invitation.Target)
            {
                case InvitationTarget.SourceRole:
                    LocalChat sourceChat = await _context.GetOrCreateLocalChatAsync(cntr.Update.Chat);
                    await _context.Entry(sourceChat).Collection(lc => lc.SourcingFor!).LoadAsync();
                    await _context.Entry(invitation).Reference(i => i.Trigger.Source).LoadAsync();
                    if (sourceChat.SourcingFor!.All(s => s.Id != invitation.Trigger.SourceId))
                    {
                        sourceChat.SourcingFor!.Add(invitation.Trigger.Source);
                        _context.LocalChats.Add(sourceChat);
                    }

                    break;
                case InvitationTarget.WorkerRole:
                    LocalChat workerChat = await _context.GetOrCreateLocalChatAsync(cntr.Update.Chat);
                    if (_context.Workers.SingleOrDefault(
                            w => w.TriggerId == invitation.TriggerId &&
                                 w.ChatId == workerChat.Id) == null)
                    {
                        Worker worker = new Worker
                        {
                            TriggerId = invitation.TriggerId, Chat = workerChat, Name = invitation.NewName
                        };
                        _context.Workers.Add(worker);
                    }

                    break;
                case InvitationTarget.SupervisorRole:
                    LocalChat supervisorChat = await _context.GetOrCreateLocalChatAsync(cntr.Update.Chat);
                    if (_context.Supervisors.SingleOrDefault(
                            s => s.TriggerId == invitation.TriggerId &&
                                 s.ChatId == supervisorChat.Id) == null)
                    {
                        Supervisor supervisor = new Supervisor
                        {
                            TriggerId = invitation.TriggerId, Chat = supervisorChat, Name = invitation.NewName
                        };
                        _context.Supervisors.Add(supervisor);
                    }

                    break;
            }

            await _context.SaveChangesAsync();
        }
        else
        {
            if (_context.LocalUsers.Find(cntr.SenderId()!) == null)
            {
                LocalUser user = await _context.GetOrCreateLocalUserAsync(cntr.Sender()!);
                await cntr.Delete();
                await SendTextMessageAsync("<tg-spoiler>Мы любим любопытных</tg-spoiler>", parseMode: ParseMode.Html);
                foreach (long adminId in _conf.Administrators)
                {
                    await cntr.BotClient.SendTextMessageAsync(adminId,
                        $"Пользователь <a href=\"tg://user?id={user.Id}\"><u>{user.Name}</u> ({user.Id})</a> проявил(а) любопытство",
                        parseMode: ParseMode.Html);
                }
            }
            else
            {
                await cntr.Delete();
            }
        }

        StopPropagation();
    }
}