using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RepoOps.Worker.Clients;
using RepoOps.Worker.Models;
using RepoOps.Worker.Options;
using RepoOps.Worker.Services;
using Xunit;

namespace RepoOps.Worker.Tests;

public sealed class CommitEngineServiceTests
{
    [Fact]
    public void BuildBranchName_UtiliseLeNumeroDePr()
    {
        using var fixture = new CommitEngineFixture();
        var action = fixture.CreateValidatedAction();

        var branchName = fixture.Service.BuildBranchName(action, CommitOperationType.Correction);

        Assert.Equal("repo-ops/fix-pr-123", branchName);
    }

    [Fact]
    public void BuildCommitMessage_ProduitUnMessageClair()
    {
        using var fixture = new CommitEngineFixture();
        var action = fixture.CreateValidatedAction();

        var message = fixture.Service.BuildCommitMessage(action, CommitOperationType.Correction);

        Assert.Equal("fix(maintenance): applique la correction validée", message.Subject);
        Assert.Contains("Action repo-ops : owner-repo-a-123-fix-required", message.Body, StringComparison.Ordinal);
        Assert.Contains("Référence source : owner/repo-a#123", message.Body, StringComparison.Ordinal);
    }

    [Fact]
    public void GuardRails_RefuseUneActionNonApprouvee()
    {
        using var fixture = new CommitEngineFixture();
        var action = new ValidatedAction
        {
            ActionId = "owner-repo-a-123-fix-required",
            Repository = "owner/repo-a",
            PullRequestNumber = 123,
            PullRequestTitle = "chore(deps): update dependency",
            PullRequestUrl = "https://github.com/owner/repo-a/pull/123",
            Priority = SupervisorActionPriority.High,
            PromptType = "fix-required",
            ResponseType = CodexResponseType.ProposedFix,
            ConfidenceLevel = CodexConfidenceLevel.Medium,
            Decision = ValidationDecisionType.NeedsReview,
            Comment = "Validation différée.",
            TimestampUtc = DateTimeOffset.UtcNow,
            RequiresHumanValidation = true,
            ReadyForExecution = false
        };

        var reason = fixture.Service.ResolveGuardRailFailureReason(
            action,
            fixture.CreateCodexResponse(),
            fixture.CreateWorkspaceEntry(),
            fixture.Settings);

        Assert.Equal("L'action n'a pas été approuvée explicitement pour l'exécution.", reason);
    }

    [Fact]
    public void GuardRails_RefuseUneReponseSansPatch()
    {
        using var fixture = new CommitEngineFixture();

        var reason = fixture.Service.ResolveGuardRailFailureReason(
            fixture.CreateValidatedAction(),
            new CodexExecutionResponse
            {
                ActionId = "owner-repo-a-123-fix-required",
                ActionType = SupervisorActionType.FixRequired,
                Repository = "owner/repo-a",
                PullRequestNumber = 123,
                PullRequestTitle = "chore(deps): update dependency",
                PullRequestUrl = "https://github.com/owner/repo-a/pull/123",
                Priority = SupervisorActionPriority.High,
                PromptType = "fix-required",
                InitialPromptText = "Prompt",
                ResponseText = "Réponse",
                ProposedUnifiedDiff = string.Empty,
                Summary = "Correctif prêt.",
                ResponseType = CodexResponseType.ProposedFix,
                ConfidenceLevel = CodexConfidenceLevel.Medium,
                RequiresHumanValidation = true,
                ReadyForExecution = true
            },
            fixture.CreateWorkspaceEntry(),
            fixture.Settings);

        Assert.Equal("Aucun patch unifié exécutable n'est présent dans la réponse Codex.", reason);
    }

    [Fact]
    public async Task ExecuteAsync_SansWorkspace_IgnoreLAction()
    {
        using var fixture = new CommitEngineFixture();
        fixture.Settings.WorkspaceMapPath = string.Empty;

        var result = await fixture.Service.ExecuteAsync(
            fixture.CreateValidationResult(),
            fixture.CreateCodexExecutionResult(),
            CancellationToken.None);

        var operation = Assert.Single(result.Operations);
        Assert.Equal(CommitOperationStatus.Skipped, operation.Status);
        Assert.Contains("Aucun workspace local", operation.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, fixture.GitRunner.CallCount);
    }

    [Fact]
    public async Task ExecuteAsync_EnDryRun_SimuleSansAppelerGit()
    {
        using var fixture = new CommitEngineFixture();

        var result = await fixture.Service.ExecuteAsync(
            fixture.CreateValidationResult(),
            fixture.CreateCodexExecutionResult(),
            CancellationToken.None);

        var operation = Assert.Single(result.Operations);
        Assert.Equal(CommitOperationStatus.Skipped, operation.Status);
        Assert.True(operation.DryRun);
        Assert.Equal(CommitValidationStatus.Succeeded, operation.PreCommitValidationStatus);
        Assert.True(fixture.GitRunner.CallCount > 0);
        Assert.Equal(1, fixture.ProcessRunner.CallCount);
        Assert.Contains(operation.Logs, log => log.Contains("Dry-run", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class CommitEngineFixture : IDisposable
    {
        private readonly string workspaceDirectory;
        private readonly string workspaceMapPath;

        public CommitEngineFixture()
        {
            workspaceDirectory = Path.Combine(Path.GetTempPath(), $"repo-ops-commit-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(workspaceDirectory);
            File.WriteAllText(Path.Combine(workspaceDirectory, "README.md"), "ancien");
            File.WriteAllText(Path.Combine(workspaceDirectory, "sample.csproj"), "<Project />");

            workspaceMapPath = Path.Combine(Path.GetTempPath(), $"repo-ops-workspaces-{Guid.NewGuid():N}.json");
            File.WriteAllText(
                workspaceMapPath,
                """
                {
                  "repositories": [
                    {
                      "repository": "owner/repo-a",
                      "localPath": "__WORKSPACE__",
                      "baseBranch": "main"
                    }
                  ]
                }
                """.Replace("__WORKSPACE__", workspaceDirectory.Replace("\\", "\\\\"), StringComparison.Ordinal));

            Settings = new CommitEngineOptions
            {
                Enabled = true,
                AllowRealExecution = false,
                DryRunEnabled = true,
                CreatePullRequest = true,
                RequireCleanWorktree = true,
                WorkspaceMapPath = workspaceMapPath,
                BranchPrefix = "repo-ops",
                PushRemote = "origin",
                DefaultBaseBranch = "main"
            };

            GitRunner = new RecordingGitCommandRunner();
            ProcessRunner = new RecordingProcessCommandRunner();
            var gitHubApiClient = new GitHubApiClient(
                new HttpClient(new ThrowOnUseHandler()),
                Microsoft.Extensions.Options.Options.Create(new GitHubOptions()),
                NullLogger<GitHubApiClient>.Instance);
            var preCommitValidationService = new PreCommitValidationService(
                ProcessRunner,
                NullLogger<PreCommitValidationService>.Instance,
                Microsoft.Extensions.Options.Options.Create(Settings));
            var workspaceExecutionService = new CommitWorkspaceExecutionService(
                NullLogger<CommitWorkspaceExecutionService>.Instance,
                GitRunner,
                gitHubApiClient,
                new CommitPatchValidationService(),
                preCommitValidationService);

            Service = new CommitEngineService(
                NullLogger<CommitEngineService>.Instance,
                workspaceExecutionService,
                Microsoft.Extensions.Options.Options.Create(Settings));
        }

        public CommitEngineOptions Settings { get; }

        public RecordingGitCommandRunner GitRunner { get; }

        public RecordingProcessCommandRunner ProcessRunner { get; }

        public CommitEngineService Service { get; }

        public void Dispose()
        {
            if (Directory.Exists(workspaceDirectory))
            {
                Directory.Delete(workspaceDirectory, recursive: true);
            }

            if (File.Exists(workspaceMapPath))
            {
                File.Delete(workspaceMapPath);
            }
        }

        public ValidatedAction CreateValidatedAction()
        {
            return new ValidatedAction
            {
                ActionId = "owner-repo-a-123-fix-required",
                Repository = "owner/repo-a",
                PullRequestNumber = 123,
                PullRequestTitle = "chore(deps): update dependency",
                PullRequestUrl = "https://github.com/owner/repo-a/pull/123",
                Priority = SupervisorActionPriority.High,
                PromptType = "fix-required",
                ResponseType = CodexResponseType.ProposedFix,
                ConfidenceLevel = CodexConfidenceLevel.Medium,
                Decision = ValidationDecisionType.Approved,
                Comment = "Validation explicite pour un dépôt pilote.",
                ReadyForExecution = true,
                Summary = "Le correctif proposé est prêt pour une exécution contrôlée."
            };
        }

        public ValidationResult CreateValidationResult()
        {
            return new ValidationResult
            {
                Decisions = [CreateValidatedAction()]
            };
        }

        public CodexExecutionResponse CreateCodexResponse()
        {
            return new CodexExecutionResponse
            {
                ActionId = "owner-repo-a-123-fix-required",
                ActionType = SupervisorActionType.FixRequired,
                Repository = "owner/repo-a",
                PullRequestNumber = 123,
                PullRequestTitle = "chore(deps): update dependency",
                PullRequestUrl = "https://github.com/owner/repo-a/pull/123",
                Priority = SupervisorActionPriority.High,
                PromptType = "fix-required",
                InitialPromptText = "Prompt",
                ResponseText = "Réponse",
                ProposedUnifiedDiff =
                    """
                    diff --git a/README.md b/README.md
                    --- a/README.md
                    +++ b/README.md
                    @@ -1 +1 @@
                    -ancien
                    +nouveau
                    """,
                Summary = "Correctif prêt.",
                ResponseType = CodexResponseType.ProposedFix,
                ConfidenceLevel = CodexConfidenceLevel.Medium,
                RequiresHumanValidation = true,
                ReadyForExecution = true
            };
        }

        public CodexExecutionResult CreateCodexExecutionResult()
        {
            return new CodexExecutionResult
            {
                Responses = [CreateCodexResponse()]
            };
        }

        public RepositoryWorkspaceEntry CreateWorkspaceEntry()
        {
            return new RepositoryWorkspaceEntry
            {
                Repository = "owner/repo-a",
                LocalPath = workspaceDirectory,
                BaseBranch = "main"
            };
        }
    }

    private sealed class RecordingGitCommandRunner : IGitCommandRunner
    {
        public int CallCount { get; private set; }

        public Task<GitCommandResult> RunAsync(
            string workingDirectory,
            string arguments,
            string? standardInput,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;

            if (arguments.StartsWith("remote get-url", StringComparison.Ordinal))
            {
                return Task.FromResult(new GitCommandResult(0, "https://github.com/owner/repo-a.git", string.Empty));
            }

            if (arguments.StartsWith("clone ", StringComparison.Ordinal))
            {
                var tempPath = ExtractLastQuotedValue(arguments);
                Directory.CreateDirectory(tempPath);
                File.WriteAllText(Path.Combine(tempPath, "README.md"), "ancien");
                File.WriteAllText(Path.Combine(tempPath, "sample.csproj"), "<Project />");
                return Task.FromResult(new GitCommandResult(0, string.Empty, string.Empty));
            }

            if (arguments == "status --porcelain")
            {
                return Task.FromResult(new GitCommandResult(0, string.Empty, string.Empty));
            }

            if (arguments.StartsWith("diff --stat", StringComparison.Ordinal))
            {
                return Task.FromResult(new GitCommandResult(0, " README.md | 2 +-", string.Empty));
            }

            if (arguments.StartsWith("diff --name-only", StringComparison.Ordinal))
            {
                return Task.FromResult(new GitCommandResult(0, "README.md", string.Empty));
            }

            return Task.FromResult(new GitCommandResult(0, string.Empty, string.Empty));
        }

        private static string ExtractLastQuotedValue(string arguments)
        {
            var lastQuote = arguments.LastIndexOf('"');
            var previousQuote = arguments.LastIndexOf('"', lastQuote - 1);
            return arguments[(previousQuote + 1)..lastQuote];
        }
    }

    private sealed class RecordingProcessCommandRunner : IProcessCommandRunner
    {
        public int CallCount { get; private set; }

        public Task<ProcessCommandResult> RunAsync(
            string fileName,
            string arguments,
            string workingDirectory,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            return Task.FromResult(new ProcessCommandResult(0, "Build succeeded.", string.Empty));
        }
    }

    private sealed class ThrowOnUseHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Aucun appel HTTP GitHub n'est attendu dans ces tests.");
        }
    }
}
