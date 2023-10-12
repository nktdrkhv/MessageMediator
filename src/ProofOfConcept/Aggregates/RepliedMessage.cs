using MessageMediator.ProofOfConcept.Entities;
using Telegram.Bot.Types.ReplyMarkups;

namespace MessageMediator.ProofOfConcept.Aggregates;

public record RepliedMessage
{
    public LocalMessage DestinationMessage { get; set; } = null!;
    public string? CustomText { get; set; }
    public IReplyMarkup? Markup { get; set; }
    public bool Protect { get; set; } = true;
}