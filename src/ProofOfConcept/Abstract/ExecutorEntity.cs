using MessageMediator.ProofOfConcept.Entities;
using MessageMediator.ProofOfConcept.Enums;

namespace MessageMediator.ProofOfConcept.Abstract;

public abstract class ExecutorEntity : IssueEntity
{
    public long ChatId { get; set; }
    public LocalChat Chat { get; set; } = null!;

    public int TriggerId { get; set; }
    public Trigger Trigger { get; set; } = null!;
}