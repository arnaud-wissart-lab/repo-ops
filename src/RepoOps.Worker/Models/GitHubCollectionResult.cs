namespace RepoOps.Worker.Models;

public sealed class GitHubCollectionResult
{
    public string Status { get; init; } = "Failed";

    public IReadOnlyList<string> ScannedRepositories { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> CreatedPullRequests { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> MergedPullRequests { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> FailedPullRequests { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> RemainingVulnerabilities { get; init; } = Array.Empty<string>();

    public PullRequestStatuses PullRequestStatuses { get; init; } = new();

    public AutoMergeSummary AutoMerge { get; init; } = new();

    public IReadOnlyList<string> Logs { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Notes { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> ManualActions { get; init; } = Array.Empty<string>();
}
