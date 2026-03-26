namespace RepoOps.Worker.Models;

public sealed class DeploymentExecutionResult
{
    public string Status { get; init; } = "NotTriggered";

    public string RequestedBy { get; init; } = "unknown";

    public string TargetName { get; init; } = "machine-locale";

    public bool DryRunEnabled { get; init; }

    public DateTimeOffset? StartedAtUtc { get; init; }

    public DateTimeOffset? FinishedAtUtc { get; init; }

    public double? DurationSeconds { get; init; }

    public string Command { get; init; } = string.Empty;

    public string WorkingDirectory { get; init; } = string.Empty;

    public int? ExitCode { get; init; }

    public string Summary { get; init; } = string.Empty;

    public IReadOnlyList<string> Logs { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}
