using System.ComponentModel.DataAnnotations.Schema;
using MessageMediator.ProofOfConcept.Abstract;

namespace MessageMediator.ProofOfConcept.Entities;

[Table("Worker")]
public class Worker : ExecutorEntity
{
    public bool IsOnProbation { get; set; } = false;
}