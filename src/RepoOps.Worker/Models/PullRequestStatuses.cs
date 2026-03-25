namespace RepoOps.Worker.Models;

public sealed class PullRequestStatuses
{
    public IReadOnlyList<string> ReadyForReview { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Blocked { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> FailedChecks { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> MergedRecently { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> ClosedWithoutMerge { get; init; } = Array.Empty<string>();
}
