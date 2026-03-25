using RepoOps.Worker.Models;

namespace RepoOps.Worker.Services;

public sealed class MaintenanceReportBuilder(IConfiguration configuration)
{
    public MaintenanceRunReport Build(string inputSource)
    {
        var repositories = ResolveRepositories(configuration["RENOVATE_REPOSITORIES"]);

        return new MaintenanceRunReport
        {
            Summary = new MaintenanceExecutionSummary
            {
                InputSource = inputSource,
                RunDateUtc = DateTimeOffset.UtcNow,
                ScannedRepositories = repositories,
                Counts = new MaintenanceCounts
                {
                    ScannedRepositories = repositories.Count,
                    CreatedPullRequests = 0,
                    MergedPullRequests = 0,
                    FailedPullRequests = 0,
                    RemainingVulnerabilities = 0
                }
            },
            Messages = new MaintenanceMessages
            {
                Logs =
                [
                    "[worker] Exécution placeholder sans appel externe.",
                    "[worker] Le périmètre des dépôts provient de RENOVATE_REPOSITORIES lorsqu'il est renseigné."
                ],
                Notes =
                [
                    "Le worker .NET devient la source de vérité du reporting dans repo-ops.",
                    "Aucune interrogation GitHub réelle n'est encore implémentée à ce stade."
                ]
            },
            Recommendations = new MaintenanceRecommendations
            {
                ManualActions =
                [
                    "Brancher la collecte réelle sur l'API GitHub ou sur les journaux Renovate.",
                    "Conserver n8n dans un rôle d'orchestration et de notification.",
                    "Préparer ensuite l'intégration de sources réelles sans casser le contrat JSON."
                ]
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
