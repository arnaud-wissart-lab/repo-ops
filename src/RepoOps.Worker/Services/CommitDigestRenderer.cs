using System.Text;
using RepoOps.Worker.Models;

namespace RepoOps.Worker.Services;

public sealed class CommitDigestRenderer
{
    public CommitExecutionDigest Render(CommitExecutionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var builder = new StringBuilder();
        builder.AppendLine($"Dry-run : {result.DryRunEnabled}");
        builder.AppendLine($"Opérations : {result.Summary.TotalOperations}");
        builder.AppendLine($"Succès : {result.Summary.SuccessfulOperations}");
        builder.AppendLine($"Échecs : {result.Summary.FailedOperations}");
        builder.AppendLine($"Ignorées : {result.Summary.SkippedOperations}");
        builder.AppendLine($"Pull requests créées : {result.Summary.PullRequestsCreated}");
        builder.AppendLine();
        builder.AppendLine("Opérations traitées :");

        if (result.Operations.Count == 0)
        {
            builder.AppendLine("- Aucune opération.");
        }
        else
        {
            foreach (var operation in result.Operations)
            {
                builder.Append("- ");
                builder.Append(operation.Repository);
                builder.Append(" -> ");
                builder.Append(FormatStatus(operation.Status));
                builder.Append(" | branche ");
                builder.Append(operation.BranchName);

                if (!string.IsNullOrWhiteSpace(operation.ErrorMessage))
                {
                    builder.Append(" | ");
                    builder.Append(operation.ErrorMessage);
                }

                builder.AppendLine();
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

        return new CommitExecutionDigest
        {
            Subject = $"[repo-ops] Exécutions Git du {result.GeneratedAtUtc:yyyy-MM-dd}",
            PlainTextBody = builder.ToString().TrimEnd()
        };
    }

    private static string FormatStatus(CommitOperationStatus status) => status switch
    {
        CommitOperationStatus.Success => "succès",
        CommitOperationStatus.Failed => "échec",
        _ => "ignorée"
    };
}
