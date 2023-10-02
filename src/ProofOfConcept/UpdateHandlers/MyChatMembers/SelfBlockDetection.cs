using MessageMediator.ProofOfConcept.Entities;
using MessageMediator.ProofOfConcept.Persistance;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramUpdater.FilterAttributes.Attributes;
using TelegramUpdater.Filters;
using TelegramUpdater.UpdateContainer;
using TelegramUpdater.UpdateHandlers.Scoped;
using TelegramUpdater.UpdateHandlers.Scoped.ReadyToUse;

namespace MessageMediator.ProofOfConcept.UpdateHandlers.MyChatMembers;

[Order(0)]
public sealed class SelfBlockDetection : MyChatMemberHandler
{
    private readonly BotDbContext _context;

    public SelfBlockDetection(BotDbContext context) => _context = context;

    protected async override Task HandleAsync(IContainer<ChatMemberUpdated> cntr)
    {
        var chat = await _context.LocalChats.FindAsync(cntr.Update.Chat.Id);
        if (chat == null)
        {
            if (cntr.Update.Chat.Type is not ChatType.Group or ChatType.Supergroup or ChatType.Channel)
                StopPropagation();
            var localChat = new LocalChat(cntr.Update.Chat);
            await _context.LocalChats.AddAsync(localChat);
            await _context.SaveChangesAsync();
            StopPropagation();
        }

        if (cntr.Update.NewChatMember.Status == ChatMemberStatus.Member)
        {
            chat!.IsSelfBlocked = true;
            await UserBlock(chat, true);
        }
        else if (cntr.Update.NewChatMember.Status is ChatMemberStatus.Left or ChatMemberStatus.Kicked)
        {
            chat!.IsSelfBlocked = false;
            await UserBlock(chat, false);
        }

        await _context.SaveChangesAsync();
    }

    private async Task UserBlock(LocalChat chat, bool doBlock)
    {
        if (chat.ChatType != ChatType.Private)
            return;
        var user = await _context.LocalUsers.FindAsync(chat.Id);
        user!.IsSelfBlocked = doBlock;
    }
}