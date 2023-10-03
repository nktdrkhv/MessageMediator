using System.ComponentModel.DataAnnotations.Schema;
using MessageMediator.ProofOfConcept.Abstract;

namespace MessageMediator.ProofOfConcept.Entities;

[Table("Source")]
public class Source : IssueEntity
{
    public ICollection<Trigger>? Triggers { get; set; }
    public ICollection<LocalChat>? Submitters { get; set; }
}