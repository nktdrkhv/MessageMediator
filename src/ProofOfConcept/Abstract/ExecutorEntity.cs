using MessageMediator.ProofOfConcept.Entities;
using MessageMediator.ProofOfConcept.Enums;

namespace MessageMediator.ProofOfConcept.Abstract;

public abstract class ExecutorEntity : IssueEntity
{
    public int TriggerId { get; set; }
    public Trigger Trigger { get; set; } = null!;
}