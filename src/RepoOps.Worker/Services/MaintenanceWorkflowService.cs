using Microsoft.Extensions.Options;
using RepoOps.Worker.Models;
using RepoOps.Worker.Options;

namespace RepoOps.Worker.Services;

public sealed class MaintenanceWorkflowService(
    ILogger<MaintenanceWorkflowService> logger,
    MaintenanceReportBuilder reportBuilder,
    MaintenanceDigestRenderer digestRenderer,
    MaintenanceReportPersistenceService persistenceService,
    IOptions<RepoOpsWorkerOptions> options)
{
    public async Task<MaintenanceRunReport> RunAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var report = reportBuilder.Build(settings.InputSource);

        logger.LogInformation(
            "Début d'un cycle placeholder du worker .NET pour {RepositoryCount} dépôt(s)",
            report.Summary.Counts.ScannedRepositories);

        report = new MaintenanceRunReport
        {
            Summary = report.Summary,
            Messages = report.Messages,
            Recommendations = report.Recommendations,
            Digest = digestRenderer.Render(report)
        };

        await persistenceService.PersistAsync(report, cancellationToken);

        if (settings.EmitJsonToStdout)
        {
            Console.Out.WriteLine(persistenceService.Serialize(report));
        }

        logger.LogInformation(
            "Cycle placeholder terminé, rapport écrit dans {ReportOutputPath}, texte dans {TextOutputPath} et HTML dans {HtmlOutputPath}",
            settings.ReportOutputPath,
            settings.SummaryTextOutputPath,
            settings.SummaryHtmlOutputPath);

        return report;
    }
}
