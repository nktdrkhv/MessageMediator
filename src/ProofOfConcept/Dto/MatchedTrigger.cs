using MessageMediator.ProofOfConcept.Enums;

namespace MessageMediator.ProofOfConcept.Dto;

public record MatchedTrigger
{
    public int TriggerId { get; init; }
    public TriggerBehaviour Behaviour { get; init; }
    public int Offset { get; init; }
    public int Length { get; init; }

    public (int Offset, int Length) Previous { get; set; }
    public (int Offset, int Length) Next { get; set; }
}