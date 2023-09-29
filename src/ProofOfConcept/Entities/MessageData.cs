namespace MessageMediator.ProofOfConcept.Entities;

public class MessageData
{
    public int MessageDataId { get; set; }

    public string? Text { get; set; }
    public ICollection<Media>? MediaFiles { get; set; }
}