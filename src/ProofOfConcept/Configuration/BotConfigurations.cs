namespace MessageMediator.ProofOfConcept.Configuration;

public class BotConfiguration
{
    public static string Section = "BotConfiguration";

    public string ApiToken { get; set; } = null!;
    public string BotName { get; set; } = null!;
    public string BaseUrl { get; set; } = null!;
    public bool IsTestEnv { get; set; }
    public ICollection<long> Administrators { get; set; } = null!;
}