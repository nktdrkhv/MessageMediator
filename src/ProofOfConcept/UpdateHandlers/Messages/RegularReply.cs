using MessageMediator.ProofOfConcept.Configuration;
using MessageMediator.ProofOfConcept.Entities;
using MessageMediator.ProofOfConcept.Enums;
using MessageMediator.ProofOfConcept.Extensions;
using MessageMediator.ProofOfConcept.Persistance;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramUpdater.FilterAttributes.Attributes;
using TelegramUpdater.UpdateContainer;
using TelegramUpdater.UpdateHandlers.Scoped;
using TelegramUpdater.UpdateHandlers.Scoped.ReadyToUse;

namespace MessageMediator.ProofOfConcept.UpdateHandlers.Messages;

[Order(101), Replied]
public sealed class RegularReply : MessageHandler
{
    private readonly BotConfiguration _configuration;
    private readonly BotDbContext _context;

    public RegularReply(BotDbContext context, IOptions<BotConfiguration> configuration)
    {
        _context = context;
        _configuration = configuration.Value;
    }

    protected async override Task HandleAsync(IContainer<Message> cntr)
    {
        var repliedTo = cntr.Update.ReplyToMessage!;
        if (repliedTo.From!.IsBot && repliedTo.From.Username == _configuration.BotName &&
            _context.ChainLinks
            .Where(cl => cl.ForwardMessage.TelegramMessageId == repliedTo.MessageId)
            .Where(cl => cl.ForwardMessage.ChatId == repliedTo.Chat.Id)
            .Where(cl => cl.MotherChain.FinishedAt == null)
            .IncludeMainChainParts()
            .SingleOrDefault() is ChainLink forwardedLink)
        {
            var local = await RetrieveLocalMessageCollection(null, repliedTo);
            if (forwardedLink.IsBelongTo(cntr.SenderId()!.Value))
            {
                //var replyTo = forwardedLink
                var messages = await cntr.BotClient.SendMessageDataAsync(
                    chatId: forwardedLink.RecievedMessage.ChatId,
                    replyTo: forwardedLink.RecievedMessage.TelegramMessageId,
                    customText: null,
                    local.Select(l => l.Data).ToArray(),
                    markup: MakeControlButtons(forwardedLink, true),
                    protect: true);
                var messageByLocal = messages.Zip(local);
                var links = messageByLocal.Select((mbl, _) => new ChainLink()
                {
                    MotherChain = forwardedLink.MotherChain,
                    RecievedMessage = mbl.Second,
                    ForwardMessage = new LocalMessage(null, mbl.First)
                });
                await _context.ChainLinks.AddRangeAsync(links);
                await _context.SaveChangesAsync();
            }
        }
        else if (_context.ChainLinks
            .Where(cl => cl.RecievedMessage.TelegramMessageId == repliedTo.MessageId)
            .Where(cl => cl.RecievedMessage.ChatId == cntr.Update.Chat.Id)
            .Where(cl => cl.MotherChain.FinishedAt == null)
            .IncludeMainChainParts()
            .SingleOrDefault() is ChainLink recievedLink)
        {
            var local = await RetrieveLocalMessageCollection(null, repliedTo);
            await cntr.ResponseAsync("Возможность дополнять задания появится в следующей версии 🐶");
        }

        StopPropagation();
    }

    private async ValueTask<ICollection<LocalMessage>> RetrieveLocalMessageCollection(string? text, Message message)
    {
        if (message.MediaGroupId is string mediaGroup)
        {
            List<Message> messageGroup = new() { message };
            while (await AwaitMessageAsync(
                filter: null,
                timeOut: TimeSpan.FromSeconds(1)) is IContainer<Message> msgCntr &&
                        mediaGroup.Equals(msgCntr.Update.MediaGroupId))
                messageGroup.Add(msgCntr.Update);
            return LocalMessage.FromSet(text, messageGroup.OrderBy(m => m.MessageId).ToArray());
        }
        else
            return new[] { new LocalMessage(text, message) };
    }

    // todo: from recieved to forward
    private IReplyMarkup MakeControlButtons(ChainLink link, bool fromForwardToRecieved)
    {
        // approve - reject
        // accept - decline
        if (fromForwardToRecieved && link.RoleOfSender == TrineRole.Source)
            return new InlineKeyboardMarkup(new[] {
                InlineKeyboardButton.WithCallbackData("✅", $"accept:{link.MotherChainId}"),
                InlineKeyboardButton.WithCallbackData("⛔️", $"decline:{link.MotherChainId}"),
            });
        else if (fromForwardToRecieved && link.RoleOfSender == TrineRole.Supervisor)
            return null;
        return null;
    }
}