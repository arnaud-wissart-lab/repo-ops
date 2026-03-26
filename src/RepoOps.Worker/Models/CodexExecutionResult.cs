using System.Text.Json.Serialization;

namespace RepoOps.Worker.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CodexResponseType
{
    Analysis,
    ProposedFix,
    Refactor
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CodexConfidenceLevel
{
    Low,
    Medium,
    High
}

public sealed class CodexExecutionResult
{
    public DateTimeOffset GeneratedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? SourcePromptGeneratedAtUtc { get; init; }

    public string SourceReportStatus { get; init; } = "Unknown";

    public string ExecutorMode { get; init; } = "Stub";

    public CodexExecutionSummary Summary { get; init; } = new();

    public IReadOnlyList<CodexExecutionResponse> Responses { get; init; } = Array.Empty<CodexExecutionResponse>();

    public CodexExecutionDigest Digest { get; init; } = new();

    public IReadOnlyList<string> Notes { get; init; } = Array.Empty<string>();
}

public sealed class CodexExecutionSummary
{
    public int TotalResponses { get; init; }

    public int AnalysisResponses { get; init; }

    public int ProposedFixResponses { get; init; }

    public int RefactorResponses { get; init; }

    public int HighConfidenceResponses { get; init; }

    public int RequiresHumanValidationResponses { get; init; }
}

public sealed class CodexExecutionResponse
{
    public string ActionId { get; init; } = string.Empty;

    public SupervisorActionType ActionType { get; init; } = SupervisorActionType.Ignore;

    public string Repository { get; init; } = string.Empty;

    public int? PullRequestNumber { get; init; }

    public string PullRequestTitle { get; init; } = string.Empty;

    public string PullRequestUrl { get; init; } = string.Empty;

    public SupervisorActionPriority Priority { get; init; } = SupervisorActionPriority.Low;

    public string PromptType { get; init; } = string.Empty;

    public string InitialPromptText { get; init; } = string.Empty;

    public string ResponseText { get; init; } = string.Empty;

    public string ProposedUnifiedDiff { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public CodexResponseType ResponseType { get; init; } = CodexResponseType.Analysis;

    public CodexConfidenceLevel ConfidenceLevel { get; init; } = CodexConfidenceLevel.Low;

    public bool RequiresHumanValidation { get; init; } = true;

    public bool ReadyForExecution { get; init; }
}

public sealed class CodexExecutionDigest
{
    public string Subject { get; init; } = string.Empty;

    public string PlainTextBody { get; init; } = string.Empty;
}

public sealed class CodexClientResponse
{
    public string ResponseText { get; init; } = string.Empty;

    public string ProposedUnifiedDiff { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public CodexResponseType ResponseType { get; init; } = CodexResponseType.Analysis;

    public CodexConfidenceLevel ConfidenceLevel { get; init; } = CodexConfidenceLevel.Medium;

    public bool RequiresHumanValidation { get; init; } = true;

    public bool ReadyForExecution { get; init; }
}
