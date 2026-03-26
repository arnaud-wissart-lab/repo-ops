using RepoOps.Worker.Clients;
using RepoOps.Worker.Models;
using RepoOps.Worker.Options;

namespace RepoOps.Worker.Services;

public sealed class CommitWorkspaceExecutionService(
    ILogger<CommitWorkspaceExecutionService> logger,
    IGitCommandRunner gitCommandRunner,
    GitHubApiClient gitHubApiClient,
    CommitPatchValidationService commitPatchValidationService,
    PreCommitValidationService preCommitValidationService)
{
    public async Task<CommitOperationRecord> ExecuteAsync(
        ValidatedAction validatedAction,
        CodexExecutionResponse response,
        RepositoryWorkspaceEntry workspace,
        CommitEngineOptions settings,
        CommitOperationType operationType,
        string branchName,
        string baseBranch,
        string commitSubject,
        string commitBody,
        string pullRequestTitle,
        string pullRequestBody,
        CancellationToken cancellationToken)
    {
        var logs = new List<string>();
        var temporaryWorkspacePath = string.Empty;
        IReadOnlyList<string> modifiedFiles = Array.Empty<string>();
        IReadOnlyList<string> diffSummary = Array.Empty<string>();
        var validationResult = new PreCommitValidationResult
        {
            Status = CommitValidationStatus.NotRun,
            Output = "La validation avant commit n'a pas encore été exécutée."
        };

        try
        {
            if (settings.RequireCleanWorktree)
            {
                var sourceStatus = await gitCommandRunner.RunAsync(workspace.LocalPath, "status --porcelain", null, cancellationToken);
                if (sourceStatus.ExitCode != 0)
                {
                    return BuildFailedOperation(
                        validatedAction,
                        workspace.LocalPath,
                        temporaryWorkspacePath,
                        branchName,
                        baseBranch,
                        operationType,
                        commitSubject,
                        commitBody,
                        pullRequestTitle,
                        pullRequestBody,
                        $"Impossible de lire l'état du dépôt source : {sourceStatus.StandardError}",
                        validationResult,
                        modifiedFiles,
                        diffSummary,
                        logs);
                }

                if (!string.IsNullOrWhiteSpace(sourceStatus.StandardOutput))
                {
                    return BuildSkippedOperation(
                        validatedAction,
                        workspace.LocalPath,
                        temporaryWorkspacePath,
                        branchName,
                        baseBranch,
                        operationType,
                        commitSubject,
                        commitBody,
                        pullRequestTitle,
                        pullRequestBody,
                        "Le dépôt source local n'est pas propre.",
                        settings.DryRunEnabled || !settings.AllowRealExecution,
                        validationResult,
                        modifiedFiles,
                        diffSummary,
                        ["Le dépôt source local n'est pas propre."]);
                }
            }

            var remoteUrl = await ResolveRemoteUrlAsync(workspace.LocalPath, settings.PushRemote, cancellationToken);
            temporaryWorkspacePath = CreateTemporaryWorkspacePath(settings, validatedAction);
            Directory.CreateDirectory(Path.GetDirectoryName(temporaryWorkspacePath)!);

            logs.Add($"Clone temporaire : {temporaryWorkspacePath}");
            var clone = await gitCommandRunner.RunAsync(
                workspace.LocalPath,
                $"clone --no-hardlinks --origin {Quote(settings.PushRemote)} {Quote(remoteUrl)} {Quote(temporaryWorkspacePath)}",
                null,
                cancellationToken);
            if (clone.ExitCode != 0)
            {
                return BuildFailedOperation(
                    validatedAction,
                    workspace.LocalPath,
                    temporaryWorkspacePath,
                    branchName,
                    baseBranch,
                    operationType,
                    commitSubject,
                    commitBody,
                    pullRequestTitle,
                    pullRequestBody,
                    $"Échec du clone temporaire : {clone.StandardError}",
                    validationResult,
                    modifiedFiles,
                    diffSummary,
                    logs);
            }

            logs.Add($"Fetch de {settings.PushRemote}/{baseBranch}");
            var fetch = await gitCommandRunner.RunAsync(temporaryWorkspacePath, $"fetch {Quote(settings.PushRemote)} {Quote(baseBranch)}", null, cancellationToken);
            if (fetch.ExitCode != 0)
            {
                return BuildFailedOperation(
                    validatedAction,
                    workspace.LocalPath,
                    temporaryWorkspacePath,
                    branchName,
                    baseBranch,
                    operationType,
                    commitSubject,
                    commitBody,
                    pullRequestTitle,
                    pullRequestBody,
                    $"Échec du fetch Git : {fetch.StandardError}",
                    validationResult,
                    modifiedFiles,
                    diffSummary,
                    logs);
            }

            logs.Add($"Création de branche {branchName}");
            var checkout = await gitCommandRunner.RunAsync(temporaryWorkspacePath, $"checkout -B {Quote(branchName)} {Quote($"{settings.PushRemote}/{baseBranch}")}", null, cancellationToken);
            if (checkout.ExitCode != 0)
            {
                return BuildFailedOperation(
                    validatedAction,
                    workspace.LocalPath,
                    temporaryWorkspacePath,
                    branchName,
                    baseBranch,
                    operationType,
                    commitSubject,
                    commitBody,
                    pullRequestTitle,
                    pullRequestBody,
                    $"Échec de création de branche : {checkout.StandardError}",
                    validationResult,
                    modifiedFiles,
                    diffSummary,
                    logs);
            }

            var patchValidation = commitPatchValidationService.Validate(response.ProposedUnifiedDiff, temporaryWorkspacePath);
            modifiedFiles = patchValidation.ModifiedFiles.ToArray();
            diffSummary = patchValidation.DiffSummary.ToArray();

            if (!patchValidation.IsValid)
            {
                logs.AddRange(patchValidation.Errors.Select(error => $"Patch invalide : {error}"));
                return BuildFailedOperation(
                    validatedAction,
                    workspace.LocalPath,
                    temporaryWorkspacePath,
                    branchName,
                    baseBranch,
                    operationType,
                    commitSubject,
                    commitBody,
                    pullRequestTitle,
                    pullRequestBody,
                    string.Join(" ", patchValidation.Errors),
                    validationResult,
                    modifiedFiles,
                    diffSummary,
                    logs);
            }

            logs.Add($"Fichiers ciblés : {string.Join(", ", modifiedFiles)}");

            var applyCheck = await gitCommandRunner.RunAsync(temporaryWorkspacePath, "apply --check --whitespace=nowarn -", response.ProposedUnifiedDiff, cancellationToken);
            if (applyCheck.ExitCode != 0)
            {
                return BuildFailedOperation(
                    validatedAction,
                    workspace.LocalPath,
                    temporaryWorkspacePath,
                    branchName,
                    baseBranch,
                    operationType,
                    commitSubject,
                    commitBody,
                    pullRequestTitle,
                    pullRequestBody,
                    $"Échec du contrôle du patch : {applyCheck.StandardError}",
                    validationResult,
                    modifiedFiles,
                    diffSummary,
                    logs);
            }

            logs.Add("Application contrôlée du patch unifié");
            var apply = await gitCommandRunner.RunAsync(temporaryWorkspacePath, "apply --whitespace=nowarn -", response.ProposedUnifiedDiff, cancellationToken);
            if (apply.ExitCode != 0)
            {
                return BuildFailedOperation(
                    validatedAction,
                    workspace.LocalPath,
                    temporaryWorkspacePath,
                    branchName,
                    baseBranch,
                    operationType,
                    commitSubject,
                    commitBody,
                    pullRequestTitle,
                    pullRequestBody,
                    $"Échec d'application du patch : {apply.StandardError}",
                    validationResult,
                    modifiedFiles,
                    diffSummary,
                    logs);
            }

            diffSummary = await LoadDiffSummaryAsync(temporaryWorkspacePath, diffSummary, cancellationToken);
            modifiedFiles = await LoadModifiedFilesAsync(temporaryWorkspacePath, modifiedFiles, cancellationToken);
            validationResult = await preCommitValidationService.ValidateAsync(temporaryWorkspacePath, cancellationToken);

            logs.Add($"Validation avant commit : {validationResult.Status}");
            if (!string.IsNullOrWhiteSpace(validationResult.Command))
            {
                logs.Add($"Commande de validation : {validationResult.Command}");
            }

            if (validationResult.Status == CommitValidationStatus.Failed)
            {
                return BuildFailedOperation(
                    validatedAction,
                    workspace.LocalPath,
                    temporaryWorkspacePath,
                    branchName,
                    baseBranch,
                    operationType,
                    commitSubject,
                    commitBody,
                    pullRequestTitle,
                    pullRequestBody,
                    "La validation avant commit a échoué.",
                    validationResult,
                    modifiedFiles,
                    diffSummary,
                    logs);
            }

            if (settings.DryRunEnabled || !settings.AllowRealExecution)
            {
                logs.Add($"Dry-run : création de branche {branchName}");
                logs.Add($"Dry-run : commit '{commitSubject}'");
                logs.Add($"Dry-run : push vers {settings.PushRemote}/{branchName}");

                if (settings.CreatePullRequest)
                {
                    logs.Add($"Dry-run : création de pull request '{pullRequestTitle}' vers {baseBranch}");
                }

                return BuildSkippedOperation(
                    validatedAction,
                    workspace.LocalPath,
                    temporaryWorkspacePath,
                    branchName,
                    baseBranch,
                    operationType,
                    commitSubject,
                    commitBody,
                    pullRequestTitle,
                    pullRequestBody,
                    string.Empty,
                    true,
                    validationResult,
                    modifiedFiles,
                    diffSummary,
                    logs);
            }

            return await ExecuteRealAsync(
                validatedAction,
                workspace.LocalPath,
                temporaryWorkspacePath,
                settings,
                operationType,
                branchName,
                baseBranch,
                commitSubject,
                commitBody,
                pullRequestTitle,
                pullRequestBody,
                validationResult,
                modifiedFiles,
                diffSummary,
                logs,
                cancellationToken);
        }
        catch (Exception exception)
        {
            logs.Add($"Exception : {exception.Message}");
            return BuildFailedOperation(
                validatedAction,
                workspace.LocalPath,
                temporaryWorkspacePath,
                branchName,
                baseBranch,
                operationType,
                commitSubject,
                commitBody,
                pullRequestTitle,
                pullRequestBody,
                exception.Message,
                validationResult,
                modifiedFiles,
                diffSummary,
                logs);
        }
        finally
        {
            CleanupTemporaryWorkspace(temporaryWorkspacePath, logs);
        }
    }

    private async Task<CommitOperationRecord> ExecuteRealAsync(
        ValidatedAction validatedAction,
        string sourceWorkspacePath,
        string temporaryWorkspacePath,
        CommitEngineOptions settings,
        CommitOperationType operationType,
        string branchName,
        string baseBranch,
        string commitSubject,
        string commitBody,
        string pullRequestTitle,
        string pullRequestBody,
        PreCommitValidationResult validationResult,
        IReadOnlyList<string> modifiedFiles,
        IReadOnlyList<string> diffSummary,
        List<string> logs,
        CancellationToken cancellationToken)
    {
        logs.Add("Ajout des changements à l'index Git");
        var add = await gitCommandRunner.RunAsync(temporaryWorkspacePath, "add -A", null, cancellationToken);
        if (add.ExitCode != 0)
        {
            return BuildFailedOperation(validatedAction, sourceWorkspacePath, temporaryWorkspacePath, branchName, baseBranch, operationType, commitSubject, commitBody, pullRequestTitle, pullRequestBody, $"Échec du git add : {add.StandardError}", validationResult, modifiedFiles, diffSummary, logs);
        }

        logs.Add($"Création du commit '{commitSubject}'");
        var commit = await gitCommandRunner.RunAsync(temporaryWorkspacePath, $"commit -m {Quote(commitSubject)} -m {Quote(commitBody)}", null, cancellationToken);
        if (commit.ExitCode != 0)
        {
            return BuildFailedOperation(validatedAction, sourceWorkspacePath, temporaryWorkspacePath, branchName, baseBranch, operationType, commitSubject, commitBody, pullRequestTitle, pullRequestBody, $"Échec du commit Git : {commit.StandardError}", validationResult, modifiedFiles, diffSummary, logs);
        }

        logs.Add($"Push vers {settings.PushRemote}/{branchName}");
        var push = await gitCommandRunner.RunAsync(temporaryWorkspacePath, $"push -u {Quote(settings.PushRemote)} {Quote(branchName)}", null, cancellationToken);
        if (push.ExitCode != 0)
        {
            logs.Add("La branche distante peut être partiellement créée. Une vérification manuelle est nécessaire.");
            return BuildFailedOperation(validatedAction, sourceWorkspacePath, temporaryWorkspacePath, branchName, baseBranch, operationType, commitSubject, commitBody, pullRequestTitle, pullRequestBody, $"Échec du push Git : {push.StandardError}", validationResult, modifiedFiles, diffSummary, logs);
        }

        var pullRequestUrl = string.Empty;
        if (settings.CreatePullRequest)
        {
            var parts = validatedAction.Repository.Split('/', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2)
            {
                var pullRequest = await gitHubApiClient.CreatePullRequestAsync(
                    parts[0],
                    parts[1],
                    pullRequestTitle,
                    pullRequestBody,
                    branchName,
                    baseBranch,
                    cancellationToken);

                pullRequestUrl = pullRequest.HtmlUrl;
                logs.Add($"Pull request créée : {pullRequestUrl}");
            }
        }

        return new CommitOperationRecord
        {
            ActionId = validatedAction.ActionId,
            Repository = validatedAction.Repository,
            WorkspacePath = sourceWorkspacePath,
            TemporaryWorkspacePath = temporaryWorkspacePath,
            BranchName = branchName,
            BaseBranch = baseBranch,
            OperationType = operationType,
            Status = CommitOperationStatus.Success,
            DryRun = false,
            CommitSubject = commitSubject,
            CommitBody = commitBody,
            PullRequestTitle = pullRequestTitle,
            PullRequestBody = pullRequestBody,
            PullRequestUrl = pullRequestUrl,
            PreCommitValidationStatus = validationResult.Status,
            PreCommitValidationCommand = validationResult.Command,
            PreCommitValidationOutput = validationResult.Output,
            ModifiedFiles = modifiedFiles,
            DiffSummary = diffSummary,
            Logs = logs
        };
    }

    private async Task<string> ResolveRemoteUrlAsync(string workspacePath, string remoteName, CancellationToken cancellationToken)
    {
        var remote = await gitCommandRunner.RunAsync(workspacePath, $"remote get-url {Quote(remoteName)}", null, cancellationToken);
        if (remote.ExitCode != 0 || string.IsNullOrWhiteSpace(remote.StandardOutput))
        {
            throw new InvalidOperationException($"Impossible de résoudre le remote Git '{remoteName}' depuis '{workspacePath}'.");
        }

        return remote.StandardOutput.Trim();
    }

    private async Task<IReadOnlyList<string>> LoadDiffSummaryAsync(string workspacePath, IReadOnlyList<string> fallback, CancellationToken cancellationToken)
    {
        var result = await gitCommandRunner.RunAsync(workspacePath, "diff --stat --no-color", null, cancellationToken);
        if (result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            return fallback;
        }

        return result.StandardOutput
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
    }

    private async Task<IReadOnlyList<string>> LoadModifiedFilesAsync(string workspacePath, IReadOnlyList<string> fallback, CancellationToken cancellationToken)
    {
        var result = await gitCommandRunner.RunAsync(workspacePath, "diff --name-only --no-color", null, cancellationToken);
        if (result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            return fallback;
        }

        return result.StandardOutput
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
    }

    private static string CreateTemporaryWorkspacePath(CommitEngineOptions settings, ValidatedAction validatedAction)
    {
        var rootPath = string.IsNullOrWhiteSpace(settings.TemporaryWorkspaceRootPath)
            ? Path.Combine(Path.GetTempPath(), "repo-ops-commit-engine")
            : settings.TemporaryWorkspaceRootPath;
        var safeRepository = validatedAction.Repository.Replace('/', '-').ToLowerInvariant();
        return Path.Combine(rootPath, $"{safeRepository}-{Guid.NewGuid():N}");
    }

    private void CleanupTemporaryWorkspace(string temporaryWorkspacePath, List<string> logs)
    {
        if (string.IsNullOrWhiteSpace(temporaryWorkspacePath) || !Directory.Exists(temporaryWorkspacePath))
        {
            return;
        }

        try
        {
            Directory.Delete(temporaryWorkspacePath, recursive: true);
            logs.Add("Workspace temporaire supprimé.");
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Nettoyage du workspace temporaire impossible : {TemporaryWorkspacePath}", temporaryWorkspacePath);
            logs.Add($"Nettoyage du workspace temporaire impossible : {exception.Message}");
        }
    }

    private static CommitOperationRecord BuildSkippedOperation(
        ValidatedAction validatedAction,
        string workspacePath,
        string temporaryWorkspacePath,
        string branchName,
        string baseBranch,
        CommitOperationType operationType,
        string commitSubject,
        string commitBody,
        string pullRequestTitle,
        string pullRequestBody,
        string reason,
        bool dryRun,
        PreCommitValidationResult validationResult,
        IReadOnlyList<string> modifiedFiles,
        IReadOnlyList<string> diffSummary,
        IReadOnlyList<string> logs)
    {
        return new CommitOperationRecord
        {
            ActionId = validatedAction.ActionId,
            Repository = validatedAction.Repository,
            WorkspacePath = workspacePath,
            TemporaryWorkspacePath = temporaryWorkspacePath,
            BranchName = branchName,
            BaseBranch = baseBranch,
            OperationType = operationType,
            Status = CommitOperationStatus.Skipped,
            DryRun = dryRun,
            CommitSubject = commitSubject,
            CommitBody = commitBody,
            PullRequestTitle = pullRequestTitle,
            PullRequestBody = pullRequestBody,
            ErrorMessage = reason,
            PreCommitValidationStatus = validationResult.Status,
            PreCommitValidationCommand = validationResult.Command,
            PreCommitValidationOutput = validationResult.Output,
            ModifiedFiles = modifiedFiles,
            DiffSummary = diffSummary,
            Logs = logs
        };
    }

    private static CommitOperationRecord BuildFailedOperation(
        ValidatedAction validatedAction,
        string workspacePath,
        string temporaryWorkspacePath,
        string branchName,
        string baseBranch,
        CommitOperationType operationType,
        string commitSubject,
        string commitBody,
        string pullRequestTitle,
        string pullRequestBody,
        string errorMessage,
        PreCommitValidationResult validationResult,
        IReadOnlyList<string> modifiedFiles,
        IReadOnlyList<string> diffSummary,
        IReadOnlyList<string> logs)
    {
        return new CommitOperationRecord
        {
            ActionId = validatedAction.ActionId,
            Repository = validatedAction.Repository,
            WorkspacePath = workspacePath,
            TemporaryWorkspacePath = temporaryWorkspacePath,
            BranchName = branchName,
            BaseBranch = baseBranch,
            OperationType = operationType,
            Status = CommitOperationStatus.Failed,
            DryRun = false,
            CommitSubject = commitSubject,
            CommitBody = commitBody,
            PullRequestTitle = pullRequestTitle,
            PullRequestBody = pullRequestBody,
            ErrorMessage = errorMessage,
            PreCommitValidationStatus = validationResult.Status,
            PreCommitValidationCommand = validationResult.Command,
            PreCommitValidationOutput = validationResult.Output,
            ModifiedFiles = modifiedFiles,
            DiffSummary = diffSummary,
            Logs = logs
        };
    }

    private static string Quote(string value) => $"\"{EscapeArgument(value)}\"";

    private static string EscapeArgument(string value) => value.Replace("\"", "\\\"", StringComparison.Ordinal);
}
