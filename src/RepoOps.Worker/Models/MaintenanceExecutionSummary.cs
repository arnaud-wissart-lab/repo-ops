namespace RepoOps.Worker.Models;

public sealed class MaintenanceExecutionSummary
{
    public string Status { get; init; } = "placeholder";

    public string Mode { get; init; } = "daily-maintenance";

    public string InputSource { get; init; } = "worker-dotnet";

    public DateTimeOffset RunDateUtc { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyList<string> ScannedRepositories { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> CreatedPullRequests { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> MergedPullRequests { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> FailedPullRequests { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> RemainingVulnerabilities { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> ManualActions { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> LogMessages { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Notes { get; init; } = Array.Empty<string>();

    public MaintenanceCounts Counts { get; init; } = new();
}
