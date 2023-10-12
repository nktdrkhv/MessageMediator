using MessageMediator.ProofOfConcept.Entities;
using MessageMediator.ProofOfConcept.Enums;
using MessageMediator.ProofOfConcept.Extensions;
using MessageMediator.ProofOfConcept.Persistance;
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
    private readonly BotDbContext _context;

    public RegularReply(BotDbContext context) => _context = context;

    protected async override Task HandleAsync(IContainer<Message> cntr)
    {
        var repliedTo = cntr.Update.ReplyToMessage!;
        var recieved = await RetrieveLocalMessageCollection(null, repliedTo);

        var chainLink = _context.ChainLinks
                .Where(cl =>
                        (cl.ForwardedMessage.TelegramMessageId == repliedTo.MessageId && cl.ForwardedMessage.ChatId == repliedTo.Chat.Id) ||
                        (cl.RecievedMessage.TelegramMessageId == repliedTo.MessageId && cl.RecievedMessage.ChatId == cntr.Update.Chat.Id))
                .Where(cl => cl.MotherChain.FinishedAt == null)
                .IncludeChainLinkParts()
                .SingleOrDefault();
        if (chainLink == null)
            StopPropagation();

        var address = chainLink!.TwinOf(repliedTo.MessageId);
        var roleOfSender = chainLink.RoleOf(cntr.Update.Chat.Id);

        // todo: wrap into an aggregate
        long destinationChatId = address.ChatId;
        int? destinationReplyTo = address.TelegramMessageId;
        //string? customText = null;
        IReplyMarkup? markup = null;
        bool protect = true;

        // ----------- role specific -----------

        var adresseeRole = chainLink.RoleOf(address);

        switch (adresseeRole)
        {
            case TrineRole.Source:
                break;
            case TrineRole.Worker:
                break;
            case TrineRole.Supervisor:
                break;
            default:
                break;
        }

        if (roleOfSender is TrineRole.Worker)
        {
            if (chainLink.Mode is ChainLinkMode.Normal)
                markup = adresseeRole switch
                {
                    TrineRole.Source => InlineKeyboardMarkupWrapper.ReplyToSource(chainLink!.MotherChainId),
                    TrineRole.Supervisor => InlineKeyboardMarkupWrapper.ReplyToSupervisor(chainLink!.MotherChainId),
                    _ => null
                };
            await _context.ChainLinks.AddRangeAsync(await ForwardReplyMessage(cntr, chainLink, recieved, destinationChatId, destinationReplyTo, null, markup, protect));
        }

        if (roleOfSender is TrineRole.Source)
        {

            protect = false;
        }

        // -------------------------------------

        // todo: sould BotClient.ForwardMessageAsync be better for Source and Supervisor messages?
        var sentMessages = await cntr.BotClient.SendMessageDataAsync(
            chatId: destinationChatId,
            replyTo: destinationReplyTo,
            customText: null,
            recieved.Select(lm => lm.Data).ToArray(),
            markup: markup,
            protect: protect);

        var links = sentMessages.Zip(recieved).Select((m, _) => new ChainLink()
        {
            Mode = chainLink.Mode,
            MotherChain = chainLink.MotherChain,
            RecievedMessage = m.Second,
            ForwardedMessage = new LocalMessage(m.First)
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

    private async ValueTask<IEnumerable<ChainLink>> ForwardReplyMessage(IUpdateContainer cntr, ChainLink targetLink, IEnumerable<LocalMessage> reply, long chatId, int? replyTo, string? customText, IReplyMarkup? markup, bool protect = true)
    {
        var sentMessages = await cntr.BotClient.SendMessageDataAsync(
            chatId: chatId,
            replyTo: replyTo,
            customText: customText,
            dataCollection: reply.Select(lm => lm.Data).ToArray(),
            markup: markup,
            protect: protect);
        return sentMessages.Zip(reply).Select((m, _) => new ChainLink()
        {
            Mode = targetLink.Mode,
            MotherChain = targetLink.MotherChain,
            RecievedMessage = m.Second,
            ForwardedMessage = new LocalMessage(m.First)
        });
    }
}