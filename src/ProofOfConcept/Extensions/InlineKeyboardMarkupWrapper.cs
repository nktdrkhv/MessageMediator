using Telegram.Bot.Types.ReplyMarkups;

namespace MessageMediator.ProofOfConcept.Extensions;

public static class InlineKeyboardMarkupWrapper
{
    public static InlineKeyboardMarkup FullWorkerControls(int chainId) =>
        new(new[]
            {
                new[]{InlineKeyboardButton.WithCallbackData("Взять в работу", $"take:{chainId}") },
                new[]{InlineKeyboardButton.WithCallbackData("Задать вопрос", $"question:{chainId}") }
            });

    public static InlineKeyboardMarkup OnlyQuestionControls(int chainId) =>
        new(InlineKeyboardButton.WithCallbackData("Задать вопрос", $"question:{chainId}"));

    public static InlineKeyboardMarkup OnlyTakeControls(int chainId) =>
        new(InlineKeyboardButton.WithCallbackData("Взять в работу", $"take:{chainId}"));

    public static InlineKeyboardMarkup FullSupervisorControls(int chainId) =>
        new(new[]
            {
                new[]{InlineKeyboardButton.WithCallbackData("Дополнить задачу исполнителю", $"remark:{chainId}") },
                new[]{InlineKeyboardButton.WithCallbackData("Уточнить у источника", $"revision:{chainId}") }
            });

    public static InlineKeyboardMarkup OnlyRemarkControls(int chainId) =>
        new(InlineKeyboardButton.WithCallbackData("Дополнить задачу исполнителю", $"remark:{chainId}"));

    public static InlineKeyboardMarkup OnlyRevisionControls(int chainId) =>
        new(InlineKeyboardButton.WithCallbackData("Уточнить у источника", $"revision:{chainId}"));

    public static InlineKeyboardMarkup ReplyToSource(int chainId) =>
        new(new[]
            {
                InlineKeyboardButton.WithCallbackData("✅", $"accept:{chainId}"),
                InlineKeyboardButton.WithCallbackData("⛔️", $"decline:{chainId}"),
            });

    public static InlineKeyboardMarkup ReplyToSupervisor(int chainId) =>
        new(new[]
            {
                InlineKeyboardButton.WithCallbackData("✅", $"approve:{chainId}"),
                InlineKeyboardButton.WithCallbackData("⛔️", $"reject:{chainId}"),
            });
}