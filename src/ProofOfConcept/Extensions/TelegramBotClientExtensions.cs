using System.Text;
using MessageMediator.ProofOfConcept.Entities;
using MessageMediator.ProofOfConcept.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MessageMediator.ProofOfConcept.Extensions;

public static class TelegramBotClientExtensions
{
    public static async ValueTask<Message> SendChatRegistrationNotificationAsync(this ITelegramBotClient client, long chatId, LocalChat registeredChat) =>
        await client.SendTextMessageAsync(chatId,
        $"Чат <b>{registeredChat.Name}</b> ({registeredChat.Id}) был добавлен в бот",
        parseMode: ParseMode.Html);

    public static async ValueTask<Message> SendChatBlockedNotificationAsync(this ITelegramBotClient client, long chatId, LocalChat chat)
    {
        return chat.ChatType == ChatType.Private
            ? await client.SendTextMessageAsync(chatId,
                text: $"Пользователь <a href=\"tg://user?id={chat.Id}\"><b>{chat.Name}</b></a> заблокировал бота",
                parseMode: ParseMode.Html)
            : await client.SendTextMessageAsync(chatId,
                            text: $"Бот исключен из группы <b>{chat.Name}</b>",
                            parseMode: ParseMode.Html);
    }

    public static async ValueTask<Message> SendChatUnblockedNotificationAsync(this ITelegramBotClient client, long chatId, LocalChat chat)
    {
        return chat.ChatType == ChatType.Private
            ? await client.SendTextMessageAsync(chatId,
                text: $"Пользователь <a href=\"tg://user?id={chat.Id}\"><b>{chat.Name}</b></a> разблокировал бота",
                parseMode: ParseMode.Html)
            : await client.SendTextMessageAsync(chatId,
                            text: $"Бот восстановлен в группе <b>{chat.Name}</b>",
                            parseMode: ParseMode.Html);
    }

    public static async ValueTask<LocalMessage> SendIssueAsync(this ITelegramBotClient client, Chain chain)
    {
        var sb = new StringBuilder();

        if (chain.Trigger.Source.Alias != null)
            sb.AppendLine($"Задача от <b>{chain.Trigger.Source.Alias}</b>");
        if (chain.Trigger.Label != null)
            sb.AppendLine($"Тип: <b>{chain.Trigger.Label}</b>");
        if (chain.PreparedData.Text != null)
        {
            if (sb.Length > 0)
                sb.Append("\n\n");
            sb.Append(chain.PreparedData.Text);
        }

        var text = sb.Length > 0 ? sb.ToString() : null;

        var markup = new InlineKeyboardMarkup(
            InlineKeyboardButton.WithCallbackData("Взять в работу", $"take:{chain.Id}"));

        var msg = await client.SendMessageDataAsync(chain.Worker!.ChatId, null, text, chain.PreparedData, markup);
        return new LocalMessage(text, msg);
    }

    public static async ValueTask<Message[]> SendMessageDataAsync(this ITelegramBotClient client, long chatId, int? replyTo, string? text, MessageData data, IReplyMarkup? markup = null, bool protect = true)
    {
        switch (data.Type)
        {
            case MessageDataType.Text:
                return new[] { await client.SendTextMessageAsync(chatId: chatId, text: text ?? data.Text!, disableWebPagePreview: true, replyToMessageId: replyTo, allowSendingWithoutReply: true, replyMarkup: markup, protectContent: protect, parseMode: ParseMode.Html) };
            case MessageDataType.Media:
                var media = data.MediaFiles!.First();
                var mediaSending = media.MediaType switch
                {
                    MediaType.Animation =>
                        client.SendAnimationAsync(chatId: chatId, animation: InputFile.FromFileId(media.FileId!), caption: text ?? data.Text, replyToMessageId: replyTo, allowSendingWithoutReply: true, replyMarkup: markup, protectContent: protect, parseMode: ParseMode.Html),
                    MediaType.Photo =>
                        client.SendPhotoAsync(chatId: chatId, photo: InputFile.FromFileId(media.FileId!), caption: text ?? data.Text, replyToMessageId: replyTo, allowSendingWithoutReply: true, replyMarkup: markup, protectContent: protect, parseMode: ParseMode.Html),
                    MediaType.Video =>
                        client.SendVideoAsync(chatId: chatId, video: InputFile.FromFileId(media.FileId!), caption: text ?? data.Text, replyToMessageId: replyTo, allowSendingWithoutReply: true, replyMarkup: markup, protectContent: protect, parseMode: ParseMode.Html),
                    MediaType.VideoNote =>
                        client.SendVideoNoteAsync(chatId: chatId, videoNote: InputFile.FromFileId(media.FileId!), replyToMessageId: replyTo, allowSendingWithoutReply: true, replyMarkup: markup, protectContent: protect),
                    MediaType.Audio =>
                        client.SendAudioAsync(chatId: chatId, audio: InputFile.FromFileId(media.FileId!), caption: text ?? data.Text, replyToMessageId: replyTo, allowSendingWithoutReply: true, replyMarkup: markup, protectContent: protect, parseMode: ParseMode.Html),
                    MediaType.Voice =>
                        client.SendVoiceAsync(chatId: chatId, voice: InputFile.FromFileId(media.FileId!), caption: text ?? data.Text, replyToMessageId: replyTo, allowSendingWithoutReply: true, replyMarkup: markup, protectContent: protect, parseMode: ParseMode.Html),
                    MediaType.Document =>
                        client.SendDocumentAsync(chatId: chatId, document: InputFile.FromFileId(media.FileId!), caption: text ?? data.Text, replyToMessageId: replyTo, allowSendingWithoutReply: true, replyMarkup: markup, protectContent: protect, parseMode: ParseMode.Html),
                    MediaType.Sticker =>
                        client.SendStickerAsync(chatId: chatId, sticker: InputFile.FromFileId(media.FileId!), replyToMessageId: replyTo, allowSendingWithoutReply: true, replyMarkup: markup, protectContent: protect),
                    _ => throw new ArgumentException("MediaFile has unsupported type")
                };
                return new[] { await mediaSending };
            case MessageDataType.MediaAlbum:
                var mediaAlbum = data.MediaFiles!.Select((m, _) => (IAlbumInputMedia)InputFile.FromFileId(m.FileId!)).ToArray();
                var messages = await client.SendMediaGroupAsync(chatId: chatId, media: mediaAlbum, protectContent: protect, replyToMessageId: replyTo);
                if (markup is InlineKeyboardMarkup inlineMarkup)
                    await client.EditMessageCaptionAsync(chatId, messages.First().MessageId, text ?? data.Text, replyMarkup: inlineMarkup, parseMode: ParseMode.Html);
                return messages;
            case MessageDataType.Contact:
                return new[] { await client.SendContactAsync(chatId: chatId, phoneNumber: data.Contact!.PhoneNumber, firstName: data.Contact!.FirstName, lastName: data.Contact!.LastName, vCard: data.Contact!.Vcard) };
            case MessageDataType.Unknown:
                throw new ArgumentException("MessageData has unsupported type", nameof(data));
        }
        return Array.Empty<Message>();
    }
}