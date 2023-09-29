using MessageMediator.ProofOfConcept.Enums;
using Telegram.Bot.Types;

namespace MessageMediator.ProofOfConcept.Entities;

public class ConnectedTrigger
{
    public int ConnectedTriggerId { get; set; }

    public int TriggerId { get; set; }
    public Trigger Trigger { get; set; } = null!;

    public long ExecutorId { get; set; }
    public Chat Executor { get; set; } = null!;
    public ExecutorRole Role { get; set; }
    public string? ExecutorDefaultAlias { get; set; }
    public bool IsOnProbation { get; set; } = true;

    public bool ShowSourceAlias { get; set; } = true;
    public bool ShowExecutorAlias { get; set; } = true;

    public bool IsDisabled { get; set; }

    public ICollection<Chain>? Chains { get; set; }
}