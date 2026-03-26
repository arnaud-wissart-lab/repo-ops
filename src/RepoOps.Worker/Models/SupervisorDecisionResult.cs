namespace RepoOps.Worker.Models;

public sealed class SupervisorDecisionResult
{
    public DateTimeOffset GeneratedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public string SourceReportStatus { get; init; } = "Unknown";

    public SupervisorDecisionSummary Summary { get; init; } = new();

    public IReadOnlyList<SupervisorAction> Actions { get; init; } = Array.Empty<SupervisorAction>();

    public SupervisorDecisionDigest Digest { get; init; } = new();

    public IReadOnlyList<string> Notes { get; init; } = Array.Empty<string>();
}

public sealed class SupervisorDecisionSummary
{
    public int TotalActions { get; init; }

    public int ReviewActions { get; init; }

    public int AutoMergeEligibleActions { get; init; }

    public int FixRequiredActions { get; init; }

    public int IgnoreActions { get; init; }

    public int HighPriorityActions { get; init; }
}

public sealed class SupervisorDecisionDigest
{
    public string Subject { get; init; } = string.Empty;

    public string PlainTextBody { get; init; } = string.Empty;
}
