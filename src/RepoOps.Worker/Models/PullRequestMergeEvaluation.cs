using System.Text.Json.Serialization;

namespace RepoOps.Worker.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MergeDecision
{
    AutoMerge,
    ManualReview,
    Blocked,
    Failed
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PullRequestVersionType
{
    Unknown,
    Patch,
    Minor,
    Major
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PullRequestChecksStatus
{
    Unknown,
    Success,
    Pending,
    Failed
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PullRequestMergeActionStatus
{
    NotAttempted,
    DryRun,
    Merged,
    Failed
}

public sealed record PullRequestMergeEvaluation
{
    public string Repository { get; init; } = string.Empty;

    public int Number { get; init; }

    public string Title { get; init; } = string.Empty;

    public string HtmlUrl { get; init; } = string.Empty;

    public PullRequestVersionType VersionType { get; init; } = PullRequestVersionType.Unknown;

    public PullRequestChecksStatus ChecksStatus { get; init; } = PullRequestChecksStatus.Unknown;

    public bool? Mergeable { get; init; }

    public string MergeableState { get; init; } = string.Empty;

    public MergeDecision Decision { get; init; } = MergeDecision.ManualReview;

    public PullRequestMergeActionStatus ActionStatus { get; init; } = PullRequestMergeActionStatus.NotAttempted;

    public string Summary { get; init; } = string.Empty;
}
