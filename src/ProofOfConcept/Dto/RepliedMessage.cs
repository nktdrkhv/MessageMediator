using System.Collections;
using MessageMediator.ProofOfConcept.Entities;
using Telegram.Bot.Types.ReplyMarkups;

namespace MessageMediator.ProofOfConcept.Dto;

public record RepliedMessage
{
    public ChainLink? ReferenceLink { get; set; }
    public IEnumerable<ChainLink>? ReferenceLinks { get; set; }

    public LocalMessage DestinationMessage { get; set; } = null!;
    public IEnumerable<LocalMessage> ReplyItself { get; set; } = null!;
    public string? CustomText { get; set; }
    public IReplyMarkup? Markup { get; set; }
    public bool Protect { get; set; } = true;
}