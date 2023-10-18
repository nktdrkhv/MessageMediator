using System.Text;
using MessageMediator.ProofOfConcept.Entities;

namespace MessageMediator.ProofOfConcept.Extensions;

public static class TriggerExtensions
{
    public static string Description(this Trigger trigger, string? addition = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Задача от <b>{trigger.Source.Name}</b>");
        if (trigger.Label != null)
            sb.AppendLine($"Тип: <u>{trigger.Label}</u>");
        if (!string.IsNullOrWhiteSpace(addition))
            sb.AppendLine(addition);
        return sb.ToString();
    }
}