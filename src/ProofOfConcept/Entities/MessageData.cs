using System.ComponentModel.DataAnnotations.Schema;
using MessageMediator.ProofOfConcept.Enums;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MessageMediator.ProofOfConcept.Entities;

[Table("MessageData")]
public class MessageData
{
    public MessageData(string? text, Message message)
    {
        if (message.MediaGroupId == null)
        {
            Type = message.Type switch
            {
                MessageType.Text => MessageDataType.Text,
                MessageType.Photo or MessageType.Animation or MessageType.Photo or MessageType.Video or
                    MessageType.VideoNote or MessageType.Audio or MessageType.Voice or MessageType.Document or
                    MessageType.Sticker => MessageDataType.Media,
                MessageType.Contact => MessageDataType.Contact,
                _ => MessageDataType.Unknown
            };
        }
        else
        {
            Type = message.Type is MessageType.Photo or MessageType.Video or MessageType.Document or MessageType.Audio
                ? MessageDataType.MediaAlbum
                : MessageDataType.Unknown;
            MediaGroupId = message.MediaGroupId;
        }

        Media = new Media(message);
        Text = text ?? message.Text ?? message.Caption;
        Contact = message.Contact;
    }

    public MessageData(MessageData data)
    {
        Type = data.Type;
        MediaGroupId = data.MediaGroupId;
        Text = data.Text;
        Media = data.Media;
        Contact = data.Contact;
    }

    private MessageData() { }
    public int Id { get; }
    public string? MediaGroupId { get; }
    public MessageDataType Type { get; }

    public string? Text { get; set; }
    public Media? Media { get; set; }
    public Contact? Contact { get; set; }
}