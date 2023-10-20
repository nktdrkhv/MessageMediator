using Telegram.Bot.Types.ReplyMarkups;

namespace MessageMediator.ProofOfConcept.Extensions;

public static class InlineKeyboardMarkupWrapper
{
    public static int CombinedHash(this InlineKeyboardMarkup markup)
    {
        int hashCode = 0;
        foreach (IEnumerable<InlineKeyboardButton> row in markup.InlineKeyboard)
        foreach (InlineKeyboardButton button in row)
        {
            hashCode = HashCode.Combine(hashCode, button.Text);
        }

        return hashCode;
    }

    public static InlineKeyboardMarkup FullWorkerControls(int chainId)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Взять в работу", $"take:{chainId}") },
            new[] { InlineKeyboardButton.WithCallbackData("Задать вопрос", $"question:{chainId}") }
        });
    }

    public static InlineKeyboardMarkup OnlyQuestionControls(int chainId)
    {
        return new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Задать вопрос", $"question:{chainId}"));
    }

    public static InlineKeyboardMarkup OnlyTakeControls(int chainId)
    {
        return new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Взять в работу", $"take:{chainId}"));
    }

    public static InlineKeyboardMarkup FullSupervisorControls(int chainId)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Дополнить задачу исполнителю", $"remark:{chainId}") },
            new[] { InlineKeyboardButton.WithCallbackData("Уточнить у источника", $"revision:{chainId}") }
        });
    }

    public static InlineKeyboardMarkup OnlyRemarkControls(int chainId)
    {
        return new InlineKeyboardMarkup(
            InlineKeyboardButton.WithCallbackData("Дополнить задачу исполнителю", $"remark:{chainId}"));
    }

    public static InlineKeyboardMarkup OnlyRevisionControls(int chainId)
    {
        return new InlineKeyboardMarkup(
            InlineKeyboardButton.WithCallbackData("Уточнить у источника", $"revision:{chainId}"));
    }

    public static InlineKeyboardMarkup ReplyToSource(int chainId)
    {
        return new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData("✅", $"accept:{chainId}"),
            InlineKeyboardButton.WithCallbackData("⛔️", $"decline:{chainId}")
        });
    }

    public static InlineKeyboardMarkup ReplyToSupervisor(int chainId)
    {
        return new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData("✅", $"approve:{chainId}"),
            InlineKeyboardButton.WithCallbackData("⛔️", $"reject:{chainId}")
        });
    }

    public static InlineKeyboardMarkup FromCustomSet(string prefix, IEnumerable<Tuple<string, string>> set,
        IEnumerable<IEnumerable<Tuple<string, string>>> prelude)
    {
        return new InlineKeyboardMarkup(
            prelude
                .Select(row =>
                    row.Select(elem => InlineKeyboardButton.WithCallbackData(elem.Item1, $"{prefix}:{elem.Item2}")))
                .Concat(set
                    .Select(value =>
                        new[] { InlineKeyboardButton.WithCallbackData(value.Item1, $"{prefix}:{value.Item2}") }))
        );
    }
}