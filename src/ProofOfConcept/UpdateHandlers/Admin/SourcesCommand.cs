using MessageMediator.ProofOfConcept.Entities;
using MessageMediator.ProofOfConcept.Extensions;
using MessageMediator.ProofOfConcept.Persistance;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramUpdater;
using TelegramUpdater.FilterAttributes.Attributes;
using TelegramUpdater.Filters;
using TelegramUpdater.Helpers;
using TelegramUpdater.RainbowUtilities;
using TelegramUpdater.UpdateContainer;
using TelegramUpdater.UpdateHandlers.Scoped;
using TelegramUpdater.UpdateHandlers.Scoped.ReadyToUse;

namespace MessageMediator.ProofOfConcept.UpdateHandlers.Messages;

[Order(1)]
[Command("issues", '/', ArgumentsMode.NoArgs)]
[ChatType(ChatTypeFlags.Private)]
[UserNumericState("admin", anyState: true)]
public sealed class SourcesCommand : MessageHandler
{
    private readonly BotDbContext _context;
    private Message _panel = null!;
    private int _panelHash;

    public SourcesCommand(BotDbContext context)
    {
        _context = context;
    }

    protected override async Task HandleAsync(IContainer<Message> cntr)
    {
        Message cmdMsg = cntr.Update;
        _panel = await SendTextMessageAsync("Загрузка...");
        while (true)
        {
            try
            {
                // ------------- available sources -------------
                Tuple<string, string>[] sources = _context.Sources.Where(s => !s.IsDeleted)
                    .Select(s => Tuple.Create(s.Name, s.Id.ToString())).ToArray();
                Tuple<string, string>[][] sourcesCmd =
                {
                    new[] { Tuple.Create("Добавить", "add"), Tuple.Create("Выйти", "exit") }
                };
                await EditPanelAsync("<b>Выберите источник:</b>",
                    InlineKeyboardMarkupWrapper.FromCustomSet("source", sources, sourcesCmd));
                IContainer<CallbackQuery>? chosenQuery = await AwaitButtonClickAsync(
                    TimeSpan.FromSeconds(15),
                    new CallbackQueryRegex("source:(\\d+|add|exit)"),
                    OnUnrelatedUpdate);
                // ---------------------------------------------

                // ---------------- chosen/add source ---------------------
                await chosenQuery!.If(cntr => cntr.Update.Data!.Contains("source:add"), async cb =>
                {
                    await EditPanelAsync(
                        "Укажите название нового источника.\n\n<i>Внимание, данное название <u>видят</u> исполнители</i>",
                        new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Выход", "exit")));
                    IContainer<Message>? aliasMsg = await AwaitMessageAsync(
                        FilterCutify.Text(),
                        TimeSpan.FromSeconds(15),
                        (updater, update) => OnUnrelatedUpdate(updater, update)
                    );
                    string newAlisas = aliasMsg!.Update.Text!.Trim();
                    if (_context.Sources.Any(s => s.Name == newAlisas))
                    {
                        IContainer<Message> alreadyExistsMsg =
                            await aliasMsg.ResponseAsync("Данное имя уже существует");
                        await Task.Delay(TimeSpan.FromSeconds(3));
                        await BotClient.DeleteMessageAsync(alreadyExistsMsg.Update.Chat.Id,
                            alreadyExistsMsg.Update.MessageId);
                        await BotClient.DeleteMessageAsync(aliasMsg.Update.Chat.Id, aliasMsg.Update.MessageId);
                        throw new ContinueCycle();
                    }

                    await BotClient.DeleteMessageAsync(aliasMsg.Update.Chat.Id, aliasMsg.Update.MessageId);
                    await _context.Sources.AddAsync(new Source { Name = newAlisas });
                    await _context.SaveChangesAsync();
                });
                // ---------------------------------------------

                // ----------- concrete source -----------------
                await chosenQuery!.If("source:\\d+", async cntr =>
                {
                    string plainId = cntr.Update.Data!.Split(':')[1];
                    Source? source = await _context.Sources.FindAsync(int.Parse(plainId));

                    string status = source!.IsDisabled ? "выключен" : "включен";
                    string text = $"<b>Источник:</b> {source!.Name}\n<b>Статус:</b> {status}\n\n/exit для выхода";
                    InlineKeyboardMarkup markup = new(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Триггеры", $"source:{plainId}:triggers"),
                        InlineKeyboardButton.WithCallbackData("Чаты", $"source:{plainId}:chats")
                    });
                    await EditPanelAsync(text, markup);

                    chosenQuery = await AwaitButtonClickAsync(
                        TimeSpan.FromSeconds(15),
                        new CallbackQueryRegex("source:\\d+:(triggers|chats)"),
                        (updater, update) => OnUnrelatedUpdate(updater, update));
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
                    async cb =>
                    {
                        await cb.AnswerAsync("Выход");
                        throw new Exception();
                    });

                await chosenQuery!.AnswerAsync("Действие неопределено");
            }
            catch (ContinueCycle)
            {
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
        int newHash = HashCode.Combine(newText?.GetHashCode() ?? 0, newMarkup?.CombinedHash() ?? 0);
        if (_panelHash != newHash)
        {
            _panelHash = newHash;
            return newText != null
                ? await BotClient.EditMessageTextAsync(
                    Chat,
                    _panel.MessageId,
                    newText,
                    ParseMode.Html,
                    disableWebPagePreview: true,
                    replyMarkup: newMarkup ?? InlineKeyboardMarkup.Empty())
                : await BotClient.EditMessageReplyMarkupAsync(
                    Chat,
                    _panel.MessageId,
                    newMarkup ?? InlineKeyboardMarkup.Empty());
        }

        return null;
    }

    private static Task OnUnrelatedUpdate(IUpdater updater, ShiningInfo<long, Update> unrelated)
    {
        return unrelated.Value switch
        {
            { Message.Text: "/exit" } => Task.Run(async () =>
            {
                await updater.BotClient.DeleteMessageAsync(unrelated.Value.Message.Chat.Id,
                    unrelated.Value.Message.MessageId);
                throw new Exception();
            }),
            { Message: { } message } => updater.BotClient.DeleteMessageAsync(message.Chat.Id, message.MessageId),
            { CallbackQuery.Data: "exit" } => Task.Run(async () =>
            {
                await updater.BotClient.AnswerCallbackQueryAsync(unrelated.Value.CallbackQuery.Id);
                throw new Exception();
            }),
            { CallbackQuery: { } callbackQuery } => updater.BotClient.AnswerCallbackQueryAsync(callbackQuery.Id,
                "Завершите работу с панелью администратора"),
            _ => Task.CompletedTask
        };
    }

    private class ContinueCycle : Exception
    {
    }
}