using System.ComponentModel.DataAnnotations.Schema;
using MessageMediator.ProofOfConcept.Abstract;

namespace MessageMediator.ProofOfConcept.Entities;

[Table("Supervisor")]
public class Supervisor : ExecutorEntity
{
}