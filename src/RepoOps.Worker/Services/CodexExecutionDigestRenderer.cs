using System.Text;
using RepoOps.Worker.Models;

namespace RepoOps.Worker.Services;

public sealed class CodexExecutionDigestRenderer
{
    public CodexExecutionDigest Render(CodexExecutionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var builder = new StringBuilder();
        builder.AppendLine($"Mode d'exécution : {result.ExecutorMode}");
        builder.AppendLine($"Réponses générées : {result.Summary.TotalResponses}");
        builder.AppendLine($"Analyses : {result.Summary.AnalysisResponses}");
        builder.AppendLine($"Corrections proposées : {result.Summary.ProposedFixResponses}");
        builder.AppendLine($"Refactorisations : {result.Summary.RefactorResponses}");
        builder.AppendLine($"Validation humaine requise : {result.Summary.RequiresHumanValidationResponses}");
        builder.AppendLine();
        builder.AppendLine("Réponses prêtes à relire :");

        if (result.Responses.Count == 0)
        {
            builder.AppendLine("- Aucune réponse disponible.");
        }
        else
        {
            foreach (var response in result.Responses)
            {
                var target = response.PullRequestNumber is null
                    ? response.Repository
                    : $"{response.Repository}#{response.PullRequestNumber}";

                builder.Append("- ");
                builder.Append(target);
                builder.Append(" -> ");
                builder.Append(FormatResponseType(response.ResponseType));
                builder.Append(" | priorité ");
                builder.Append(FormatPriority(response.Priority));
                builder.Append(" | confiance ");
                builder.Append(FormatConfidenceLevel(response.ConfidenceLevel));
                builder.Append(" | ");
                builder.AppendLine(response.Summary);
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

        return new CodexExecutionDigest
        {
            Subject = $"[repo-ops] Réponses superviseur du {result.GeneratedAtUtc:yyyy-MM-dd}",
            PlainTextBody = builder.ToString().TrimEnd()
        };
    }

    private static string FormatResponseType(CodexResponseType responseType) => responseType switch
    {
        CodexResponseType.ProposedFix => "correction proposée",
        CodexResponseType.Refactor => "refactorisation",
        _ => "analyse"
    };

    private static string FormatPriority(SupervisorActionPriority priority) => priority switch
    {
        SupervisorActionPriority.High => "haute",
        SupervisorActionPriority.Medium => "moyenne",
        _ => "basse"
    };

    private static string FormatConfidenceLevel(CodexConfidenceLevel confidenceLevel) => confidenceLevel switch
    {
        CodexConfidenceLevel.High => "élevée",
        CodexConfidenceLevel.Medium => "moyenne",
        _ => "faible"
    };
}
