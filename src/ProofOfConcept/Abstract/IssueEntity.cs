using System.ComponentModel.DataAnnotations;
using MessageMediator.ProofOfConcept.Entities;
using MessageMediator.ProofOfConcept.Enums;

namespace MessageMediator.ProofOfConcept.Abstract;

public abstract class IssueEntity
{
    [Key] public int Id { get; set; }
    public string Alias { get; set; } = null!;

    public bool IsDisabled { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
}