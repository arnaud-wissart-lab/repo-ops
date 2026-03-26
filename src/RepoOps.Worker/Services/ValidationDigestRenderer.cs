using System.Text;
using RepoOps.Worker.Models;

namespace RepoOps.Worker.Services;

public sealed class ValidationDigestRenderer
{
    public ValidationDigest Render(ValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var builder = new StringBuilder();
        builder.AppendLine($"Mode source : {result.ExecutorMode}");
        builder.AppendLine($"Actions validées : {result.Summary.TotalActions}");
        builder.AppendLine($"Approuvées : {result.Summary.ApprovedActions}");
        builder.AppendLine($"Rejetées : {result.Summary.RejectedActions}");
        builder.AppendLine($"À revoir : {result.Summary.NeedsReviewActions}");
        builder.AppendLine($"Prêtes pour exécution future : {result.Summary.ReadyForExecutionActions}");
        builder.AppendLine();
        builder.AppendLine("Décisions :");

        if (result.Decisions.Count == 0)
        {
            builder.AppendLine("- Aucune décision disponible.");
        }
        else
        {
            foreach (var decision in result.Decisions)
            {
                var target = decision.PullRequestNumber is null
                    ? decision.Repository
                    : $"{decision.Repository}#{decision.PullRequestNumber}";

                builder.Append("- ");
                builder.Append(target);
                builder.Append(" -> ");
                builder.Append(FormatDecision(decision.Decision));
                builder.Append(" | priorité ");
                builder.Append(FormatPriority(decision.Priority));
                builder.Append(" | ");
                builder.AppendLine(string.IsNullOrWhiteSpace(decision.Comment) ? decision.Summary : decision.Comment);
            }
        }

        if (result.Notes.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Notes :");

            foreach (var note in result.Notes)
            {
                builder.AppendLine($"- {note}");
            }
        }

        return new ValidationDigest
        {
            Subject = $"[repo-ops] Validations humaines du {result.GeneratedAtUtc:yyyy-MM-dd}",
            PlainTextBody = builder.ToString().TrimEnd()
        };
    }

    private static string FormatDecision(ValidationDecisionType decision) => decision switch
    {
        ValidationDecisionType.Approved => "approuvée",
        ValidationDecisionType.Rejected => "rejetée",
        _ => "à revoir"
    };

    private static string FormatPriority(SupervisorActionPriority priority) => priority switch
    {
        SupervisorActionPriority.High => "haute",
        SupervisorActionPriority.Medium => "moyenne",
        _ => "basse"
    };
}
