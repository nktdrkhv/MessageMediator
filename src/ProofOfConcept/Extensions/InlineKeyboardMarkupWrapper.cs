using Telegram.Bot.Types.ReplyMarkups;

namespace MessageMediator.ProofOfConcept.Extensions;

public static class InlineKeyboardMarkupWrapper
{
    public static InlineKeyboardMarkup FullWorkerControls(int chainId) => new InlineKeyboardMarkup(new[]
        {
            new[]{InlineKeyboardButton.WithCallbackData("Взять в работу", $"take:{chainId}") },
            new[]{InlineKeyboardButton.WithCallbackData("Задать вопрос", $"ask:{chainId}") }
        });

    public static InlineKeyboardMarkup OnlyAskControls(int chainId) => new InlineKeyboardMarkup(
        InlineKeyboardButton.WithCallbackData("Задать вопрос", $"ask:{chainId}"));

    public static InlineKeyboardMarkup OnlyTakeControls(int chainId) => new InlineKeyboardMarkup(
        InlineKeyboardButton.WithCallbackData("Взять в работу", $"take:{chainId}"));

    public static InlineKeyboardMarkup ReplyToSource(int chainId) => new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData("✅", $"accept:{chainId}"),
            InlineKeyboardButton.WithCallbackData("⛔️", $"decline:{chainId}"),
        });

    public static InlineKeyboardMarkup ReplyToSupervisor(int chainId) => new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData("✅", $"approve:{chainId}"),
            InlineKeyboardButton.WithCallbackData("⛔️", $"reject:{chainId}"),
        });
}