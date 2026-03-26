using RepoOps.Worker.Clients;
using RepoOps.Worker.Models;

namespace RepoOps.Worker.Services;

public sealed class CodexExecutorService(
    ILogger<CodexExecutorService> logger,
    ICodexClient codexClient)
{
    public async Task<CodexExecutionResult> ExecuteAsync(
        GeneratedPromptResult prompts,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(prompts);

        var responses = new List<CodexExecutionResponse>(prompts.Prompts.Count);

        foreach (var prompt in prompts.Prompts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var clientResponse = await codexClient.ExecuteAsync(prompt, cancellationToken);
            var actionId = BuildActionId(prompt);
            responses.Add(new CodexExecutionResponse
            {
                ActionId = actionId,
                ActionType = prompt.ActionType,
                Repository = prompt.Repository,
                PullRequestNumber = prompt.PullRequestNumber,
                PullRequestTitle = prompt.PullRequestTitle,
                PullRequestUrl = prompt.PullRequestUrl,
                Priority = prompt.Priority,
                PromptType = prompt.PromptType,
                InitialPromptText = prompt.PromptText,
                ResponseText = clientResponse.ResponseText,
                ProposedUnifiedDiff = clientResponse.ProposedUnifiedDiff,
                Summary = clientResponse.Summary,
                ResponseType = clientResponse.ResponseType,
                ConfidenceLevel = clientResponse.ConfidenceLevel,
                RequiresHumanValidation = clientResponse.RequiresHumanValidation,
                ReadyForExecution = clientResponse.ReadyForExecution
            });
        }

        var summary = new CodexExecutionSummary
        {
            TotalResponses = responses.Count,
            AnalysisResponses = responses.Count(response => response.ResponseType == CodexResponseType.Analysis),
            ProposedFixResponses = responses.Count(response => response.ResponseType == CodexResponseType.ProposedFix),
            RefactorResponses = responses.Count(response => response.ResponseType == CodexResponseType.Refactor),
            HighConfidenceResponses = responses.Count(response => response.ConfidenceLevel == CodexConfidenceLevel.High),
            RequiresHumanValidationResponses = responses.Count(response => response.RequiresHumanValidation)
        };

        logger.LogInformation(
            "Exécuteur contrôlé terminé : {TotalResponses} réponse(s) générée(s) en mode {Mode}",
            summary.TotalResponses,
            codexClient.Mode);

        return new CodexExecutionResult
        {
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            SourcePromptGeneratedAtUtc = prompts.GeneratedAtUtc,
            SourceReportStatus = prompts.SourceReportStatus,
            ExecutorMode = codexClient.Mode,
            Summary = summary,
            Responses = responses,
            Notes = BuildNotes(prompts, responses, codexClient.Mode)
        };
    }

    private static string BuildActionId(GeneratedPrompt prompt)
    {
        var repository = string.IsNullOrWhiteSpace(prompt.Repository)
            ? "unknown-repository"
            : prompt.Repository.Replace('/', '-').ToLowerInvariant();
        var pullRequestPart = prompt.PullRequestNumber?.ToString() ?? "repo";
        var promptType = string.IsNullOrWhiteSpace(prompt.PromptType)
            ? "unknown"
            : prompt.PromptType.ToLowerInvariant();

        return $"{repository}-{pullRequestPart}-{promptType}";
    }

    private static IReadOnlyList<string> BuildNotes(
        GeneratedPromptResult prompts,
        IReadOnlyList<CodexExecutionResponse> responses,
        string mode)
    {
        var notes = new List<string>
        {
            $"Le mode d'exécution actif est {mode}. Aucun commit, aucune modification de dépôt et aucune exécution de commande ne sont déclenchés automatiquement.",
            "Toutes les réponses produites exigent une validation humaine avant toute utilisation opérationnelle."
        };

        if (responses.Count == 0)
        {
            notes.Add("Aucune réponse n'a été produite car aucun prompt exploitable n'était disponible.");
        }

        if (!string.Equals(prompts.SourceReportStatus, "Success", StringComparison.OrdinalIgnoreCase))
        {
            notes.Add($"Le rapport source est en statut {prompts.SourceReportStatus} ; les réponses doivent être lues avec prudence.");
        }

        return notes;
    }
}
