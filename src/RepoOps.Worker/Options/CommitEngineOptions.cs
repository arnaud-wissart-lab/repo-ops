using RepoOps.Worker.Models;

namespace RepoOps.Worker.Options;

public sealed class CommitEngineOptions
{
    public const string SectionName = "RepoOps:Commit";

    public bool Enabled { get; set; } = true;

    public bool AllowRealExecution { get; set; }

    public bool DryRunEnabled { get; set; } = true;

    public bool CreatePullRequest { get; set; } = true;

    public bool RequireCleanWorktree { get; set; } = true;

    public string OutputPath { get; set; } = "reports/supervisor-commit-executions.json";

    public string DigestOutputPath { get; set; } = "reports/supervisor-commit-executions.txt";

    public string InputResponsePath { get; set; } = "reports/supervisor-codex-responses.json";

    public string InputValidationPath { get; set; } = "reports/supervisor-validations.json";

    public string WorkspaceMapPath { get; set; } = string.Empty;

    public string TemporaryWorkspaceRootPath { get; set; } = string.Empty;

    public string BranchPrefix { get; set; } = "repo-ops";

    public string PushRemote { get; set; } = "origin";

    public string DefaultBaseBranch { get; set; } = "main";

    public bool PreCommitValidationEnabled { get; set; } = true;

    public string PreCommitValidationCommand { get; set; } = string.Empty;

    public string PreCommitValidationArguments { get; set; } = string.Empty;

    public int PreCommitValidationTimeoutSeconds { get; set; } = 600;
}

public sealed class CommitPatchValidationResult
{
    public bool IsValid { get; init; }

    public IReadOnlyList<string> ModifiedFiles { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> DiffSummary { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}

public sealed class PreCommitValidationResult
{
    public CommitValidationStatus Status { get; init; } = CommitValidationStatus.NotRun;

    public string Command { get; init; } = string.Empty;

    public string Output { get; init; } = string.Empty;
}
