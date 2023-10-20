using System.ComponentModel.DataAnnotations.Schema;
using MessageMediator.ProofOfConcept.Abstract;

namespace MessageMediator.ProofOfConcept.Entities;

[Table("Chain")]
public class Chain : ICreatedAt
{
    public int Id { get; }
    public DateTime? TookAt { get; set; }
    public DateTime? FinishedAt { get; set; }

    public int TriggerId { get; set; }
    public Trigger Trigger { get; set; } = null!;

    public long SourceChatId { get; set; }
    public LocalChat SourceChat { get; set; } = null!;

    public int? WorkerId { get; set; }
    public int? SupervisorId { get; set; }
    public Worker? Worker { get; set; }
    public Supervisor? Supervisor { get; set; }

    public ICollection<MessageData> PreparedData { get; set; } = null!;
    public ICollection<ChainLink>? Links { get; set; }
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
}