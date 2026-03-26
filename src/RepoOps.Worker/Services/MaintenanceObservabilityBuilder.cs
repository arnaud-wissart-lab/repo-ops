using RepoOps.Worker.Models;

namespace RepoOps.Worker.Services;

public sealed class MaintenanceObservabilityBuilder
{
    public MaintenanceObservability Build(
        MaintenanceRunReport report,
        string runId,
        DateTimeOffset startedAtUtc,
        DateTimeOffset finishedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);

        return new MaintenanceObservability
        {
            RunId = runId,
            StartedAtUtc = startedAtUtc,
            FinishedAtUtc = finishedAtUtc,
            DurationMilliseconds = (long)Math.Max(0, (finishedAtUtc - startedAtUtc).TotalMilliseconds),
            Metrics = BuildMetrics(report)
        };
    }

    private static MaintenanceRunMetrics BuildMetrics(MaintenanceRunReport report)
    {
        var analyzedPullRequests = report.AutoMerge.Evaluations.Count
            + report.PullRequestStatuses.MergedRecently.Count
            + report.PullRequestStatuses.ClosedWithoutMerge.Count;

        var errorCount = report.RenovateExecution.Errors.Count;
        if (string.Equals(report.Summary.Status, "Failed", StringComparison.OrdinalIgnoreCase))
        {
            errorCount++;
        }

        if (string.Equals(report.RenovateExecution.Status, "Failed", StringComparison.OrdinalIgnoreCase))
        {
            errorCount++;
        }

        return new MaintenanceRunMetrics
        {
            AnalyzedPullRequests = analyzedPullRequests,
            AutoMergedPullRequests = report.AutoMerge.AutoMergedPullRequests.Count,
            BlockedPullRequests = report.AutoMerge.BlockedPullRequests.Count,
            ErrorCount = errorCount
        };
    }
}
