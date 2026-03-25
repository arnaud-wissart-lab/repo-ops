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
    private readonly SemaphoreSlim executionLock = new(1, 1);

    public async Task<MaintenanceRunReport> RunAsync(
        MaintenanceRunRequest request,
        bool emitJsonToStdout,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(settings.ExecutionTimeoutSeconds));

        await executionLock.WaitAsync(timeoutCts.Token);

        try
        {
            logger.LogInformation(
                "Début d'un cycle du worker .NET via {InputSource}, lancement Renovate demandé : {TriggerRenovateExecution}",
                request.InputSource,
                request.TriggerRenovateExecution);

            var report = await reportBuilder.BuildAsync(request, timeoutCts.Token);

            report = new MaintenanceRunReport
            {
                Summary = report.Summary,
                RenovateExecution = report.RenovateExecution,
                PullRequestStatuses = report.PullRequestStatuses,
                Vulnerabilities = report.Vulnerabilities,
                AutoMerge = report.AutoMerge,
                Messages = report.Messages,
                Recommendations = report.Recommendations,
                Digest = digestRenderer.Render(report)
            };

            await persistenceService.PersistAsync(report, timeoutCts.Token);

            if (emitJsonToStdout)
            {
                Console.Out.WriteLine(persistenceService.Serialize(report));
            }

            logger.LogInformation(
                "Cycle terminé, rapport écrit dans {ReportOutputPath}, texte dans {TextOutputPath} et HTML dans {HtmlOutputPath}",
                settings.ReportOutputPath,
                settings.SummaryTextOutputPath,
                settings.SummaryHtmlOutputPath);

            return report;
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested && timeoutCts.IsCancellationRequested)
        {
            throw new MaintenanceExecutionTimeoutException(
                $"Le délai d'exécution du cycle de maintenance a dépassé {settings.ExecutionTimeoutSeconds} seconde(s).",
                exception);
        }
        finally
        {
            executionLock.Release();
        }
    }
}
