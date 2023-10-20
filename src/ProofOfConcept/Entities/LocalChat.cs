using System.ComponentModel.DataAnnotations.Schema;
using MessageMediator.ProofOfConcept.Abstract;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MessageMediator.ProofOfConcept.Entities;

[Table("Chat")]
public class LocalChat : TelegramEntity
{
    public LocalChat(Chat chat) : base(chat)
    {
        ChatType = chat.Type;
    }

    private LocalChat() { }
    public ChatType ChatType { get; set; }

    public ICollection<LocalUser>? DecisionMakers { get; set; }

    public ICollection<Source>? SourcingFor { get; set; }
    public ICollection<Worker>? WorkingOn { get; set; }
    public ICollection<Supervisor>? SupervisingOn { get; set; }
}