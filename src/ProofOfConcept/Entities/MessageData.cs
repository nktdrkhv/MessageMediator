using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using MessageMediator.ProofOfConcept.Enums;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MessageMediator.ProofOfConcept.Entities;

[Table("MessageData")]
public class MessageData
{
    public int MessageDataId { get; private set; }
    public MessageDataType Type { get; set; }

    public string? Text { get; set; }
    public ICollection<Media>? MediaFiles { get; set; }
    public Contact? Contact { get; set; }

    public MessageData(string? text, params Message[] messages)
    {
        var firstOne = messages.First();
        Type = firstOne.Type switch
        {
            MessageType.Text => MessageDataType.Text,
            MessageType.Photo or
            MessageType.Animation or
            MessageType.Photo or
            MessageType.Video or
            MessageType.VideoNote or
            MessageType.Audio or
            MessageType.Voice or
            MessageType.Document or
            MessageType.Sticker => MessageDataType.Media,
            MessageType.Contact => MessageDataType.Contact,
            _ => MessageDataType.Unknown
        };
        MediaFiles = messages.Select(m => new Media(m)).ToArray();
        Text = text ?? firstOne.Text ?? firstOne.Caption;
        Contact = firstOne.Contact;
    }

    public MessageData(MessageData data)
    {
        Type = data.Type;
        Text = data.Text;
        MediaFiles = data.MediaFiles;
        Contact = data.Contact;
    }

    private MessageData() { }
}