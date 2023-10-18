using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace MessageMediator.ProofOfConcept.Abstract;

[Index(nameof(Name), IsUnique = true)]
public abstract class IssueEntity
{
    [Key] public int Id { get; set; }
    [Required] public string Name { get; set; } = null!;

    public bool IsDisabled { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
}