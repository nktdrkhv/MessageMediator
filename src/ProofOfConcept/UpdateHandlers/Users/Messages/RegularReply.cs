using MessageMediator.ProofOfConcept.Dto;
using MessageMediator.ProofOfConcept.Entities;
using MessageMediator.ProofOfConcept.Enums;
using MessageMediator.ProofOfConcept.Extensions;
using MessageMediator.ProofOfConcept.Persistance;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
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
        ChainLink? chainLink = null;

        try
        {
            chainLink = _context.ChainLinks
                .WhereLinkFits(repliedTo, true)
                .IncludeChainLinkParts()
                .SingleOrDefault();
        }
        catch (InvalidOperationException)
        {
            await cntr.ResponseAsync("<i>Получатель неопределён</i>", parseMode: ParseMode.Html);
            StopPropagation();
        }

        if (chainLink == null)
            StopPropagation();

        var roleOfSender = chainLink!.RoleOf(cntr.Update.Chat.Id);
        var address = chainLink!.TwinOf(repliedTo.MessageId);
        var addresseeRole = chainLink.RoleOf(address);

        switch (roleOfSender)
        {
            case TrineRole.Source:
                if (addresseeRole is TrineRole.Worker)
                {
                    await CleanOldMarkups(cntr, chainLink.MotherChain, address.ChatId);
                    var reply = new RepliedMessage
                    {
                        ReferenceLink = chainLink,
                        DestinationMessage = address,
                        ReplyItself = recieved,
                        Markup = InlineKeyboardMarkupWrapper.OnlyQuestionControls(chainLink.MotherChainId)
                    };
                    await _context.ChainLinks.AddRangeAsync(await ForwardReplyMessage(cntr, reply));
                    await _context.SaveChangesAsync();
                }
                else if (addresseeRole is TrineRole.Supervisor)
                {
                    // sending to a supervisor first
                    await CleanOldMarkups(cntr, chainLink.MotherChain, address.ChatId);
                    var customTextToSupervisor = recieved.First().Data.Text is string sourceText
                        ? $"<i>Источник</i>\n\n{sourceText}".Trim()
                        : "<i>Источник</i>";
                    var replyToSupervisor = new RepliedMessage
                    {
                        ReferenceLink = chainLink,
                        DestinationMessage = address,
                        CustomText = customTextToSupervisor,
                        ReplyItself = recieved,
                        Markup = InlineKeyboardMarkupWrapper.FullSupervisorControls(chainLink.MotherChainId)
                    };
                    var supervisorLinks = await ForwardReplyMessage(cntr, replyToSupervisor);
                    await _context.ChainLinks.AddRangeAsync(supervisorLinks);

                    // resending to a worker then
                    var workerChatId = chainLink.MotherChain.Worker!.ChatId;
                    var workerDestination = address.ReferenceTo; // todo: explain explicitly 
                    await CleanOldMarkups(cntr, chainLink.MotherChain, workerChatId);
                    var replyToWorker = new RepliedMessage
                    {
                        ReferenceLinks = supervisorLinks,
                        DestinationMessage = workerDestination!,
                        ReplyItself = supervisorLinks.Select(sl => sl.ForwardedMessage),
                        Markup = InlineKeyboardMarkupWrapper.OnlyQuestionControls(chainLink.MotherChainId)
                    };
                    await _context.ChainLinks.AddRangeAsync(await ForwardReplyMessage(cntr, replyToWorker));
                    await _context.SaveChangesAsync();
                }

                break;
            case TrineRole.Worker:
                var workerMarkup = chainLink.Mode is ChainLinkMode.Normal ? addresseeRole switch
                {
                    TrineRole.Source => InlineKeyboardMarkupWrapper.ReplyToSource(chainLink!.MotherChainId),
                    TrineRole.Supervisor => InlineKeyboardMarkupWrapper.ReplyToSupervisor(chainLink!.MotherChainId),
                    _ => null
                } : null;
                var workerCustomText = addresseeRole is TrineRole.Supervisor
                    ? recieved.First().Data.Text is string workerText
                        ? $"<i>Исполнитель</i>\n\n{workerText}".Trim()
                        : "<i>Исполнитель</i>"
                    : null;
                var workerReply = new RepliedMessage
                {
                    ReferenceLink = chainLink,
                    DestinationMessage = address,
                    ReplyItself = recieved,
                    CustomText = workerCustomText,
                    Markup = workerMarkup,
                };
                await _context.ChainLinks.AddRangeAsync(await ForwardReplyMessage(cntr, workerReply));
                await _context.SaveChangesAsync();
                break;
            case TrineRole.Supervisor:
                IReplyMarkup? supervisorMarkup = null;
                if (addresseeRole is TrineRole.Worker)
                {
                    supervisorMarkup = InlineKeyboardMarkupWrapper.OnlyQuestionControls(chainLink.MotherChainId);
                    await CleanOldMarkups(cntr, chainLink.MotherChain, address.ChatId);
                }
                var supervisorReply = new RepliedMessage
                {
                    ReferenceLink = chainLink,
                    DestinationMessage = address,
                    ReplyItself = recieved,
                    Markup = supervisorMarkup,
                };
                await _context.ChainLinks.AddRangeAsync(await ForwardReplyMessage(cntr, supervisorReply));
                await _context.SaveChangesAsync();
                break;
        }
        StopPropagation();
    }

    private async ValueTask<ICollection<LocalMessage>> RetrieveLocalMessageCollection(string? customText, Message firstMessage)
    {
        if (firstMessage.MediaGroupId is string mediaGroup)
        {
            List<Message> messageGroup = new() { firstMessage };
            while (await AwaitMessageAsync(
                       filter: null,
                       timeOut: TimeSpan.FromSeconds(1)) is IContainer<Message> msgCntr &&
                   mediaGroup.Equals(msgCntr.Update.MediaGroupId))
                messageGroup.Add(msgCntr.Update);
            return LocalMessage.FromSet(customText, messageGroup.OrderBy(m => m.MessageId).ToArray());
        }
        else
            return new[] { new LocalMessage(customText, firstMessage) };
    }

    private async ValueTask CleanOldMarkups(IUpdateContainer cntr, Chain chain, long chatId)
    {
        var relativeMessages =
            (from link in _context.ChainLinks.Include(cl => cl.ForwardedMessage)
             where link.MotherChainId == chain.Id
             where link.ForwardedMessage.ChatId == chatId
             where link.ForwardedMessage.ActiveMarkup
             select link.ForwardedMessage).ToArray();
        for (int i = 0; i < relativeMessages.Length; i++)
        {
            try
            {
                await cntr.BotClient.EditMessageReplyMarkupAsync(
                    chatId: relativeMessages[i].ChatId,
                    messageId: relativeMessages[i].TelegramMessageId,
                    replyMarkup: InlineKeyboardMarkup.Empty()
                );
                relativeMessages[i].ActiveMarkup = false;
            }
            catch { }
        }
        await _context.SaveChangesAsync();
    }

    public static async ValueTask<IEnumerable<ChainLink>> ForwardReplyMessage(IUpdateContainer cntr, RepliedMessage repliedMessage)
    {
        // todo: sould BotClient.ForwardMessageAsync be better for Source and Supervisor messages?
        var sentMessages = await cntr.BotClient.SendMessageDataAsync(
            chatId: repliedMessage.DestinationMessage.ChatId,
            replyTo: repliedMessage.DestinationMessage.TelegramMessageId,
            customText: repliedMessage.CustomText,
            dataCollection: repliedMessage.ReplyItself.OrderBy(lm => lm.TelegramMessageId).Select(lm => lm.Data),
            markup: repliedMessage.Markup,
            protect: repliedMessage.Protect);

        return sentMessages.Zip(repliedMessage.ReplyItself).Select((m, i) => new ChainLink(
            motherChain: repliedMessage.ReferenceLinks?.ElementAt(i).MotherChain ?? repliedMessage.ReferenceLink!.MotherChain,
            recievedMessage: m.Second,
            forwardedMessage: new LocalMessage(m.First),
            repliedMessage: repliedMessage.DestinationMessage,
            referenceLink: repliedMessage.ReferenceLinks?.ElementAt(i) ?? repliedMessage.ReferenceLink!
        ));
    }
}