using System.ComponentModel.DataAnnotations.Schema;
using MessageMediator.ProofOfConcept.Abstract;
using MessageMediator.ProofOfConcept.Enums;
using Microsoft.EntityFrameworkCore;

namespace MessageMediator.ProofOfConcept.Entities;

[Table("Invitation")]
[Index(nameof(Code), IsUnique = true)]
public class Invitation : ICreatedAt
{
    public int Id { get; private set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RedeemAt { get; set; }

    public InvitationTarget Target { get; set; }
    public string Code { get; set; } = Guid.NewGuid().ToString()[..7];

    public int? TriggerId { get; set; }
    public Trigger? Trigger { get; set; } = null!;
}