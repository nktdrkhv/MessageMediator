using System.ComponentModel.DataAnnotations.Schema;
using MessageMediator.ProofOfConcept.Enums;

namespace MessageMediator.ProofOfConcept.Entities;

[Table("Trigger")]
public class Trigger
{
    public int TriggerId { get; }

    public string Text { get; set; } = null!;
    public TriggerType Type { get; set; }
    public TriggerBehaviour Behaviour { get; set; }

    public long SourceId { get; set; }
    public LocalChat Source { get; set; } = null!;
    public string? SourceDefaultAlias { get; set; }

    public bool IsDisabled { get; set; }

    public ICollection<ConnectedTrigger>? ConnectedTriggers { get; set; }
}