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
                Summary = $"{evaluation.Summary} L'auto-merge exécutable est désactivé."
            };
        }

        if (settings.DryRunEnabled)
        {
            return evaluation with
            {
                ActionStatus = PullRequestMergeActionStatus.DryRun,
                Summary = $"{evaluation.Summary} Dry-run actif, aucun merge réel n'a été effectué."
            };
        }

        try
        {
            var mergeResult = await gitHubApiClient.MergePullRequestAsync(
                owner,
                repositoryName,
                evaluation.Number,
                settings.MergeMethod,
                cancellationToken);

            if (mergeResult.Merged)
            {
                return evaluation with
                {
                    ActionStatus = PullRequestMergeActionStatus.Merged,
                    Summary = $"{evaluation.Summary} Merge GitHub exécuté avec succès via la méthode {settings.MergeMethod}."
                };
            }

            return evaluation with
            {
                Decision = MergeDecision.Failed,
                ActionStatus = PullRequestMergeActionStatus.Failed,
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
                Summary = $"Le merge GitHub a échoué : {exception.Message}"
            };
        }
    }
}
