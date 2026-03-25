namespace RepoOps.Worker.Models;

public sealed class MaintenanceRunReport
{
    public MaintenanceExecutionSummary Summary { get; init; } = new();

    public MaintenanceDigest Digest { get; init; } = new();
}
