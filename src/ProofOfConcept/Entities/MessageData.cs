using System.ComponentModel.DataAnnotations.Schema;
using MessageMediator.ProofOfConcept.Enums;
using Telegram.Bot.Types;

namespace MessageMediator.ProofOfConcept.Entities;

[Table("MessageData")]
public class MessageData
{
    public int MessageDataId { get; private set; }
    public MessageDataType Type { get; set; }

    public string? Text { get; set; }
    public ICollection<Media>? MediaFiles { get; set; }
    public Contact? Contact { get; set; }
}