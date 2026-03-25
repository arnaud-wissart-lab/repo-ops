using RepoOps.Worker.Models;
using RepoOps.Worker.Options;
using Microsoft.Extensions.Options;

namespace RepoOps.Worker.Services;

public sealed class MaintenanceReportBuilder(
    IConfiguration configuration,
    IOptions<RepoOpsWorkerOptions> workerOptions,
    GitHubMaintenanceCollector gitHubMaintenanceCollector,
    RenovateExecutionService renovateExecutionService,
    ILogger<MaintenanceReportBuilder> logger)
{
    public async Task<MaintenanceRunReport> BuildAsync(
        string inputSource,
        CancellationToken cancellationToken)
    {
        var settings = workerOptions.Value;
        var repositories = ResolveRepositories(configuration["RENOVATE_REPOSITORIES"]);
        var renovateExecution = await renovateExecutionService.ResolveAsync(
            settings.TriggerRenovateExecution,
            cancellationToken);
        var collectionResult = await gitHubMaintenanceCollector.CollectAsync(repositories, cancellationToken);

        logger.LogInformation(
            "Collecte GitHub terminée avec le statut {Status} pour {ScannedRepositoryCount} dépôt(s) scanné(s)",
            collectionResult.Status,
            collectionResult.ScannedRepositories.Count);
        logger.LogInformation(
            "Synthèse Renovate résolue avec le statut {Status} en mode {Mode}",
            renovateExecution.Status,
            renovateExecution.Mode);

        var notes = collectionResult.Notes.ToList();
        var logs = collectionResult.Logs.ToList();
        var manualActions = collectionResult.ManualActions.ToList();

        logs.AddRange(renovateExecution.Logs.Select(log => $"[renovate] {log}"));
        logs.AddRange(renovateExecution.Errors.Select(error => $"[renovate][stderr] {error}"));

        notes.Add(renovateExecution.Summary);

        if (!renovateExecution.TriggerRequested)
        {
            notes.Add("Le cycle quotidien n'a pas déclenché Renovate automatiquement ; le rapport réutilise l'état connu le plus récent lorsqu'il est disponible.");
        }

        if (string.Equals(renovateExecution.Status, "Failed", StringComparison.OrdinalIgnoreCase))
        {
            manualActions.Add("Analyser l'exécution explicite de Renovate avant de considérer le scan comme exploitable.");
        }

        if (string.Equals(renovateExecution.Status, "NotTriggered", StringComparison.OrdinalIgnoreCase))
        {
            manualActions.Add("Déclencher explicitement Renovate si vous attendez un scan de dépendances actualisé.");
        }

        var globalStatus = ResolveGlobalStatus(collectionResult.Status, renovateExecution);

        return new MaintenanceRunReport
        {
            Summary = new MaintenanceExecutionSummary
            {
                Status = globalStatus,
                Mode = settings.TriggerRenovateExecution
                    ? "maintenance-with-explicit-renovate"
                    : "daily-maintenance",
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
            RenovateExecution = renovateExecution,
            PullRequestStatuses = collectionResult.PullRequestStatuses,
            Messages = new MaintenanceMessages
            {
                Logs = logs.Distinct(StringComparer.Ordinal).ToArray(),
                Notes = notes.Distinct(StringComparer.Ordinal).ToArray()
            },
            Recommendations = new MaintenanceRecommendations
            {
                ManualActions = manualActions.Distinct(StringComparer.Ordinal).ToArray()
            }
        };
    }

    private static string ResolveGlobalStatus(
        string gitHubStatus,
        RenovateExecutionDetails renovateExecution)
    {
        if (string.Equals(gitHubStatus, "Failed", StringComparison.OrdinalIgnoreCase))
        {
            return "Failed";
        }

        if (string.Equals(gitHubStatus, "Partial", StringComparison.OrdinalIgnoreCase))
        {
            return "Partial";
        }

        if (renovateExecution.TriggerRequested
            && string.Equals(renovateExecution.Status, "Failed", StringComparison.OrdinalIgnoreCase))
        {
            return "Partial";
        }

        return "Success";
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
