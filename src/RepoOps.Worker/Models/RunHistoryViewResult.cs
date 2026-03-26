namespace RepoOps.Worker.Models;

public sealed class RunHistoryViewResult
{
    public DateTimeOffset GeneratedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public int RequestedCount { get; init; }

    public IReadOnlyList<RunHistoryEntry> Runs { get; init; } = Array.Empty<RunHistoryEntry>();

    public RunHistoryDigest Digest { get; init; } = new();
}

public sealed class RunHistoryIndex
{
    public DateTimeOffset UpdatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyList<RunHistoryEntry> Runs { get; init; } = Array.Empty<RunHistoryEntry>();
}

public sealed class RunHistoryEntry
{
    public string RunId { get; init; } = string.Empty;

    public DateTimeOffset RunDateUtc { get; init; } = DateTimeOffset.UtcNow;

    public string Status { get; init; } = "Unknown";

    public string Mode { get; init; } = string.Empty;

    public string InputSource { get; init; } = string.Empty;

    public long DurationMilliseconds { get; init; }

    public MaintenanceRunMetrics Metrics { get; init; } = new();

    public string ReportPath { get; init; } = string.Empty;
}

public sealed class RunHistoryDigest
{
    public string Subject { get; init; } = string.Empty;

    public string PlainTextBody { get; init; } = string.Empty;
}
