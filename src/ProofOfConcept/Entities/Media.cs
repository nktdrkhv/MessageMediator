using System.ComponentModel.DataAnnotations.Schema;
using MessageMediator.ProofOfConcept.Enums;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MessageMediator.ProofOfConcept.Entities;

[Table("Media")]
public class Media
{
    public int Id { get; private set; }
    public MediaType MediaType { get; private set; } = MediaType.None;
    public string? FileId { get; private set; }
    public string? FileUniqueId { get; private set; }
    public string? Path { get; private set; }

    public Media(MediaType mediaType, string? fileId = null, string? fileUniqueId = null, string? path = null)
    {
        if (string.IsNullOrWhiteSpace(fileId) && string.IsNullOrWhiteSpace(path))
            throw new ArgumentNullException(nameof(path), "Path of the local media file can not be null, if fileId is not submitted");
        MediaType = mediaType;
        FileId = fileId;
        FileUniqueId = fileUniqueId;
        Path = path;
    }

    public Media(Message message)
    {
        Set(message);
        if (string.IsNullOrWhiteSpace(FileId) || MediaType == MediaType.None)
            throw new ArgumentException("There is no suitable media in the submitted message", nameof(message));
    }

    private Media() { }

    public void Set(Message message)
    {
        if (!string.IsNullOrWhiteSpace(FileId))
            return;

        var converted = message.Type switch
        {
            MessageType.Photo => MediaType.Photo,
            MessageType.Audio => MediaType.Audio,
            MessageType.Video => MediaType.Video,
            MessageType.Voice => MediaType.Voice,
            MessageType.Document => MediaType.Document,
            MessageType.Sticker => MediaType.Sticker,
            MessageType.VideoNote => MediaType.VideoNote,
            MessageType.Animation => MediaType.Animation,
            _ => MediaType.Unknown
        };

        if (MediaType != MediaType.None && MediaType != converted)
            return;

        MediaType = converted;

        switch (message.Type)
        {
            case MessageType.Photo:
                var photo = message.Photo!.Last();
                FileId = photo.FileId;
                FileUniqueId = photo.FileUniqueId;
                break;
            case MessageType.Audio:
                FileId = message.Audio!.FileId;
                FileUniqueId = message.Audio!.FileUniqueId;
                break;
            case MessageType.Video:
                FileId = message.Video!.FileId;
                FileUniqueId = message.Video!.FileUniqueId;
                break;
            case MessageType.Voice:
                FileId = message.Voice!.FileId;
                FileUniqueId = message.Voice!.FileUniqueId;
                break;
            case MessageType.Document:
                FileId = message.Document!.FileId;
                FileUniqueId = message.Document!.FileUniqueId;
                break;
            case MessageType.Sticker:
                FileId = message.Sticker!.FileId;
                FileUniqueId = message.Sticker!.FileUniqueId;
                break;
            case MessageType.VideoNote:
                FileId = message.VideoNote!.FileId;
                FileUniqueId = message.VideoNote!.FileUniqueId;
                break;
            case MessageType.Animation:
                FileId = message.Animation!.FileId;
                FileUniqueId = message.Animation!.FileUniqueId;
                break;
            default:
                break;
        };
    }
}