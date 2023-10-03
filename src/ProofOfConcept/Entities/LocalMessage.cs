using System.ComponentModel.DataAnnotations.Schema;
using MessageMediator.ProofOfConcept.Abstract;
using Telegram.Bot.Types;

namespace MessageMediator.ProofOfConcept.Entities;

[Table("Message")]
public class LocalMessage : ICreatedAt
{
    public int Id { get; private set; }
    public int TelegramMessageId { get; set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public long ChatId { get; set; }
    public LocalChat Chat { get; set; } = null!;

    public long? UserId { get; set; }
    public LocalUser? User { get; set; }

    public int DataId { get; set; }
    public MessageData Data { get; set; } = null!;

    public LocalMessage(string? text, params Message[] messages)
    {
        var firstOne = messages.First();
        TelegramMessageId = firstOne.MessageId;
        ChatId = firstOne.Chat.Id;
        UserId = firstOne.From?.Id;
        Data = new MessageData(text, messages);
    }

    private LocalMessage() { }
}