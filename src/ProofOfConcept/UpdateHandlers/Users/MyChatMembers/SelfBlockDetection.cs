using MessageMediator.ProofOfConcept.Configuration;
using MessageMediator.ProofOfConcept.Entities;
using MessageMediator.ProofOfConcept.Extensions;
using MessageMediator.ProofOfConcept.Persistance;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramUpdater.UpdateContainer;
using TelegramUpdater.UpdateHandlers.Scoped;
using TelegramUpdater.UpdateHandlers.Scoped.ReadyToUse;

namespace MessageMediator.ProofOfConcept.UpdateHandlers.MyChatMembers;

[Order(0)]
public sealed class SelfBlockDetection : MyChatMemberHandler
{
    private readonly BotConfiguration _conf;
    private readonly BotDbContext _context;

    public SelfBlockDetection(BotDbContext context, IOptions<BotConfiguration> options)
    {
        _context = context;
        _conf = options.Value;
    }

    protected override async Task HandleAsync(IContainer<ChatMemberUpdated> cntr)
    {
        LocalChat? chat = await _context.LocalChats.FindAsync(cntr.Update.Chat.Id);
        if (chat == null &&
            cntr.Update.NewChatMember.Status == ChatMemberStatus.Member &&
            cntr.Update.Chat.Type is ChatType.Group or ChatType.Supergroup or ChatType.Channel)
        {
            LocalChat localChat = new LocalChat(cntr.Update.Chat);
            await _context.LocalChats.AddAsync(localChat);
            await _context.SaveChangesAsync();

            foreach (long adminId in _conf.Administrators)
            {
                await cntr.BotClient.SendChatRegistrationNotificationAsync(adminId, localChat);
            }

            StopPropagation();
        }
        else if (chat == null)
        {
            StopPropagation();
        }

        if (cntr.Update.NewChatMember.Status == ChatMemberStatus.Member)
        {
            chat!.IsSelfBlocked = false;
            await UserBlock(chat, false);

            foreach (long adminId in _conf.Administrators)
            {
                await cntr.BotClient.SendChatUnblockedNotificationAsync(adminId, chat);
            }
        }
        else if (cntr.Update.NewChatMember.Status is ChatMemberStatus.Left or ChatMemberStatus.Kicked)
        {
            chat!.IsSelfBlocked = true;
            await UserBlock(chat, true);

            foreach (long adminId in _conf.Administrators)
            {
                await cntr.BotClient.SendChatBlockedNotificationAsync(adminId, chat);
            }
        }

        await _context.SaveChangesAsync();
    }

    private async ValueTask UserBlock(LocalChat chat, bool doBlock)
    {
        if (chat.ChatType != ChatType.Private)
        {
            return;
        }

        LocalUser? user = await _context.LocalUsers.FindAsync(chat.Id);
        user!.IsSelfBlocked = doBlock;
    }
}