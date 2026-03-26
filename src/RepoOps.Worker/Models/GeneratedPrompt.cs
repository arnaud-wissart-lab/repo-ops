namespace RepoOps.Worker.Models;

public sealed class GeneratedPromptResult
{
    public DateTimeOffset GeneratedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? SourceDecisionGeneratedAtUtc { get; init; }

    public string SourceReportStatus { get; init; } = "Unknown";

    public GeneratedPromptSummary Summary { get; init; } = new();

    public IReadOnlyList<GeneratedPrompt> Prompts { get; init; } = Array.Empty<GeneratedPrompt>();

    public GeneratedPromptDigest Digest { get; init; } = new();

    public IReadOnlyList<string> Notes { get; init; } = Array.Empty<string>();
}

public sealed class GeneratedPromptSummary
{
    public int TotalPrompts { get; init; }

    public int HighPriorityPrompts { get; init; }

    public int ReviewPrompts { get; init; }

    public int FixPrompts { get; init; }

    public int ValidationPrompts { get; init; }
}

public sealed class GeneratedPrompt
{
    public SupervisorActionType ActionType { get; init; } = SupervisorActionType.Ignore;

    public string Repository { get; init; } = string.Empty;

    public int? PullRequestNumber { get; init; }

    public string PullRequestTitle { get; init; } = string.Empty;

    public string PullRequestUrl { get; init; } = string.Empty;

    public SupervisorActionPriority Priority { get; init; } = SupervisorActionPriority.Low;

    public string PromptType { get; init; } = string.Empty;

    public string PromptText { get; init; } = string.Empty;

    public PromptContext Context { get; init; } = new();
}

public sealed class PromptContext
{
    public string ProblemSummary { get; init; } = string.Empty;

    public string ChecksStatus { get; init; } = string.Empty;

    public string Recommendation { get; init; } = string.Empty;

    public bool IsSecurityRelated { get; init; }

    public string SecuritySeverity { get; init; } = string.Empty;
}

public sealed class GeneratedPromptDigest
{
    public string Subject { get; init; } = string.Empty;

    public string PlainTextBody { get; init; } = string.Empty;
}
