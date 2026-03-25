using Microsoft.Extensions.Options;
using RepoOps.Worker.Clients;
using RepoOps.Worker.Models;
using RepoOps.Worker.Options;

namespace RepoOps.Worker.Services;

public sealed class PullRequestAutoMergeService(
    GitHubApiClient gitHubApiClient,
    IOptions<AutoMergeOptions> options,
    ILogger<PullRequestAutoMergeService> logger)
{
    public async Task<PullRequestMergeEvaluation> ExecuteAsync(
        string owner,
        string repositoryName,
        PullRequestMergeEvaluation evaluation,
        CancellationToken cancellationToken)
    {
        if (evaluation.Decision != MergeDecision.AutoMerge)
        {
            return evaluation;
        }

        var settings = options.Value;

        if (!settings.Enabled)
        {
            return evaluation with
            {
                ActionStatus = PullRequestMergeActionStatus.NotAttempted,
                Reasons = evaluation.Reasons
                    .Concat(["Le feature flag d'exécution réelle de l'auto-merge est désactivé."])
                    .ToArray(),
                Summary = $"{evaluation.Summary} L'auto-merge exécutable est désactivé."
            };
        }

        if (settings.DryRunEnabled)
        {
            return evaluation with
            {
                ActionStatus = PullRequestMergeActionStatus.DryRun,
                Reasons = evaluation.Reasons
                    .Concat(["Le mode dry-run est actif, aucun merge réel n'a été effectué."])
                    .ToArray(),
                Summary = $"{evaluation.Summary} Dry-run actif, aucun merge réel n'a été effectué."
            };
        }

        try
        {
            var mergeResult = await gitHubApiClient.MergePullRequestAsync(
                owner,
                repositoryName,
                evaluation.Number,
                evaluation.MergeMethod,
                cancellationToken);

            if (mergeResult.Merged)
            {
                return evaluation with
                {
                    ActionStatus = PullRequestMergeActionStatus.Merged,
                    Reasons = evaluation.Reasons
                        .Concat([$"Le merge GitHub a été exécuté avec succès via la méthode {evaluation.MergeMethod}."])
                        .ToArray(),
                    Summary = $"{evaluation.Summary} Merge GitHub exécuté avec succès via la méthode {evaluation.MergeMethod}."
                };
            }

            return evaluation with
            {
                Decision = MergeDecision.Failed,
                ActionStatus = PullRequestMergeActionStatus.Failed,
                Reasons = evaluation.Reasons
                    .Concat([$"GitHub a répondu sans fusion effective : {mergeResult.Message}"])
                    .ToArray(),
                Summary = $"Le merge GitHub a répondu sans fusion effective : {mergeResult.Message}"
            };
        }
        catch (GitHubApiException exception)
        {
            logger.LogWarning(
                exception,
                "Échec de merge GitHub pour {Repository}#{PullRequestNumber}",
                evaluation.Repository,
                evaluation.Number);

            return evaluation with
            {
                Decision = MergeDecision.Failed,
                ActionStatus = PullRequestMergeActionStatus.Failed,
                Reasons = evaluation.Reasons
                    .Concat([$"La tentative de merge GitHub a échoué : {exception.Message}"])
                    .ToArray(),
                Summary = $"Le merge GitHub a échoué : {exception.Message}"
            };
        }
    }
}
