using System.Text;
using RepoOps.Worker.Models;

namespace RepoOps.Worker.Services;

public sealed class SupervisorDecisionDigestRenderer
{
    public SupervisorDecisionDigest Render(SupervisorDecisionResult result)
    {
        return new SupervisorDecisionDigest
        {
            Subject = $"[repo-ops] Décisions superviseur du {result.GeneratedAtUtc:yyyy-MM-dd}",
            PlainTextBody = BuildPlainText(result)
        };
    }

    private static string BuildPlainText(SupervisorDecisionResult result)
    {
        var builder = new StringBuilder();

        builder.AppendLine("Décisions superviseur repo-ops");
        builder.AppendLine($"Date de génération : {result.GeneratedAtUtc:O}");
        builder.AppendLine($"Statut du rapport source : {result.SourceReportStatus}");
        builder.AppendLine();
        builder.AppendLine("Compteurs :");
        builder.AppendLine($"- Actions totales : {result.Summary.TotalActions}");
        builder.AppendLine($"- Revue manuelle : {result.Summary.ReviewActions}");
        builder.AppendLine($"- Auto-merge éligible : {result.Summary.AutoMergeEligibleActions}");
        builder.AppendLine($"- Correctif requis : {result.Summary.FixRequiredActions}");
        builder.AppendLine($"- Ignorées : {result.Summary.IgnoreActions}");
        builder.AppendLine($"- Priorité haute : {result.Summary.HighPriorityActions}");
        builder.AppendLine();

        AppendSection(
            builder,
            "Actions prioritaires",
            result.Actions.Where(action => action.Priority == SupervisorActionPriority.High).ToArray(),
            "Aucune action prioritaire.");
        builder.AppendLine();

        AppendSection(
            builder,
            "Actions éligibles à l'auto-merge",
            result.Actions.Where(action => action.Type == SupervisorActionType.AutoMergeEligible).ToArray(),
            "Aucune action éligible à l'auto-merge.");
        builder.AppendLine();

        AppendSection(
            builder,
            "Actions de revue",
            result.Actions.Where(action => action.Type == SupervisorActionType.Review).ToArray(),
            "Aucune action de revue.");
        builder.AppendLine();

        AppendSection(
            builder,
            "Correctifs requis",
            result.Actions.Where(action => action.Type == SupervisorActionType.FixRequired).ToArray(),
            "Aucun correctif requis.");
        builder.AppendLine();

        AppendSection(
            builder,
            "Actions ignorées",
            result.Actions.Where(action => action.Type == SupervisorActionType.Ignore).ToArray(),
            "Aucune action ignorée.");
        builder.AppendLine();
        builder.AppendLine("Notes :");

        foreach (var note in result.Notes)
        {
            builder.AppendLine($"- {note}");
        }

        return builder.ToString().TrimEnd();
    }

    private static void AppendSection(
        StringBuilder builder,
        string title,
        IReadOnlyList<SupervisorAction> actions,
        string fallback)
    {
        builder.AppendLine($"{title} :");

        if (actions.Count == 0)
        {
            builder.AppendLine($"- {fallback}");
            return;
        }

        foreach (var action in actions)
        {
            var target = action.PullRequestNumber is null
                ? action.Repository
                : $"{action.Repository}#{action.PullRequestNumber}";

            builder.AppendLine($"- [{action.Priority}] {target} - {action.Type} - {action.Reason}");
        }
    }
}
