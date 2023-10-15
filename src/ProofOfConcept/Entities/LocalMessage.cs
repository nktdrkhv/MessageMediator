using System.Collections;
using System.ComponentModel.DataAnnotations.Schema;
using MessageMediator.ProofOfConcept.Abstract;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Telegram.Bot.Types;

namespace MessageMediator.ProofOfConcept.Entities;

[Table("Message")]
public class LocalMessage : ICreatedAt, IEnumerable<LocalMessage>
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

    public bool ActiveMarkup { get; set; }
    public bool ForceShow { get; set; }

    public LocalMessage(Message message, bool forceShow = false) : this(null, message, forceShow) { }

    public LocalMessage(string? customText, Message message, bool forceShow = false)
    {
        TelegramMessageId = message.MessageId;
        ChatId = message.Chat.Id;
        UserId = message.From?.Id;
        Data = new MessageData(customText, message);
        ActiveMarkup = message.ReplyMarkup != null;
        ForceShow = forceShow;
    }

    private LocalMessage() { }

    public static ICollection<LocalMessage> FromSet(string? customText, params Message[] messages)
    {
        var locals = new List<LocalMessage> { new(customText, messages.First()) };
        foreach (var msg in messages.Skip(1))
            locals.Add(new(null, msg));
        return locals;
    }

    public IEnumerator<LocalMessage> GetEnumerator() => new LocalMessageEnumerator(this);
    IEnumerator IEnumerable.GetEnumerator() => new LocalMessageEnumerator(this);
}

public class LocalMessageEnumerator : IEnumerator<LocalMessage>
{
    private readonly LocalMessage _start;
    private LocalMessage? _current = null;

    public LocalMessage Current => _current ?? throw new ArgumentException();
    object IEnumerator.Current => Current;

    public LocalMessageEnumerator(LocalMessage localMessage) => _start = localMessage;


    public void Dispose() => GC.SuppressFinalize(this);

    public bool MoveNext()
    {
        if (_current?.ReferenceTo != null)
        {
            _current = _current.ReferenceTo;
            return true;
        }
        else
        {
            return false;
        }
    }

    public void Reset() => _current = _start;
}