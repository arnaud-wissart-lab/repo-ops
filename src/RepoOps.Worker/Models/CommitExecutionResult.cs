using System.Text.Json.Serialization;

namespace RepoOps.Worker.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CommitOperationType
{
    Correction,
    Refactor
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CommitOperationStatus
{
    Success,
    Failed,
    Skipped
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CommitValidationStatus
{
    NotRun,
    Succeeded,
    Failed,
    Skipped
}

public sealed class CommitExecutionResult
{
    public DateTimeOffset GeneratedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? SourceValidationGeneratedAtUtc { get; init; }

    public DateTimeOffset? SourceResponseGeneratedAtUtc { get; init; }

    public bool DryRunEnabled { get; init; } = true;

    public CommitExecutionSummary Summary { get; init; } = new();

    public IReadOnlyList<CommitOperationRecord> Operations { get; init; } = Array.Empty<CommitOperationRecord>();

    public CommitExecutionDigest Digest { get; init; } = new();

    public IReadOnlyList<string> Notes { get; init; } = Array.Empty<string>();
}

public sealed class CommitExecutionSummary
{
    public int TotalOperations { get; init; }

    public int SuccessfulOperations { get; init; }

    public int FailedOperations { get; init; }

    public int SkippedOperations { get; init; }

    public int PullRequestsCreated { get; init; }

    public int DryRunOperations { get; init; }
}

public sealed class CommitOperationRecord
{
    public string ActionId { get; init; } = string.Empty;

    public string Repository { get; init; } = string.Empty;

    public string WorkspacePath { get; init; } = string.Empty;

    public string TemporaryWorkspacePath { get; init; } = string.Empty;

    public string BranchName { get; init; } = string.Empty;

    public string BaseBranch { get; init; } = string.Empty;

    public CommitOperationType OperationType { get; init; } = CommitOperationType.Correction;

    public CommitOperationStatus Status { get; init; } = CommitOperationStatus.Skipped;

    public bool DryRun { get; init; } = true;

    public string CommitSubject { get; init; } = string.Empty;

    public string CommitBody { get; init; } = string.Empty;

    public string PullRequestTitle { get; init; } = string.Empty;

    public string PullRequestBody { get; init; } = string.Empty;

    public string PullRequestUrl { get; init; } = string.Empty;

    public string ErrorMessage { get; init; } = string.Empty;

    public CommitValidationStatus PreCommitValidationStatus { get; init; } = CommitValidationStatus.NotRun;

    public string PreCommitValidationCommand { get; init; } = string.Empty;

    public string PreCommitValidationOutput { get; init; } = string.Empty;

    public IReadOnlyList<string> ModifiedFiles { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> DiffSummary { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Logs { get; init; } = Array.Empty<string>();
}

public sealed class RepositoryWorkspaceMap
{
    public IReadOnlyList<RepositoryWorkspaceEntry> Repositories { get; init; } = Array.Empty<RepositoryWorkspaceEntry>();
}

public sealed class RepositoryWorkspaceEntry
{
    public string Repository { get; init; } = string.Empty;

    public string LocalPath { get; init; } = string.Empty;

    public string BaseBranch { get; init; } = string.Empty;
}

public sealed class CommitExecutionDigest
{
    public string Subject { get; init; } = string.Empty;

    public string PlainTextBody { get; init; } = string.Empty;
}
