using System.Text;
using MessageMediator.ProofOfConcept.Entities;

namespace MessageMediator.ProofOfConcept.Extensions;

public static class TriggerExtensions
{
    public static string Description(this Trigger trigger, string? addition = null)
    {
        var sb = new StringBuilder();
        if (trigger.Source.Alias != null)
            sb.AppendLine($"Задача от <b>{trigger.Source.Alias}</b>");
        if (trigger.Label != null)
            sb.AppendLine($"Тип: <u>{trigger.Label}</u>");
        if (!string.IsNullOrWhiteSpace(addition))
        {
            if (sb.Length > 0)
                sb.Append("\n\n");
            sb.Append(addition);
        }
        return sb.ToString();
    }
}