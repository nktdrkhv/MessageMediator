using MessageMediator.ProofOfConcept.Aggregates;
using MessageMediator.ProofOfConcept.Entities;
using MessageMediator.ProofOfConcept.Enums;
using MessageMediator.ProofOfConcept.Extensions;
using MessageMediator.ProofOfConcept.Persistance;
using MessageMediator.ProofOfConcept.UpdateHandlers.Messages;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramUpdater.UpdateContainer;
using TelegramUpdater.UpdateHandlers.Scoped;
using TelegramUpdater.UpdateHandlers.Scoped.ReadyToUse;

namespace MessageMediator.ProofOfConcept.UpdateHandlers.CallbackQueries;

[Order(100)]
public sealed class RegularReaction : CallbackQueryHandler
{
    private readonly BotDbContext _context;

    public RegularReaction(BotDbContext context) => _context = context;

    protected override async Task HandleAsync(IContainer<CallbackQuery> cntr)
    {
        var queryArgs = cntr.Update.Data?.Split(':');

        // ----------- validation -----------

        if (queryArgs == null || queryArgs.Length != 2 || cntr.Update.Message == null || !int.TryParse(queryArgs[1], out int chainId))
        {
            await cntr.AnswerAsync("Некорректные данные");
            await cntr.EditAsync(inlineKeyboardMarkup: InlineKeyboardMarkup.Empty());
            //StopPropagation();
            return;
        }

        var msgId = cntr.Update.Message!.MessageId;
        var chatId = cntr.Update.Message!.Chat.Id;
        var activeChain = await _context.Chains.FindAsync(chainId);
        if (activeChain == null)
        {
            await cntr.AnswerAsync("Информация по данной цепочке сообщений отсутсвует");
            await cntr.EditAsync(inlineKeyboardMarkup: InlineKeyboardMarkup.Empty());
            //StopPropagation();
            return;
        }

        // ----------------------------------

        // ------------ reacting ------------

        // todo: current model (e.g. accept:5 where 5 is chain's ID) is unsecure. Check rights via DecisionMakers property
        // take, question, remark, revision, accept, decline, approve, reject
        switch (queryArgs![0])
        {
            case "remark":
            case "revision":
                await _context.Chains.Entry(activeChain).Reference(c => c.Supervisor).LoadAsync();
                if (activeChain.Supervisor!.ChatId == chatId)
                {
                    await _context.Chains.Entry(activeChain).Collection(c => c.Links!)
                        .Query().AsSplitQuery()
                        .Include(cl => cl.ForwardedMessage.ReferenceTo)
                        .Include(cl => cl.RecievedMessage.ReferenceTo)
                        .LoadAsync();
                    string supervisorText = string.Empty;
                    LocalMessage refersTo = null!;
                    ChainLinkMode mode = ChainLinkMode.Normal;
                    if (queryArgs![0].Equals("remark"))
                    {
                        mode = ChainLinkMode.Remark;
                        supervisorText = "<i>Связь с исполнителем</i>";
                        refersTo = await _context.LocalMessages
                            .Include(lm => lm.ReferenceTo)
                            .FirstAsync(lm => lm.ReferenceTo!.TelegramMessageId == msgId && lm.ReferenceTo.ChatId == chatId);
                    }
                    else if (queryArgs![0].Equals("revision"))
                    {
                        mode = ChainLinkMode.Revision;
                        supervisorText = "<i>Связь с источником</i>";
                        refersTo = activeChain.Links!
                            .Where(cl => cl.ForwardedMessage.TelegramMessageId == msgId)
                            .Where(cl => cl.ForwardedMessage.ChatId == chatId)
                            .Single().RecievedMessage;
                    }
                    await cntr.AnswerAsync("Напишите вопрос, как ответ на сообщение");
                    var pleaseAskMsg = await cntr.SendAsync(
                        text: supervisorText,
                        sendAsReply: true, parseMode: ParseMode.Html, disableNotification: true, protectContent: true,
                        replyMarkup: new ForceReplyMarkup() { InputFieldPlaceholder = "Ваш вопрос...", Selective = true });
                    activeChain.Links!.Add(new ChainLink(
                        activeChain,
                        refersTo,
                        new LocalMessage(pleaseAskMsg.Update, true),
                        mode: mode,
                        hide: true
                    ));
                    await _context.SaveChangesAsync();
                    if (activeChain.Links!.All(l => l.Mode != ChainLinkMode.Remark))
                        await cntr.EditAsync(inlineKeyboardMarkup: InlineKeyboardMarkupWrapper.OnlyRemarkControls(activeChain.Id));
                    else if (activeChain.Links!.All(l => l.Mode != ChainLinkMode.Revision))
                        await cntr.EditAsync(inlineKeyboardMarkup: InlineKeyboardMarkupWrapper.OnlyRevisionControls(activeChain.Id));
                    else
                        await cntr.EditAsync(inlineKeyboardMarkup: InlineKeyboardMarkup.Empty());
                }
                break;
            case "question":
                await _context.Chains.Entry(activeChain).Reference(c => c.Worker).LoadAsync();
                if (activeChain.Worker!.ChatId == chatId)
                {
                    await _context.Chains.Entry(activeChain).Collection(c => c.Links!).LoadAsync();
                    var questionOnMsg = await _context.LocalMessages
                        .Where(lm => lm.TelegramMessageId == msgId)
                        .Where(lm => lm.ChatId == chainId)
                        .Include(lm => lm.ReferenceTo)
                        .SingleAsync();
                    await cntr.AnswerAsync("Напишите вопрос, как ответ на сообщение");
                    var pleaseAskMsg = await cntr.SendAsync(
                        text: "<i>Обратная связь</i>",
                        sendAsReply: true, parseMode: ParseMode.Html, disableNotification: true, protectContent: true,
                        replyMarkup: new ForceReplyMarkup() { InputFieldPlaceholder = "Ваш вопрос...", Selective = true });
                    activeChain.Links!.Add(new ChainLink(
                        activeChain,
                        questionOnMsg.ReferenceTo!,
                        new LocalMessage(pleaseAskMsg.Update, true),
                        mode: ChainLinkMode.Question,
                        hide: true
                    ));
                    await _context.SaveChangesAsync();
                    if (activeChain.TookAt != null)
                        await cntr.EditAsync(inlineKeyboardMarkup: InlineKeyboardMarkup.Empty());
                    else
                        await cntr.EditAsync(inlineKeyboardMarkup: InlineKeyboardMarkupWrapper.OnlyTakeControls(activeChain.Id));
                }
                break;
            case "take":
                // todo: depends on broadcastins the issue mode, this may require to delete inline markups of other workers
                await _context.Chains.Entry(activeChain).Reference(c => c.Worker).LoadAsync();
                if (activeChain.Worker!.ChatId == chatId)
                {
                    await _context.Chains.Entry(activeChain).Collection(c => c.Links!).LoadAsync();
                    activeChain.TookAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    await cntr.AnswerAsync("Взято в работу");
                    if (activeChain.Links!.Any(l => l.Mode == ChainLinkMode.Question))
                        await cntr.EditAsync(inlineKeyboardMarkup: InlineKeyboardMarkup.Empty());
                    else
                        await cntr.EditAsync(inlineKeyboardMarkup: InlineKeyboardMarkupWrapper.OnlyQuestionControls(activeChain.Id));
                }
                break;
            case "accept":
                // todo: the source might accept only one part of the responsed work. there sould be an option for a partial acception;
                if (activeChain.SourceChatId == chatId)
                {
                    await _context.Chains.Entry(activeChain).Collection(c => c.Links!)
                        .Query().AsSplitQuery()
                        .Include(cl => cl.ForwardedMessage)
                        .Include(cl => cl.RecievedMessage)
                        .LoadAsync();
                    activeChain.FinishedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    await cntr.AnswerAsync("Работа принята");
                    await cntr.EditAsync(inlineKeyboardMarkup: InlineKeyboardMarkup.Empty());
                    var acceptedMessage = activeChain.Links!.AsQueryable().WhereLinkFits(cntr.Update.Message!, false).ToArray();
                    foreach (var msg in acceptedMessage)
                        await cntr.BotClient.SendTextMessageAsync(
                            chatId: msg.RecievedMessage.ChatId,
                            replyToMessageId: msg.RecievedMessage.TelegramMessageId,
                            text: "<i>Принято</i>",
                            parseMode: ParseMode.Html,
                            protectContent: true,
                            disableNotification: true
                        );
                }
                break;
            case "approve":
                await _context.Chains.Entry(activeChain).Reference(c => c.Supervisor).LoadAsync();
                if (activeChain.Supervisor!.ChatId == chatId)
                {
                    await _context.Chains.Entry(activeChain).Collection(c => c.Links!)
                        .Query().AsSplitQuery()
                        .Include(cl => cl.ForwardedMessage.Data)
                        .Include(cl => cl.ForwardedMessage.ReferenceTo)
                        .Include(cl => cl.RecievedMessage.ReferenceTo)
                        .Include(cl => cl.RecievedMessage.Data)
                        .LoadAsync();
                    var approvedOnLink = activeChain.Links!
                        .Where(cl => cl.ForwardedMessage.TelegramMessageId == msgId)
                        .Where(cl => cl.ForwardedMessage.ChatId == chatId)
                        .Single();
                    var toForward = approvedOnLink.ForwardedMessage.Data.MediaGroupId is string mediaGroupId
                        ? activeChain.Links!
                            .Where(l => l.ForwardedMessage.Data.Equals(mediaGroupId))
                            .OrderBy(l => l.ForwardedMessage.TelegramMessageId)
                            .ToArray()
                        : new[] { approvedOnLink };
                    foreach (var msg in approvedOnLink.ForwardedMessage)
                        if (msg.ChatId == activeChain.SourceChatId)
                        {
                            var replied = new RepliedMessage
                            {
                                ReferenceLinks = toForward,
                                DestinationMessage = msg,
                                ReplyItself = toForward.Select((cl, _) => cl.ForwardedMessage),
                                Markup = InlineKeyboardMarkupWrapper.ReplyToSource(activeChain.Id)
                            };
                            await _context.ChainLinks.AddRangeAsync(await RegularReply.ForwardReplyMessage(cntr, replied));
                            await _context.SaveChangesAsync();
                            await cntr.AnswerAsync($"Одобрено");
                            break;
                        }
                }
                break;
            case "decline":
            case "reject":
                await cntr.AnswerAsync("Работа отклонена. Ответьте на сообщение, чтобы оставить комментарий", showAlert: false);
                await cntr.EditAsync(inlineKeyboardMarkup: InlineKeyboardMarkup.Empty());
                break;
            default:
                await cntr.AnswerAsync("Неизвестная команда");
                break;
        }
    }
}