namespace RepoOps.Worker.Models;

public sealed class MaintenanceRunReport
{
    public MaintenanceExecutionSummary Summary { get; init; } = new();

    public RenovateExecutionDetails RenovateExecution { get; init; } = new();

    public PullRequestStatuses PullRequestStatuses { get; init; } = new();

    public AutoMergeSummary AutoMerge { get; init; } = new();

    public MaintenanceDigest Digest { get; init; } = new();

    public MaintenanceMessages Messages { get; init; } = new();

    public MaintenanceRecommendations Recommendations { get; init; } = new();
}
