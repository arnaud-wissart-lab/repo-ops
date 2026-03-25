namespace RepoOps.Worker.Models;

public sealed class AutoMergeSummary
{
    public bool Enabled { get; init; }

    public bool DryRunEnabled { get; init; } = true;

    public string MergeMethod { get; init; } = "squash";

    public IReadOnlyList<string> ReadyForMerge { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> ManualReviewPullRequests { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> BlockedPullRequests { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> FailedPullRequests { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> AutoMergedPullRequests { get; init; } = Array.Empty<string>();

    public IReadOnlyList<PullRequestMergeEvaluation> Evaluations { get; init; } = Array.Empty<PullRequestMergeEvaluation>();
}
