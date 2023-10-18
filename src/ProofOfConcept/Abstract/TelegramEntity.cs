using System.ComponentModel.DataAnnotations;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MessageMediator.ProofOfConcept.Abstract;

public abstract class TelegramEntity : ICreatedAt
{
    [Key] public long Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string Name { get; set; } = null!;
    public string? Username { get; set; }
    public string? Alias { get; set; }

    public bool IsSelfBlocked { get; set; } = false;

    protected TelegramEntity(Chat chat)
    {
        Id = chat.Id;
        Name = chat.Type switch
        {
            ChatType.Private => chat.LastName != null ? $"{chat.FirstName} {chat.LastName}" : chat.FirstName!,
            ChatType.Group => chat.Title!,
            ChatType.Channel => chat.Title!,
            ChatType.Supergroup => chat.Title!,
            _ => throw new NotImplementedException(),
        };
        Username = chat.Username;
    }

    protected TelegramEntity(User user)
    {
        Id = user.Id;
        Name = user.LastName != null ? $"{user.FirstName} {user.LastName}" : user.FirstName;
        Username = user.Username;
    }

    protected TelegramEntity() { }
}