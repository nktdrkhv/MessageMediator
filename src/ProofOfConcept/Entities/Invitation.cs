using System.ComponentModel.DataAnnotations.Schema;
using MessageMediator.ProofOfConcept.Abstract;
using MessageMediator.ProofOfConcept.Enums;
using Microsoft.EntityFrameworkCore;

namespace MessageMediator.ProofOfConcept.Entities;

[Table("Invitation"), Index(nameof(Code), IsUnique = true)]
public class Invitation : ICreatedAt
{
    public int Id { get; private set; }

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? RedeemAt { get; private set; }

    public int TriggerId { get; private set; }
    public Trigger Trigger { get; private set; } = null!;

    public InvitationTarget Target { get; private set; }
    public string Code { get; private set; } = Guid.NewGuid().ToString()[..7];
    public string NewName { get; private set; } = null!;

    public Invitation(Trigger trigger, InvitationTarget target, string newName)
    {
        Trigger = trigger;
        Target = target;
        NewName = newName;
    }

    private Invitation() { }

    public void Redeemed() => RedeemAt = DateTime.UtcNow;
}