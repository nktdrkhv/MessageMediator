namespace MessageMediator.ProofOfConcept.Entities;

public class Chain
{
    public int ChainId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int ConnectedTriggerId { get; set; }
    public ConnectedTrigger ConnectedTrigger { get; set; } = null!;

    public int ReasonId { get; set; }
    public LocalMessage Reason { get; set; } = null!;

    public int PreparedDataId { get; set; }
    public MessageData PreparedData { get; set; } = null!;

    public ICollection<ChainLink>? Links { get; set; }
}