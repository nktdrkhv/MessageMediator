using MessageMediator.ProofOfConcept.Entities;
using MessageMediator.ProofOfConcept.Extensions;
using MessageMediator.ProofOfConcept.Persistance;
using Telegram.Bot.Types;
using TelegramUpdater.FilterAttributes.Attributes;
using TelegramUpdater.Filters;
using TelegramUpdater.Helpers;
using TelegramUpdater.UpdateContainer;
using TelegramUpdater.UpdateHandlers.Scoped;
using TelegramUpdater.UpdateHandlers.Scoped.ReadyToUse;
using TelegramUpdater.RainbowUtilities;
using TelegramUpdater;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using System.Runtime.CompilerServices;
using System.Globalization;

namespace MessageMediator.ProofOfConcept.UpdateHandlers.Messages;

[Order(1)]
[Command("issues", '/', ArgumentsMode.NoArgs)]
[ChatType(ChatTypeFlags.Private)]
[UserNumericState("admin", anyState: true)]
public sealed class SourcesCommand : MessageHandler
{
    private readonly BotDbContext _context;
    private Message _panel = null!;
    private int _panelHash = 0;

    public SourcesCommand(BotDbContext context) => _context = context;

    protected override async Task HandleAsync(IContainer<Message> cntr)
    {
        var cmdMsg = cntr.Update;
        _panel = await SendTextMessageAsync("Загрузка...");
        while (true)
        {
            try
            {
                // ------------- available sources -------------
                var sources = _context.Sources.Where(s => !s.IsDeleted).Select(s => Tuple.Create(s.Name!, s.Id.ToString())).ToArray();
                var sourcesCmd = new[]{new[]
                {
                    Tuple.Create("Добавить", "add"),
                    Tuple.Create("Выйти", "exit")
                }};
                await EditPanelAsync("<b>Выберите источник:</b>", InlineKeyboardMarkupWrapper.FromCustomSet("source", sources, sourcesCmd));
                var chosenQuery = await AwaitButtonClickAsync(
                    timeOut: TimeSpan.FromSeconds(15),
                    callbackQueryRegex: new("source:(\\d+|add|exit)"),
                    onUnrelatedUpdate: (updater, update) => OnUnrelatedUpdate(updater, update));
                // ---------------------------------------------

                // ---------------- chosen/add source ---------------------
                await chosenQuery!.If(cntr => cntr.Update.Data!.Contains("source:add"), async cb =>
                {
                    await EditPanelAsync(
                        "Укажите название нового источника.\n\n<i>Внимание, данное название <u>видят</u> исполнители</i>",
                        new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Выход", "exit")));
                    var aliasMsg = await AwaitMessageAsync(
                        filter: FilterCutify.Text(),
                        timeOut: TimeSpan.FromSeconds(15),
                        onUnrelatedUpdate: (updater, update) => OnUnrelatedUpdate(updater, update)
                    );
                    var newAlisas = aliasMsg!.Update.Text!.Trim();
                    if (_context.Sources.Any(s => s.Name == newAlisas))
                    {
                        var alreadyExistsMsg = await aliasMsg.ResponseAsync("Данное имя уже существует");
                        await Task.Delay(TimeSpan.FromSeconds(3));
                        await BotClient.DeleteMessageAsync(alreadyExistsMsg.Update.Chat.Id, alreadyExistsMsg.Update.MessageId);
                        await BotClient.DeleteMessageAsync(aliasMsg.Update.Chat.Id, aliasMsg.Update.MessageId);
                        throw new ContinueCycle();
                    }
                    else
                    {
                        await BotClient.DeleteMessageAsync(aliasMsg.Update.Chat.Id, aliasMsg.Update.MessageId);
                        await _context.Sources.AddAsync(new Source { Name = newAlisas });
                        await _context.SaveChangesAsync();
                    }
                });
                // ---------------------------------------------

                // ----------- concrete source -----------------
                await chosenQuery!.If("source:\\d+", async cntr =>
                {
                    var plainId = cntr.Update.Data!.Split(':')[1];
                    var source = await _context.Sources.FindAsync(int.Parse(plainId));

                    var status = source!.IsDisabled ? "выключен" : "включен";
                    var text = $"<b>Источник:</b> {source!.Name}\n<b>Статус:</b> {status}\n\n/exit для выхода";
                    var markup = new InlineKeyboardMarkup(new[]{
                        InlineKeyboardButton.WithCallbackData("Триггеры", $"source:{plainId}:triggers"),
                        InlineKeyboardButton.WithCallbackData("Чаты",$"source:{plainId}:chats"),
                    });
                    await EditPanelAsync(text, markup);

                    chosenQuery = await AwaitButtonClickAsync(
                        timeOut: TimeSpan.FromSeconds(15),
                        callbackQueryRegex: new("source:\\d+:(triggers|chats)"),
                        onUnrelatedUpdate: (updater, update) => OnUnrelatedUpdate(updater, update));
                });
                // ---------------------------------------------

                // ----------------- blank ---------------------

                // ---------------------------------------------

                // ----------------- blank ---------------------

                // ---------------------------------------------

                // ----------------- blank ---------------------

                // ---------------------------------------------

                // ------------- exit catch --------------------
                await chosenQuery!.If(cntr => cntr.Update.Data!.Contains("exit"),
                    async cb => { await cb.AnswerAsync("Выход"); throw new Exception(); });

                await chosenQuery!.AnswerAsync("Действие неопределено");
            }
            catch (ContinueCycle)
            {
                continue;
            }
            catch (Exception ex)
            {
                // var expired = await SendTextMessageAsync("Время ответа истекло");
                // await Task.Delay(TimeSpan.FromSeconds(3));
                // await cntr.BotClient.DeleteMessageAsync(Chat, expired.MessageId);
                await cntr.BotClient.DeleteMessageAsync(Chat, _panel.MessageId);
                await DeleteAsync();
                break;
            }
        }
        StopPropagation();
    }

    private async ValueTask<Message?> EditPanelAsync(string? newText, InlineKeyboardMarkup? newMarkup)
    {

        var newHash = HashCode.Combine(newText?.GetHashCode() ?? 0, newMarkup?.CombinedHash() ?? 0);
        if (_panelHash != newHash)
        {
            _panelHash = newHash;
            return newText != null
                ? await BotClient.EditMessageTextAsync(
                    chatId: Chat,
                    messageId: _panel.MessageId,
                    text: newText,
                    parseMode: ParseMode.Html,
                    disableWebPagePreview: true,
                    replyMarkup: newMarkup ?? InlineKeyboardMarkup.Empty())
                : await BotClient.EditMessageReplyMarkupAsync(
                    chatId: Chat,
                    messageId: _panel.MessageId,
                    replyMarkup: newMarkup ?? InlineKeyboardMarkup.Empty());
        }
        else
            return null;
    }

    private static Task OnUnrelatedUpdate(IUpdater updater, ShiningInfo<long, Update> unrelated) => unrelated.Value switch
    {
        { Message.Text: "/exit" } => Task.Run(async () =>
        {
            await updater.BotClient.DeleteMessageAsync(unrelated.Value.Message.Chat.Id, unrelated.Value.Message.MessageId);
            throw new Exception();
        }),
        { Message: { } message } => updater.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId),
        { CallbackQuery.Data: "exit" } => Task.Run(async () =>
        {
            await updater.BotClient.AnswerCallbackQueryAsync(unrelated.Value.CallbackQuery.Id);
            throw new Exception();
        }),
        { CallbackQuery: { } callbackQuery } => updater.BotClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Завершите работу с панелью администратора"),
        _ => Task.CompletedTask
    };

    private class ContinueCycle : Exception { }
}