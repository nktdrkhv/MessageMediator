namespace MessageMediator.ProofOfConcept.Entities;

public class ChainLink
{
    public int ChainLinkId { get; set; }
    public DateTime CreaеtedAt { get; } = DateTime.UtcNow;

    public int MotherChainId { get; set; }
    public Chain MotherChain { get; set; } = null!;

    public int RecievedId { get; set; }
    public LocalMessage Recieved { get; set; } = null!;

    public int ForwardId { get; set; }
    public LocalMessage Forward { get; set; } = null!;
}