using System.ComponentModel.DataAnnotations.Schema;
using MessageMediator.ProofOfConcept.Abstract;
using Telegram.Bot.Types;

namespace MessageMediator.ProofOfConcept.Entities;

[Table("User")]
public class LocalUser : TelegramEntity
{
    public LocalUser(User user) : base(user) { }
    private LocalUser() { }
    public ICollection<LocalChat>? ResponsibleFor { get; set; }
}