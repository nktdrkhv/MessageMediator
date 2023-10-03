using System.ComponentModel.DataAnnotations.Schema;
using MessageMediator.ProofOfConcept.Abstract;

namespace MessageMediator.ProofOfConcept.Entities;

[Table("ChainLink")]
public class ChainLink : ICreatedAt
{
    public int Id { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public int MotherChainId { get; set; }
    public Chain MotherChain { get; set; } = null!;

    public int RecievedMessageId { get; set; }
    public int ForwardMessageId { get; set; }
    public LocalMessage RecievedMessage { get; set; } = null!;
    public LocalMessage ForwardMessage { get; set; } = null!;
}