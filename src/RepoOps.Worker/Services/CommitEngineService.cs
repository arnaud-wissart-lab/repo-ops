using System.Text.Json;
using Microsoft.Extensions.Options;
using RepoOps.Worker.Clients;
using RepoOps.Worker.Models;
using RepoOps.Worker.Options;

namespace RepoOps.Worker.Services;

public sealed class CommitEngineService(
    ILogger<CommitEngineService> logger,
    CommitWorkspaceExecutionService commitWorkspaceExecutionService,
    IOptions<CommitEngineOptions> options)
{
    public async Task<CommitExecutionResult> ExecuteAsync(
        ValidationResult validationResult,
        CodexExecutionResult codexResponses,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(validationResult);
        ArgumentNullException.ThrowIfNull(codexResponses);

        var settings = options.Value;
        var workspaceMap = await LoadWorkspaceMapAsync(settings.WorkspaceMapPath, cancellationToken);
        var responsesByActionId = codexResponses.Responses
            .ToDictionary(response => response.ActionId, StringComparer.OrdinalIgnoreCase);
        var operations = new List<CommitOperationRecord>(validationResult.Decisions.Count);

        foreach (var validatedAction in validationResult.Decisions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            operations.Add(await ExecuteValidatedActionAsync(
                validatedAction,
                responsesByActionId,
                workspaceMap,
                settings,
                cancellationToken));
        }

        var summary = new CommitExecutionSummary
        {
            TotalOperations = operations.Count,
            SuccessfulOperations = operations.Count(operation => operation.Status == CommitOperationStatus.Success),
            FailedOperations = operations.Count(operation => operation.Status == CommitOperationStatus.Failed),
            SkippedOperations = operations.Count(operation => operation.Status == CommitOperationStatus.Skipped),
            PullRequestsCreated = operations.Count(operation => !string.IsNullOrWhiteSpace(operation.PullRequestUrl)),
            DryRunOperations = operations.Count(operation => operation.DryRun)
        };

        return new CommitExecutionResult
        {
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            SourceValidationGeneratedAtUtc = validationResult.GeneratedAtUtc,
            SourceResponseGeneratedAtUtc = codexResponses.GeneratedAtUtc,
            DryRunEnabled = settings.DryRunEnabled || !settings.AllowRealExecution,
            Summary = summary,
            Operations = operations,
            Notes = BuildNotes(settings, workspaceMap)
        };
    }

    public string BuildBranchName(ValidatedAction validatedAction, CommitOperationType operationType)
    {
        var operationToken = operationType == CommitOperationType.Refactor ? "refactor" : "fix";

        if (validatedAction.PullRequestNumber is int pullRequestNumber)
        {
            return $"{options.Value.BranchPrefix}/{operationToken}-pr-{pullRequestNumber}";
        }

        var repositoryToken = validatedAction.Repository.Replace('/', '-').ToLowerInvariant();
        return $"{options.Value.BranchPrefix}/{operationToken}-{repositoryToken}";
    }

    public (string Subject, string Body) BuildCommitMessage(
        ValidatedAction validatedAction,
        CommitOperationType operationType)
    {
        var subject = operationType == CommitOperationType.Refactor
            ? "refactor(maintenance): applique le refactor validé"
            : "fix(maintenance): applique la correction validée";
        var target = validatedAction.PullRequestNumber is int pullRequestNumber
            ? $"{validatedAction.Repository}#{pullRequestNumber}"
            : validatedAction.Repository;
        var comment = string.IsNullOrWhiteSpace(validatedAction.Comment)
            ? "Aucun commentaire de validation fourni."
            : validatedAction.Comment;

        var body = string.Join(
            Environment.NewLine,
            [
                $"Action repo-ops : {validatedAction.ActionId}",
                $"Référence source : {target}",
                $"Décision humaine : {validatedAction.Decision}",
                $"Commentaire : {comment}",
                $"Résumé : {validatedAction.Summary}"
            ]);

        return (subject, body);
    }

    public string BuildPullRequestTitle(ValidatedAction validatedAction, CommitOperationType operationType)
    {
        var target = validatedAction.PullRequestNumber is int pullRequestNumber
            ? $"#{pullRequestNumber}"
            : validatedAction.Repository;

        return operationType == CommitOperationType.Refactor
            ? $"refactor(maintenance): refactor validé pour {target}"
            : $"fix(maintenance): correction validée pour {target}";
    }

    public string BuildPullRequestBody(ValidatedAction validatedAction, CommitOperationType operationType)
    {
        var operationLabel = operationType == CommitOperationType.Refactor ? "refactor" : "correction";
        var target = validatedAction.PullRequestNumber is int pullRequestNumber
            ? $"{validatedAction.Repository}#{pullRequestNumber}"
            : validatedAction.Repository;
        var comment = string.IsNullOrWhiteSpace(validatedAction.Comment)
            ? "Aucun commentaire de validation fourni."
            : validatedAction.Comment;

        return string.Join(
            Environment.NewLine,
            [
                "## Résumé",
                $"- Application contrôlée d'une {operationLabel} validée dans repo-ops",
                string.Empty,
                "## Contexte",
                $"- Action : {validatedAction.ActionId}",
                $"- Référence source : {target}",
                $"- Validation humaine : {validatedAction.Decision}",
                $"- Commentaire : {comment}",
                string.Empty,
                "## Validation",
                "- Exécution déclenchée explicitement via le Commit Engine",
                "- Aucun push direct vers main ou master",
                "- Branche dédiée obligatoire"
            ]);
    }

    public string? ResolveGuardRailFailureReason(
        ValidatedAction validatedAction,
        CodexExecutionResponse? response,
        RepositoryWorkspaceEntry? workspace,
        CommitEngineOptions settings)
    {
        if (!settings.Enabled)
        {
            return "Le Commit Engine est désactivé.";
        }

        if (validatedAction.Decision != ValidationDecisionType.Approved || !validatedAction.ReadyForExecution)
        {
            return "L'action n'a pas été approuvée explicitement pour l'exécution.";
        }

        if (response is null)
        {
            return "La réponse Codex structurée associée est introuvable.";
        }

        if (response.ResponseType == CodexResponseType.Analysis)
        {
            return "Le type de réponse est analytique et ne porte aucune modification exécutable.";
        }

        if (string.IsNullOrWhiteSpace(response.ProposedUnifiedDiff))
        {
            return "Aucun patch unifié exécutable n'est présent dans la réponse Codex.";
        }

        if (workspace is null || string.IsNullOrWhiteSpace(workspace.LocalPath))
        {
            return "Aucun workspace local n'est configuré pour ce dépôt.";
        }

        if (!Directory.Exists(workspace.LocalPath))
        {
            return $"Le workspace local '{workspace.LocalPath}' est introuvable.";
        }

        return null;
    }

    private async Task<CommitOperationRecord> ExecuteValidatedActionAsync(
        ValidatedAction validatedAction,
        IReadOnlyDictionary<string, CodexExecutionResponse> responsesByActionId,
        IReadOnlyDictionary<string, RepositoryWorkspaceEntry> workspaceMap,
        CommitEngineOptions settings,
        CancellationToken cancellationToken)
    {
        responsesByActionId.TryGetValue(validatedAction.ActionId, out var response);
        workspaceMap.TryGetValue(validatedAction.Repository, out var workspace);
        var operationType = ResolveOperationType(response);
        var branchName = BuildBranchName(validatedAction, operationType);
        var baseBranch = string.IsNullOrWhiteSpace(workspace?.BaseBranch) ? settings.DefaultBaseBranch : workspace.BaseBranch;
        var (commitSubject, commitBody) = BuildCommitMessage(validatedAction, operationType);
        var pullRequestTitle = BuildPullRequestTitle(validatedAction, operationType);
        var pullRequestBody = BuildPullRequestBody(validatedAction, operationType);

        var guardRailFailureReason = ResolveGuardRailFailureReason(validatedAction, response, workspace, settings);
        if (guardRailFailureReason is not null)
        {
            return BuildSkippedOperation(
                validatedAction,
                workspace?.LocalPath ?? string.Empty,
                branchName,
                baseBranch,
                operationType,
                commitSubject,
                commitBody,
                pullRequestTitle,
                pullRequestBody,
                guardRailFailureReason,
                dryRun: settings.DryRunEnabled || !settings.AllowRealExecution);
        }

        return await commitWorkspaceExecutionService.ExecuteAsync(
            validatedAction,
            response!,
            workspace!,
            settings,
            operationType,
            branchName,
            baseBranch,
            commitSubject,
            commitBody,
            pullRequestTitle,
            pullRequestBody,
            cancellationToken);
    }

    private static CommitOperationType ResolveOperationType(CodexExecutionResponse? response)
    {
        return response?.ResponseType == CodexResponseType.Refactor
            ? CommitOperationType.Refactor
            : CommitOperationType.Correction;
    }

    private async Task<IReadOnlyDictionary<string, RepositoryWorkspaceEntry>> LoadWorkspaceMapAsync(
        string workspaceMapPath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(workspaceMapPath))
        {
            return new Dictionary<string, RepositoryWorkspaceEntry>(StringComparer.OrdinalIgnoreCase);
        }

        var fullPath = Path.GetFullPath(workspaceMapPath);
        if (!File.Exists(fullPath))
        {
            logger.LogWarning("Le fichier de mapping des workspaces est introuvable : {WorkspaceMapPath}", fullPath);
            return new Dictionary<string, RepositoryWorkspaceEntry>(StringComparer.OrdinalIgnoreCase);
        }

        var json = await File.ReadAllTextAsync(fullPath, cancellationToken);
        var map = JsonSerializer.Deserialize<RepositoryWorkspaceMap>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        return map?.Repositories
            .GroupBy(entry => entry.Repository, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, RepositoryWorkspaceEntry>(StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<string> BuildNotes(
        CommitEngineOptions settings,
        IReadOnlyDictionary<string, RepositoryWorkspaceEntry> workspaceMap)
    {
        var notes = new List<string>
        {
            "Aucun push direct vers main ou master n'est autorisé ; le moteur travaille toujours sur une branche dédiée."
        };

        if (settings.DryRunEnabled || !settings.AllowRealExecution)
        {
            notes.Add("Le mode dry-run est actif ou l'exécution réelle est interdite ; aucune opération Git réelle n'a été effectuée.");
        }

        if (workspaceMap.Count == 0)
        {
            notes.Add("Aucun mapping de workspace local n'est disponible ; les actions réelles seront ignorées.");
        }

        return notes;
    }

    private static CommitOperationRecord BuildSkippedOperation(
        ValidatedAction validatedAction,
        string workspacePath,
        string branchName,
        string baseBranch,
        CommitOperationType operationType,
        string commitSubject,
        string commitBody,
        string pullRequestTitle,
        string pullRequestBody,
        string reason,
        bool dryRun)
    {
        return new CommitOperationRecord
        {
            ActionId = validatedAction.ActionId,
            Repository = validatedAction.Repository,
            WorkspacePath = workspacePath,
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
            Logs = [reason],
            PreCommitValidationStatus = CommitValidationStatus.NotRun,
            PreCommitValidationOutput = "Aucune validation préalable n'a été exécutée."
        };
    }

    private static CommitOperationRecord BuildFailedOperation(
        ValidatedAction validatedAction,
        string workspacePath,
        string branchName,
        string baseBranch,
        CommitOperationType operationType,
        string commitSubject,
        string commitBody,
        string pullRequestTitle,
        string pullRequestBody,
        string errorMessage,
        IReadOnlyList<string> logs)
    {
        return new CommitOperationRecord
        {
            ActionId = validatedAction.ActionId,
            Repository = validatedAction.Repository,
            WorkspacePath = workspacePath,
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
            Logs = logs,
            PreCommitValidationStatus = CommitValidationStatus.NotRun,
            PreCommitValidationOutput = "Aucune validation préalable n'a été exécutée."
        };
    }
}
