using MessageMediator.ProofOfConcept.Entities;
using MessageMediator.ProofOfConcept.Enums;
using MessageMediator.ProofOfConcept.Extensions;
using MessageMediator.ProofOfConcept.Persistance;
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
            await cntr.EditAsync(inlineKeyboardMarkup: null);
            StopPropagation();
            return;
        }

        //var activeChat = await _context.LocalChats.FindAsync(cntr.Update.Message!.Chat.Id);
        var chatId = cntr.Update.Message!.Chat.Id;
        var activeChain = await _context.Chains.FindAsync(chainId);
        if (activeChain == null)
        {
            await cntr.AnswerAsync("Информация по данной цепочке сообщений отсутсвует");
            await cntr.EditAsync(inlineKeyboardMarkup: null);
            StopPropagation();
            return;
        }

        // ----------------------------------

        // ------------ reacting ------------

        // todo: current model (e.g. accept:5 where 5 is chain's ID) is unsecure. Check rights via DecisionMakers property
        switch (queryArgs![0])
        {
            case "ask":
                await _context.Chains.Entry(activeChain).Reference(c => c.Worker).LoadAsync();
                await _context.Chains.Entry(activeChain).Collection(c => c.Links!).LoadAsync();
                if (activeChain.Worker!.ChatId == chatId)
                {
                    await cntr.AnswerAsync("Напишите вопрос, как ответ на сообщение");
                    var pleaseAskMsg = await cntr.SendAsync(
                        text: "<i>Обратная связь</i>",
                        sendAsReply: true,
                        parseMode: ParseMode.Html,
                        disableNotification: true,
                        protectContent: true,
                        replyMarkup: new ForceReplyMarkup() { InputFieldPlaceholder = "Ваш вопрос...", Selective = true });
                    activeChain.Links!.Add(new ChainLink()
                    {
                        Mode = ChainLinkMode.Question,
                        // todo: should be as acceptedMessage at case "accept"
                        RecievedMessageId = activeChain.Links.First().RecievedMessageId,
                        ForwardedMessage = new LocalMessage(pleaseAskMsg.Update)
                    });
                    await _context.SaveChangesAsync();
                    if (activeChain.TookAt != null)
                        await cntr.EditAsync(inlineKeyboardMarkup: null);
                    else
                        await cntr.EditAsync(inlineKeyboardMarkup: InlineKeyboardMarkupWrapper.OnlyTakeControls(activeChain.Id));
                }
                break;
            case "take":
                // todo: depends on broadcastins the issue mode, this may require to delete inline markups of other workers
                await _context.Chains.Entry(activeChain).Reference(c => c.Worker).LoadAsync();
                await _context.Chains.Entry(activeChain).Collection(c => c.Links!).LoadAsync();
                if (activeChain.Worker!.ChatId == chatId)
                {
                    activeChain.TookAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    await cntr.AnswerAsync("Взято в работу");
                    if (activeChain.Links!.Any(l => l.Mode == ChainLinkMode.Question))
                        await cntr.EditAsync(inlineKeyboardMarkup: null);
                    else
                        await cntr.EditAsync(inlineKeyboardMarkup: InlineKeyboardMarkupWrapper.OnlyAskControls(activeChain.Id));
                }
                break;
            case "accept":
                // todo: the source might accept only one part of the responsed work. there sould be an option for a partial acception;
                await _context.Chains.Entry(activeChain).Collection(c => c.Links!)
                    .Query().Include(cl => cl.ForwardedMessage).Include(cl => cl.RecievedMessage).LoadAsync();
                if (activeChain.SourceChatId == chatId)
                {
                    activeChain.FinishedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    await cntr.AnswerAsync("Работа принята");
                    await cntr.EditAsync(inlineKeyboardMarkup: null);
                    var acceptedMessage = activeChain.Links!
                        .Single(cl => cl.ForwardedMessage.TelegramMessageId == cntr.Update.Message!.MessageId).RecievedMessage;
                    await cntr.BotClient.SendTextMessageAsync(
                        chatId: acceptedMessage.ChatId,
                        replyToMessageId: acceptedMessage.TelegramMessageId,
                        text: "<i>Принято</i>",
                        parseMode: ParseMode.Html,
                        protectContent: true
                    );
                }
                break;
            case "approve":
                break;
            case "decline":
            case "reject":
                await cntr.AnswerAsync("Работа отклонена. Ответьте на сообщение, чтобы оставить комментарий", showAlert: false);
                await cntr.EditAsync(inlineKeyboardMarkup: null);
                break;
            default:
                await cntr.AnswerAsync("Неизвестная команда");
                break;
        }
    }
}