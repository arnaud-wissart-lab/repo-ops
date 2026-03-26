using System.Text.Json.Serialization;

namespace RepoOps.Worker.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ValidationDecisionType
{
    Approved,
    Rejected,
    NeedsReview
}

public sealed class ValidationResult
{
    public DateTimeOffset GeneratedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? SourceResponseGeneratedAtUtc { get; init; }

    public string SourceReportStatus { get; init; } = "Unknown";

    public string ExecutorMode { get; init; } = "Unknown";

    public ValidationSummary Summary { get; init; } = new();

    public IReadOnlyList<ValidatedAction> Decisions { get; init; } = Array.Empty<ValidatedAction>();

    public ValidationDigest Digest { get; init; } = new();

    public IReadOnlyList<string> Notes { get; init; } = Array.Empty<string>();
}

public sealed class ValidationSummary
{
    public int TotalActions { get; init; }

    public int ApprovedActions { get; init; }

    public int RejectedActions { get; init; }

    public int NeedsReviewActions { get; init; }

    public int ReadyForExecutionActions { get; init; }
}

public sealed class ValidatedAction
{
    public string ActionId { get; init; } = string.Empty;

    public string Repository { get; init; } = string.Empty;

    public int? PullRequestNumber { get; init; }

    public string PullRequestTitle { get; init; } = string.Empty;

    public string PullRequestUrl { get; init; } = string.Empty;

    public SupervisorActionPriority Priority { get; init; } = SupervisorActionPriority.Low;

    public string PromptType { get; init; } = string.Empty;

    public CodexResponseType ResponseType { get; init; } = CodexResponseType.Analysis;

    public CodexConfidenceLevel ConfidenceLevel { get; init; } = CodexConfidenceLevel.Low;

    public ValidationDecisionType Decision { get; init; } = ValidationDecisionType.NeedsReview;

    public string Comment { get; init; } = string.Empty;

    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;

    public bool RequiresHumanValidation { get; init; } = true;

    public bool ReadyForExecution { get; init; }

    public string Summary { get; init; } = string.Empty;
}

public sealed class ValidationDigest
{
    public string Subject { get; init; } = string.Empty;

    public string PlainTextBody { get; init; } = string.Empty;
}

public sealed class ValidationInputRecord
{
    public string ActionId { get; init; } = string.Empty;

    public ValidationDecisionType Decision { get; init; } = ValidationDecisionType.NeedsReview;

    public string Comment { get; init; } = string.Empty;

    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
}
