using System.ComponentModel.DataAnnotations.Schema;
using MessageMediator.ProofOfConcept.Enums;
using Telegram.Bot.Types.Enums;

namespace MessageMediator.ProofOfConcept.Entities;

[Table("Trigger")]
public class Trigger
{
    public int TriggerId { get; private set; }

    public string Text { get; set; } = null!;
    public MessageEntityType Type { get; set; }
    public TriggerBehaviour Behaviour { get; set; }

    public int SourceId { get; set; }
    public Source Source { get; set; } = null!;

    public ICollection<Worker> Workers { get; set; } = null!;
    public ICollection<Supervisor>? Supervisors { get; set; }
    public ICollection<Chain>? Chains { get; set; }

    public bool IsDisabled { get; set; }
}