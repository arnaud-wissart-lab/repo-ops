using System.Text;
using RepoOps.Worker.Models;

namespace RepoOps.Worker.Services;

public sealed class PromptDigestRenderer
{
    public GeneratedPromptDigest Render(GeneratedPromptResult result)
    {
        return new GeneratedPromptDigest
        {
            Subject = $"[repo-ops] Prompts superviseur du {result.GeneratedAtUtc:yyyy-MM-dd}",
            PlainTextBody = BuildPlainText(result)
        };
    }

    private static string BuildPlainText(GeneratedPromptResult result)
    {
        var builder = new StringBuilder();

        builder.AppendLine("Prompts superviseur repo-ops");
        builder.AppendLine($"Date de génération : {result.GeneratedAtUtc:O}");
        builder.AppendLine($"Statut du rapport source : {result.SourceReportStatus}");
        builder.AppendLine();
        builder.AppendLine("Compteurs :");
        builder.AppendLine($"- Prompts totaux : {result.Summary.TotalPrompts}");
        builder.AppendLine($"- Priorité haute : {result.Summary.HighPriorityPrompts}");
        builder.AppendLine($"- Prompts de revue : {result.Summary.ReviewPrompts}");
        builder.AppendLine($"- Prompts de correction : {result.Summary.FixPrompts}");
        builder.AppendLine($"- Prompts de validation finale : {result.Summary.ValidationPrompts}");
        builder.AppendLine();
        builder.AppendLine("Prompts prêts à l'emploi :");

        if (result.Prompts.Count == 0)
        {
            builder.AppendLine("- Aucun prompt disponible.");
        }
        else
        {
            foreach (var prompt in result.Prompts)
            {
                var target = prompt.PullRequestNumber is null
                    ? prompt.Repository
                    : $"{prompt.Repository}#{prompt.PullRequestNumber}";
                builder.AppendLine($"- [{prompt.Priority}] {target} - {prompt.PromptType}");
                builder.AppendLine($"  Résumé : {prompt.Context.ProblemSummary}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("Notes :");

        foreach (var note in result.Notes)
        {
            builder.AppendLine($"- {note}");
        }

        return builder.ToString().TrimEnd();
    }
}
