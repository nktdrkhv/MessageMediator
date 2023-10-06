using Telegram.Bot.Types.Enums;

namespace MessageMediator.ProofOfConcept.Aggregates;

public record OriginalEntity
{
    public string Text { get; init; } = null!;
    public MessageEntityType Type { get; init; }
    public int Offset { get; init; }
    public int Length { get; init; }
}