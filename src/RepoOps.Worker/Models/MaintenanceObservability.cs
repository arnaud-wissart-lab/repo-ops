namespace RepoOps.Worker.Models;

public sealed class MaintenanceObservability
{
    public string RunId { get; init; } = string.Empty;

    public DateTimeOffset StartedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset FinishedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public long DurationMilliseconds { get; init; }

    public MaintenanceRunMetrics Metrics { get; init; } = new();
}

public sealed class MaintenanceRunMetrics
{
    public int AnalyzedPullRequests { get; init; }

    public int AutoMergedPullRequests { get; init; }

    public int BlockedPullRequests { get; init; }

    public int ErrorCount { get; init; }
}
