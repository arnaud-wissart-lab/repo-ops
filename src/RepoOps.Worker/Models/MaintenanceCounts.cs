namespace RepoOps.Worker.Models;

public sealed class MaintenanceCounts
{
    public int ScannedRepositories { get; init; }

    public int CreatedPullRequests { get; init; }

    public int MergedPullRequests { get; init; }

    public int FailedPullRequests { get; init; }

    public int RemainingVulnerabilities { get; init; }
}
