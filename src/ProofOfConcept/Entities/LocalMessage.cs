using System.Collections;
using System.ComponentModel.DataAnnotations.Schema;
using MessageMediator.ProofOfConcept.Abstract;
using Telegram.Bot.Types;

namespace MessageMediator.ProofOfConcept.Entities;

[Table("Message")]
public class LocalMessage : ICreatedAt, IEnumerable<LocalMessage>
{
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
    public int Id { get; }
    public int TelegramMessageId { get; private set; }

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
    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    public IEnumerator<LocalMessage> GetEnumerator()
    {
        return new LocalMessageEnumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return new LocalMessageEnumerator(this);
    }

    public static ICollection<LocalMessage> FromSet(string? customText, params Message[] messages)
    {
        List<LocalMessage> locals = new List<LocalMessage> { new(customText, messages.First()) };
        foreach (Message msg in messages.Skip(1))
        {
            locals.Add(new LocalMessage(null, msg));
        }

        return locals;
    }
}

public class LocalMessageEnumerator : IEnumerator<LocalMessage>
{
    private readonly LocalMessage _start;
    private LocalMessage? _current;

    public LocalMessageEnumerator(LocalMessage localMessage)
    {
        _start = localMessage;
    }

    public LocalMessage Current => _current ?? throw new ArgumentException();
    object IEnumerator.Current => Current;


    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public bool MoveNext()
    {
        if (_current?.ReferenceTo != null)
        {
            _current = _current.ReferenceTo;
            return true;
        }

        return false;
    }

    public void Reset()
    {
        _current = _start;
    }
}