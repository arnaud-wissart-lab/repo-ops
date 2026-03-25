using RepoOps.Worker.Models;
using RepoOps.Worker.Options;

namespace RepoOps.Worker.Services;

public sealed class MaintenanceReportBuilder(
    IConfiguration configuration,
    GitHubMaintenanceCollector gitHubMaintenanceCollector,
    ILogger<MaintenanceReportBuilder> logger)
{
    public async Task<MaintenanceRunReport> BuildAsync(
        string inputSource,
        CancellationToken cancellationToken)
    {
        var repositories = ResolveRepositories(configuration["RENOVATE_REPOSITORIES"]);
        var collectionResult = await gitHubMaintenanceCollector.CollectAsync(repositories, cancellationToken);

        logger.LogInformation(
            "Collecte GitHub terminée avec le statut {Status} pour {ScannedRepositoryCount} dépôt(s) scanné(s)",
            collectionResult.Status,
            collectionResult.ScannedRepositories.Count);

        return new MaintenanceRunReport
        {
            Summary = new MaintenanceExecutionSummary
            {
                Status = collectionResult.Status,
                InputSource = inputSource,
                RunDateUtc = DateTimeOffset.UtcNow,
                ScannedRepositories = collectionResult.ScannedRepositories,
                CreatedPullRequests = collectionResult.CreatedPullRequests,
                MergedPullRequests = collectionResult.MergedPullRequests,
                FailedPullRequests = collectionResult.FailedPullRequests,
                RemainingVulnerabilities = collectionResult.RemainingVulnerabilities,
                Counts = new MaintenanceCounts
                {
                    ScannedRepositories = collectionResult.ScannedRepositories.Count,
                    CreatedPullRequests = collectionResult.CreatedPullRequests.Count,
                    MergedPullRequests = collectionResult.MergedPullRequests.Count,
                    FailedPullRequests = collectionResult.FailedPullRequests.Count,
                    RemainingVulnerabilities = collectionResult.RemainingVulnerabilities.Count
                }
            },
            Messages = new MaintenanceMessages
            {
                Logs = collectionResult.Logs,
                Notes = collectionResult.Notes
            },
            Recommendations = new MaintenanceRecommendations
            {
                ManualActions = collectionResult.ManualActions
            }
        };
    }

    private static List<string> ResolveRepositories(string? repositoriesCsv)
    {
        if (string.IsNullOrWhiteSpace(repositoriesCsv))
        {
            return [];
        }

        return repositoriesCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static repository => !string.IsNullOrWhiteSpace(repository))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
