using System.ComponentModel.DataAnnotations.Schema;

namespace MessageMediator.ProofOfConcept.Entities;

[Table("LocalUser")]
public class LocalUser
{
    public long Id { get; }
    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    public string Name { get; private set; } = null!;
    public string? Username { get; private set; }
    public string? Alias { get; private set; }

    public bool IsDisabled { get; private set; } = false;
    public bool IsSelfBlocked { get; private set; } = false;
}