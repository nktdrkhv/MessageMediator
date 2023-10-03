using MessageMediator.ProofOfConcept.Enums;

namespace MessageMediator.ProofOfConcept.Aggregates;

public record MatchedTrigger
{
    public int TriggerId { get; init; }
    public TriggerBehaviour Behaviour { get; init; }
    public int Offset { get; init; }

    public int PreviousOffset { get; set; }
    public int NextOffset { get; set; }
}