using System.Text.Json.Serialization;

namespace RepoOps.Worker.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SupervisorActionType
{
    Review,
    AutoMergeEligible,
    FixRequired,
    Ignore
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SupervisorActionPriority
{
    Low,
    Medium,
    High
}

public sealed record SupervisorAction
{
    public SupervisorActionType Type { get; init; } = SupervisorActionType.Ignore;

    public string Repository { get; init; } = string.Empty;

    public int? PullRequestNumber { get; init; }

    public string PullRequestTitle { get; init; } = string.Empty;

    public string PullRequestUrl { get; init; } = string.Empty;

    public SupervisorActionPriority Priority { get; init; } = SupervisorActionPriority.Low;

    public string Reason { get; init; } = string.Empty;
}
