namespace RepoOps.Worker.Models;

public sealed class RenovateExecutionDetails
{
    public string Status { get; init; } = "NotTriggered";

    public bool TriggerRequested { get; init; }

    public bool IncludedFromLatestKnownExecution { get; init; }

    public DateTimeOffset? StartedAtUtc { get; init; }

    public DateTimeOffset? FinishedAtUtc { get; init; }

    public double? DurationSeconds { get; init; }

    public string Mode { get; init; } = "daily-report";

    public string Command { get; init; } = string.Empty;

    public int? ExitCode { get; init; }

    public string Summary { get; init; } = string.Empty;

    public IReadOnlyList<string> Logs { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}
