using System.ComponentModel.DataAnnotations.Schema;
using MessageMediator.ProofOfConcept.Abstract;

namespace MessageMediator.ProofOfConcept.Entities;

[Table("Chain")]
public class Chain : ICreatedAt
{
    public int Id { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? FinishedAt { get; set; }

    public int TriggerId { get; set; }
    public Trigger Trigger { get; set; } = null!;

    public int? WorkerId { get; set; }
    public Worker? Worker { get; set; }

    public int? SupervisorId { get; set; }
    public Supervisor? Supervisor { get; set; }

    public int ReasonId { get; set; }
    public LocalMessage Reason { get; set; } = null!;

    public int PreparedDataId { get; set; }
    public MessageData PreparedData { get; set; } = null!;

    public ICollection<ChainLink>? Links { get; set; }
}