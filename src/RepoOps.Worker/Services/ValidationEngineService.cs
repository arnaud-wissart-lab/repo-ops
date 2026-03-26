using RepoOps.Worker.Models;

namespace RepoOps.Worker.Services;

public sealed class ValidationEngineService(ILogger<ValidationEngineService> logger)
{
    public ValidationResult Apply(
        CodexExecutionResult responses,
        IReadOnlyList<ValidationInputRecord> decisions)
    {
        ArgumentNullException.ThrowIfNull(responses);
        ArgumentNullException.ThrowIfNull(decisions);

        var decisionsByActionId = decisions
            .GroupBy(decision => decision.ActionId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);

        var validatedActions = responses.Responses
            .Select(response => BuildValidatedAction(response, decisionsByActionId))
            .ToArray();

        var summary = new ValidationSummary
        {
            TotalActions = validatedActions.Length,
            ApprovedActions = validatedActions.Count(action => action.Decision == ValidationDecisionType.Approved),
            RejectedActions = validatedActions.Count(action => action.Decision == ValidationDecisionType.Rejected),
            NeedsReviewActions = validatedActions.Count(action => action.Decision == ValidationDecisionType.NeedsReview),
            ReadyForExecutionActions = validatedActions.Count(action => action.ReadyForExecution)
        };

        logger.LogInformation(
            "Validation humaine appliquée : {TotalActions} action(s), {ApprovedActions} approuvée(s), {NeedsReviewActions} à revoir",
            summary.TotalActions,
            summary.ApprovedActions,
            summary.NeedsReviewActions);

        return new ValidationResult
        {
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            SourceResponseGeneratedAtUtc = responses.GeneratedAtUtc,
            SourceReportStatus = responses.SourceReportStatus,
            ExecutorMode = responses.ExecutorMode,
            Summary = summary,
            Decisions = validatedActions,
            Notes = BuildNotes(responses, decisionsByActionId, validatedActions)
        };
    }

    private static ValidatedAction BuildValidatedAction(
        CodexExecutionResponse response,
        IReadOnlyDictionary<string, ValidationInputRecord> decisionsByActionId)
    {
        if (!decisionsByActionId.TryGetValue(response.ActionId, out var manualDecision))
        {
            return new ValidatedAction
            {
                ActionId = response.ActionId,
                Repository = response.Repository,
                PullRequestNumber = response.PullRequestNumber,
                PullRequestTitle = response.PullRequestTitle,
                PullRequestUrl = response.PullRequestUrl,
                Priority = response.Priority,
                PromptType = response.PromptType,
                ResponseType = response.ResponseType,
                ConfidenceLevel = response.ConfidenceLevel,
                Decision = ValidationDecisionType.NeedsReview,
                Comment = "Aucune décision humaine fournie pour cette action.",
                TimestampUtc = DateTimeOffset.UtcNow,
                RequiresHumanValidation = response.RequiresHumanValidation,
                ReadyForExecution = false,
                Summary = response.Summary
            };
        }

        return new ValidatedAction
        {
            ActionId = response.ActionId,
            Repository = response.Repository,
            PullRequestNumber = response.PullRequestNumber,
            PullRequestTitle = response.PullRequestTitle,
            PullRequestUrl = response.PullRequestUrl,
            Priority = response.Priority,
            PromptType = response.PromptType,
            ResponseType = response.ResponseType,
            ConfidenceLevel = response.ConfidenceLevel,
            Decision = manualDecision.Decision,
            Comment = manualDecision.Comment,
            TimestampUtc = manualDecision.TimestampUtc,
            RequiresHumanValidation = response.RequiresHumanValidation,
            ReadyForExecution = manualDecision.Decision == ValidationDecisionType.Approved,
            Summary = response.Summary
        };
    }

    private static IReadOnlyList<string> BuildNotes(
        CodexExecutionResult responses,
        IReadOnlyDictionary<string, ValidationInputRecord> decisionsByActionId,
        IReadOnlyList<ValidatedAction> validatedActions)
    {
        var notes = new List<string>
        {
            "La validation humaine prépare une exécution future, mais ne déclenche aucune action automatiquement.",
            "Une action approuvée devient readyForExecution=true sans exécution effective dans ce lot."
        };

        var missingDecisions = validatedActions.Count(action => action.Decision == ValidationDecisionType.NeedsReview
            && string.Equals(action.Comment, "Aucune décision humaine fournie pour cette action.", StringComparison.Ordinal));
        if (missingDecisions > 0)
        {
            notes.Add($"{missingDecisions} action(s) restent sans décision humaine explicite.");
        }

        var orphanDecisions = decisionsByActionId.Keys
            .Where(actionId => validatedActions.All(action => !string.Equals(action.ActionId, actionId, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        if (orphanDecisions.Length > 0)
        {
            notes.Add($"Le fichier de validation contient {orphanDecisions.Length} action(s) inconnue(s) ignorée(s).");
        }

        if (!string.Equals(responses.SourceReportStatus, "Success", StringComparison.OrdinalIgnoreCase))
        {
            notes.Add($"Le rapport source est en statut {responses.SourceReportStatus} ; une revue humaine renforcée est recommandée.");
        }

        return notes;
    }
}
