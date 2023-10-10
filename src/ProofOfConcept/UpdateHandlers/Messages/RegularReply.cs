using MessageMediator.ProofOfConcept.Configuration;
using MessageMediator.ProofOfConcept.Entities;
using MessageMediator.ProofOfConcept.Enums;
using MessageMediator.ProofOfConcept.Extensions;
using MessageMediator.ProofOfConcept.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Telegram.Bot;
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
        var recieved = await RetrieveLocalMessageCollection(null, repliedTo);

        var chainLink = _context.ChainLinks
                .Where(cl =>
                        (cl.ForwardMessage.TelegramMessageId == repliedTo.MessageId && cl.ForwardMessage.ChatId == repliedTo.Chat.Id) ||
                        (cl.RecievedMessage.TelegramMessageId == repliedTo.MessageId && cl.RecievedMessage.ChatId == cntr.Update.Chat.Id))
                .Where(cl => cl.MotherChain.FinishedAt == null)
                .IncludeMainChainParts()
                .SingleOrDefault();
        if (chainLink == null)
            StopPropagation();

        var address = chainLink!.TwinOf(repliedTo.MessageId);
        var roleOfSender = chainLink.RoleOf(cntr.Update.Chat.Id);

        IReplyMarkup? markup = null;
        bool protect = true;

        if (roleOfSender is TrineRole.Worker)
            markup = chainLink.RoleOf(address) switch
            {
                TrineRole.Source => new InlineKeyboardMarkup(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("✅", $"accept:{chainLink!.MotherChainId}"),
                        InlineKeyboardButton.WithCallbackData("⛔️", $"decline:{chainLink!.MotherChainId}"),
                    }),
                TrineRole.Supervisor => new InlineKeyboardMarkup(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("✅", $"approve:{chainLink!.MotherChainId}"),
                        InlineKeyboardButton.WithCallbackData("⛔️", $"reject:{chainLink!.MotherChainId}"),
                    }),
                _ => null
            };

        if (roleOfSender is TrineRole.Source)
            protect = false;

        // todo: sould BotClient.ForwardMessageAsync be better for Source and Supervisor messages?
        var sentMessages = await cntr.BotClient.SendMessageDataAsync(
                            chatId: address.ChatId,
                            replyTo: address.TelegramMessageId,
                            customText: null,
                            recieved.Select(l => l.Data).ToArray(),
                            markup: markup,
                            protect: protect);

        var links = sentMessages.Zip(recieved).Select((m, _) => new ChainLink()
        {
            MotherChain = chainLink.MotherChain,
            RecievedMessage = m.Second,
            ForwardMessage = new LocalMessage(m.First)
        });

        await _context.ChainLinks.AddRangeAsync(links);
        await _context.SaveChangesAsync();

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
}