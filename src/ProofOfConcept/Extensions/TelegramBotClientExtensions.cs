using MessageMediator.ProofOfConcept.Entities;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramUpdater.Exceptions;

namespace MessageMediator.ProofOfConcept.Extensions;

public static class TelegramBotClientExtensions
{
    public static async ValueTask<Message> SendChatRegistrationNotificationAsync(this ITelegramBotClient client, long chatId, LocalChat registeredChat) =>
        await client.SendTextMessageAsync(chatId,
        $"Чат <b>{registeredChat.Name}</b> ({registeredChat.Id}) был добавлен в бот",
        parseMode: ParseMode.Html);

    public static async ValueTask<Message> SendChatBlockedNotificationAsync(this ITelegramBotClient client, long chatId, LocalChat chat)
    {
        if (chat.ChatType == Telegram.Bot.Types.Enums.ChatType.Private)
        {
            return await client.SendTextMessageAsync(chatId,
                text: $"Пользователь <a href=\"tg://user?id={chat.Id}\"><b>{chat.Name}</b></a> заблокировал бота",
                parseMode: ParseMode.Html);
        }
        else
        {
            return await client.SendTextMessageAsync(chatId,
                            text: $"Бот исключен из группы <b>{chat.Name}</b>",
                            parseMode: ParseMode.Html);
        }
    }

    public static async ValueTask<Message> SendChatUnblockedNotificationAsync(this ITelegramBotClient client, long chatId, LocalChat chat)
    {
        if (chat.ChatType == Telegram.Bot.Types.Enums.ChatType.Private)
        {
            return await client.SendTextMessageAsync(chatId,
                text: $"Пользователь <a href=\"tg://user?id={chat.Id}\"><b>{chat.Name}</b></a> разблокировал бота",
                parseMode: ParseMode.Html);
        }
        else
        {
            return await client.SendTextMessageAsync(chatId,
                            text: $"Бот восстановлен в группе <b>{chat.Name}</b>",
                            parseMode: ParseMode.Html);
        }
    }

    public static async ValueTask<LocalMessage> SendIssueAsync(this ITelegramBotClient client, Chain chain)
    {
        throw new NotImplementedException();
    }

    //public StateKeeperNotRegistried
}