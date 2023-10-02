using System.ComponentModel.DataAnnotations.Schema;
using MessageMediator.ProofOfConcept.Abstract;

namespace MessageMediator.ProofOfConcept.Entities;

[Table("Message")]
public class LocalMessage : ICreatedAt
{
    public int LocalMessageId { get; private set; }
    public int TelegramMessageId { get; set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public long ChatId { get; set; }
    public LocalChat Chat { get; set; } = null!;

    public long? UserId { get; set; }
    public LocalUser? User { get; set; }

    public int DataId { get; set; }
    public MessageData Data { get; set; } = null!;
}