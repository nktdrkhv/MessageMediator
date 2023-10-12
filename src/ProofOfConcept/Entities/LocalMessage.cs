using System.ComponentModel.DataAnnotations.Schema;
using MessageMediator.ProofOfConcept.Abstract;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Telegram.Bot.Types;

namespace MessageMediator.ProofOfConcept.Entities;

[Table("Message")]
public class LocalMessage : ICreatedAt
{
    public int Id { get; private set; }
    public int TelegramMessageId { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public int? ReferenceToId { get; set; }
    public LocalMessage? ReferenceTo { get; set; }

    public long ChatId { get; set; }
    public long? UserId { get; set; }
    public LocalChat Chat { get; set; } = null!;
    public LocalUser? User { get; set; }

    public int DataId { get; set; }
    public MessageData Data { get; set; } = null!;

    public LocalMessage(Message message) : this(null, message) { }

    public LocalMessage(string? customText, Message message)
    {
        TelegramMessageId = message.MessageId;
        ChatId = message.Chat.Id;
        UserId = message.From?.Id;
        Data = new MessageData(customText, message);
    }

    private LocalMessage() { }

    public static ICollection<LocalMessage> FromSet(string? customText, params Message[] messages)
    {
        var locals = new List<LocalMessage> { new LocalMessage(customText, messages.First()) };
        foreach (var msg in messages.Skip(1))
            locals.Add(new(null, msg));
        return locals;
    }
}