using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RepoOps.Worker.Models;
using RepoOps.Worker.Options;

namespace RepoOps.Worker.Services;

public sealed class MaintenanceWorkflowService(
    ILogger<MaintenanceWorkflowService> logger,
    IConfiguration configuration,
    IOptions<RepoOpsWorkerOptions> options)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<MaintenanceRunReport> RunAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var repositories = ResolveRepositories(configuration["RENOVATE_REPOSITORIES"]);

        logger.LogInformation(
            "Début d'un cycle placeholder du worker .NET pour {RepositoryCount} dépôt(s)",
            repositories.Count);

        var summary = new MaintenanceExecutionSummary
        {
            InputSource = settings.InputSource,
            RunDateUtc = DateTimeOffset.UtcNow,
            ScannedRepositories = repositories,
            ManualActions =
            [
                "Brancher la collecte réelle sur l'API GitHub ou sur les journaux Renovate.",
                "Remplacer progressivement les scripts de transition par des services .NET ciblés.",
                "Connecter plus tard la synthèse du worker au workflow n8n pour l'envoi d'email."
            ],
            LogMessages =
            [
                "[worker] Exécution placeholder sans appel externe.",
                "[worker] Le périmètre des dépôts provient de RENOVATE_REPOSITORIES lorsqu'il est renseigné."
            ],
            Notes =
            [
                "Le worker .NET devient la future couche métier pour la collecte, la consolidation et la synthèse.",
                "Aucune interrogation GitHub réelle n'est encore implémentée à ce stade."
            ],
            Counts = new MaintenanceCounts
            {
                ScannedRepositories = repositories.Count,
                CreatedPullRequests = 0,
                MergedPullRequests = 0,
                FailedPullRequests = 0,
                RemainingVulnerabilities = 0
            }
        };

        var digest = new MaintenanceDigest
        {
            Subject = $"[repo-ops] Synthèse placeholder du {summary.RunDateUtc:yyyy-MM-dd}",
            PlainTextBody = BuildPlainTextDigest(summary)
        };

        var report = new MaintenanceRunReport
        {
            Summary = summary,
            Digest = digest
        };

        await PersistReportAsync(report, settings, cancellationToken);

        logger.LogInformation(
            "Cycle placeholder terminé, rapport écrit dans {ReportOutputPath}",
            settings.ReportOutputPath);

        return report;
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

    private static string BuildPlainTextDigest(MaintenanceExecutionSummary summary)
    {
        var builder = new StringBuilder();

        builder.AppendLine("Synthèse placeholder repo-ops");
        builder.AppendLine($"Date d'exécution : {summary.RunDateUtc:O}");
        builder.AppendLine(
            $"Dépôts scannés : {(summary.ScannedRepositories.Count > 0 ? string.Join(", ", summary.ScannedRepositories) : "aucun dépôt configuré")}");
        builder.AppendLine($"PR créées : {summary.Counts.CreatedPullRequests}");
        builder.AppendLine($"PR fusionnées : {summary.Counts.MergedPullRequests}");
        builder.AppendLine($"PR en échec : {summary.Counts.FailedPullRequests}");
        builder.AppendLine($"Vulnérabilités restantes : {summary.Counts.RemainingVulnerabilities}");
        builder.AppendLine("Actions manuelles recommandées :");

        foreach (var action in summary.ManualActions)
        {
            builder.AppendLine($"- {action}");
        }

        return builder.ToString().TrimEnd();
    }

    private static async Task PersistReportAsync(
        MaintenanceRunReport report,
        RepoOpsWorkerOptions settings,
        CancellationToken cancellationToken)
    {
        await WriteFileAsync(
            settings.ReportOutputPath,
            JsonSerializer.Serialize(report, JsonOptions),
            cancellationToken);

        await WriteFileAsync(
            settings.SummaryTextOutputPath,
            report.Digest.PlainTextBody,
            cancellationToken);
    }

    private static async Task WriteFileAsync(
        string outputPath,
        string content,
        CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(outputPath);
        var directoryPath = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        await File.WriteAllTextAsync(fullPath, content, cancellationToken);
    }
}
