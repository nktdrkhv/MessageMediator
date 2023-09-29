namespace MessageMediator.ProofOfConcept.Entities;

public class LocalMessage
{
    public int LocalMessageId { get; set; }
    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    public long? UserId { get; set; }
    public long? ChatId { get; set; }
    public LocalUser? User { get; set; }
    public LocalChat? Chat { get; set; }

    public int DataId { get; set; }
    public MessageData Data { get; set; } = null!;
}