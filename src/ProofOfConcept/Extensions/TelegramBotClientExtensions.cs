using MessageMediator.ProofOfConcept.Entities;
using MessageMediator.ProofOfConcept.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MessageMediator.ProofOfConcept.Extensions;

public static class TelegramBotClientExtensions
{
    public static async ValueTask<Message> SendChatRegistrationNotificationAsync(this ITelegramBotClient client,
        long chatId, LocalChat registeredChat)
    {
        return await client.SendTextMessageAsync(chatId,
            $"Чат <b>{registeredChat.Name}</b> ({registeredChat.Id}) был добавлен в бот",
            parseMode: ParseMode.Html);
    }

    public static async ValueTask<Message> SendChatBlockedNotificationAsync(this ITelegramBotClient client, long chatId,
        LocalChat chat)
    {
        return chat.ChatType == ChatType.Private
            ? await client.SendTextMessageAsync(chatId,
                $"Пользователь <a href=\"tg://user?id={chat.Id}\"><b>{chat.Name}</b></a> заблокировал бота",
                parseMode: ParseMode.Html)
            : await client.SendTextMessageAsync(chatId,
                $"Бот исключен из <b>{chat.Name}</b>",
                parseMode: ParseMode.Html);
    }

    public static async ValueTask<Message> SendChatUnblockedNotificationAsync(this ITelegramBotClient client,
        long chatId, LocalChat chat)
    {
        return chat.ChatType == ChatType.Private
            ? await client.SendTextMessageAsync(chatId,
                $"Пользователь <a href=\"tg://user?id={chat.Id}\"><b>{chat.Name}</b></a> разблокировал бота",
                parseMode: ParseMode.Html)
            : await client.SendTextMessageAsync(chatId,
                $"Бот восстановлен в <b>{chat.Name}</b>",
                parseMode: ParseMode.Html);
    }

    public static async ValueTask<ICollection<LocalMessage>> SendIssueToWorkerAsync(this ITelegramBotClient client,
        Chain chain)
    {
        string customText = chain.Trigger.Description(chain.PreparedData.First().Text);
        InlineKeyboardMarkup markup = InlineKeyboardMarkupWrapper.FullWorkerControls(chain.Id);
        ICollection<Message> messages =
            await client.SendMessageDataAsync(chain.Worker!.ChatId, null, customText, chain.PreparedData, markup);
        return messages.OrderBy(m => m.MessageId).Select((m, _) => new LocalMessage(m)).ToArray();
    }

    public static async ValueTask<ICollection<LocalMessage>> SendIssueToSupervisorAsync(this ITelegramBotClient client,
        Chain chain)
    {
        string customText = chain.Trigger.Description(chain.PreparedData.First().Text);
        InlineKeyboardMarkup markup = InlineKeyboardMarkupWrapper.FullSupervisorControls(chain.Id);
        ICollection<Message> messages =
            await client.SendMessageDataAsync(chain.Supervisor!.ChatId, null, customText, chain.PreparedData, markup);
        return messages.OrderBy(m => m.MessageId).Select((m, _) => new LocalMessage(m)).ToArray();
    }

    // todo: channging document's name can be only after reloading
    public static async ValueTask<ICollection<Message>> SendMessageDataAsync(this ITelegramBotClient client,
        long chatId, int? replyTo, string? customText, IEnumerable<MessageData> dataCollection,
        IReplyMarkup? markup = null, bool protect = true)
    {
        if (dataCollection.Count() == 1 && dataCollection.First() is MessageData data)
        {
            switch (data.Type)
            {
                case MessageDataType.Text:
                    Message textMsg = await client.SendTextMessageAsync(chatId, customText ?? data.Text!,
                        disableWebPagePreview: true, replyToMessageId: replyTo, allowSendingWithoutReply: true,
                        replyMarkup: markup, protectContent: protect, parseMode: ParseMode.Html);
                    textMsg.Text = data.Text;
                    return new[] { textMsg };
                case MessageDataType.Media:
                    Media media = data.Media!;
                    Task<Message> mediaSending = media.MediaType switch
                    {
                        MediaType.Animation =>
                            client.SendAnimationAsync(chatId, InputFile.FromFileId(media.FileId!),
                                caption: customText ?? data.Text, replyToMessageId: replyTo,
                                allowSendingWithoutReply: true, replyMarkup: markup, protectContent: protect,
                                parseMode: ParseMode.Html),
                        MediaType.Photo =>
                            client.SendPhotoAsync(chatId, InputFile.FromFileId(media.FileId!),
                                caption: customText ?? data.Text, replyToMessageId: replyTo,
                                allowSendingWithoutReply: true, replyMarkup: markup, protectContent: protect,
                                parseMode: ParseMode.Html),
                        MediaType.Video =>
                            client.SendVideoAsync(chatId, InputFile.FromFileId(media.FileId!),
                                caption: customText ?? data.Text, replyToMessageId: replyTo,
                                allowSendingWithoutReply: true, replyMarkup: markup, protectContent: protect,
                                parseMode: ParseMode.Html),
                        MediaType.VideoNote =>
                            client.SendVideoNoteAsync(chatId, InputFile.FromFileId(media.FileId!),
                                replyToMessageId: replyTo, allowSendingWithoutReply: true, replyMarkup: markup,
                                protectContent: protect),
                        MediaType.Audio =>
                            client.SendAudioAsync(chatId, InputFile.FromFileId(media.FileId!),
                                caption: customText ?? data.Text, replyToMessageId: replyTo,
                                allowSendingWithoutReply: true, replyMarkup: markup, protectContent: protect,
                                parseMode: ParseMode.Html),
                        MediaType.Voice =>
                            client.SendVoiceAsync(chatId, InputFile.FromFileId(media.FileId!),
                                caption: customText ?? data.Text, replyToMessageId: replyTo,
                                allowSendingWithoutReply: true, replyMarkup: markup, protectContent: protect,
                                parseMode: ParseMode.Html),
                        MediaType.Document =>
                            client.SendDocumentAsync(chatId, InputFile.FromFileId(media.FileId!),
                                caption: customText ?? data.Text, replyToMessageId: replyTo,
                                allowSendingWithoutReply: true, replyMarkup: markup, protectContent: protect,
                                parseMode: ParseMode.Html),
                        MediaType.Sticker =>
                            client.SendStickerAsync(chatId, InputFile.FromFileId(media.FileId!),
                                replyToMessageId: replyTo, allowSendingWithoutReply: true, replyMarkup: markup,
                                protectContent: protect),
                        _ => throw new ArgumentException("MediaFile has unsupported type")
                    };
                    Message mediaMsg = await mediaSending;
                    mediaMsg.Caption = data.Text;
                    return new[] { mediaMsg };
                case MessageDataType.Contact:
                    return new[]
                    {
                        await client.SendContactAsync(chatId, data.Contact!.PhoneNumber, data.Contact!.FirstName,
                            lastName: data.Contact!.LastName, vCard: data.Contact!.Vcard)
                    };
                default:
                    throw new ArgumentException("MessageData has unsupported type");
            }
        }

        if (dataCollection.Count() > 1 && dataCollection.All(md => !string.IsNullOrWhiteSpace(md.MediaGroupId)))
        {
            IEnumerable<IAlbumInputMedia> mediaAlbum =
                dataCollection.Select((dc, _) => (IAlbumInputMedia)InputFile.FromFileId(dc.Media!.FileId!));
            Message[] messages = await client.SendMediaGroupAsync(chatId, mediaAlbum, protectContent: protect,
                replyToMessageId: replyTo);
            if (markup is InlineKeyboardMarkup inlineMarkup)
            {
                messages[0] = await client.EditMessageCaptionAsync(chatId, messages.First().MessageId,
                    customText ?? dataCollection.First().Text, replyMarkup: inlineMarkup, parseMode: ParseMode.Html);
            }

            if (dataCollection.First().Text is string text)
            {
                messages.First().Caption = text;
            }

            return messages;
        }

        return Array.Empty<Message>();
    }
}