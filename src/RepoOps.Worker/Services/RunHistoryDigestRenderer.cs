using System.Text;
using RepoOps.Worker.Models;

namespace RepoOps.Worker.Services;

public sealed class RunHistoryDigestRenderer
{
    public RunHistoryDigest Render(RunHistoryViewResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var builder = new StringBuilder();
        builder.AppendLine($"Runs affichés : {result.Runs.Count}");

        if (result.Runs.Count == 0)
        {
            builder.AppendLine("Aucun run historisé.");
        }
        else
        {
            foreach (var run in result.Runs)
            {
                builder.AppendLine(
                    $"- {run.RunDateUtc:yyyy-MM-dd HH:mm:ss} UTC | {run.Status} | {run.InputSource} | durée {run.DurationMilliseconds} ms");
                builder.AppendLine(
                    $"  PR analysées : {run.Metrics.AnalyzedPullRequests}, auto-mergées : {run.Metrics.AutoMergedPullRequests}, bloquées : {run.Metrics.BlockedPullRequests}, erreurs : {run.Metrics.ErrorCount}");
                builder.AppendLine($"  Rapport : {run.ReportPath}");
            }
        }

        return new RunHistoryDigest
        {
            Subject = $"[repo-ops] Historique des runs du {DateTimeOffset.UtcNow:yyyy-MM-dd}",
            PlainTextBody = builder.ToString().TrimEnd()
        };
    }
}
