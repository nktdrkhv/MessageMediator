using System.ComponentModel.DataAnnotations.Schema;
using Telegram.Bot.Types.Enums;

namespace MessageMediator.ProofOfConcept.Entities;

[Table("LocalChat")]
public class LocalChat
{
    public long Id { get; }
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public ChatType ChatType { get; }

    public string Title { get; private set; } = null!;
    public string? Username { get; private set; }
    public string? Alias { get; private set; }

    public bool IsDisabled { get; private set; } = false;
    public bool IsSelfBlocked { get; private set; } = false;
}