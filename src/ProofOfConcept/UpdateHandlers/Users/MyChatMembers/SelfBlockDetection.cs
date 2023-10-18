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
    private readonly BotDbContext _context;
    private readonly BotConfiguration _conf;

    public SelfBlockDetection(BotDbContext context, IOptions<BotConfiguration> options)
    {
        _context = context;
        _conf = options.Value;
    }

    protected async override Task HandleAsync(IContainer<ChatMemberUpdated> cntr)
    {
        var chat = await _context.LocalChats.FindAsync(cntr.Update.Chat.Id);
        if (chat == null &&
            cntr.Update.NewChatMember.Status == ChatMemberStatus.Member &&
            cntr.Update.Chat.Type is ChatType.Group or ChatType.Supergroup or ChatType.Channel)
        {
            var localChat = new LocalChat(cntr.Update.Chat);
            await _context.LocalChats.AddAsync(localChat);
            await _context.SaveChangesAsync();

            foreach (var adminId in _conf.Administrators)
                await cntr.BotClient.SendChatRegistrationNotificationAsync(adminId, localChat);

            StopPropagation();
        }
        else if (chat == null)
            StopPropagation();

        if (cntr.Update.NewChatMember.Status == ChatMemberStatus.Member)
        {
            chat!.IsSelfBlocked = false;
            await UserBlock(chat, false);

            foreach (var adminId in _conf.Administrators)
                await cntr.BotClient.SendChatUnblockedNotificationAsync(adminId, chat);
        }
        else if (cntr.Update.NewChatMember.Status is ChatMemberStatus.Left or ChatMemberStatus.Kicked)
        {
            chat!.IsSelfBlocked = true;
            await UserBlock(chat, true);

            foreach (var adminId in _conf.Administrators)
                await cntr.BotClient.SendChatBlockedNotificationAsync(adminId, chat);
        }

        await _context.SaveChangesAsync();
    }

    private async ValueTask UserBlock(LocalChat chat, bool doBlock)
    {
        if (chat.ChatType != ChatType.Private)
            return;
        var user = await _context.LocalUsers.FindAsync(chat.Id);
        user!.IsSelfBlocked = doBlock;
    }
}