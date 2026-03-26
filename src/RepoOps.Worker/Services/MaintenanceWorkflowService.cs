using Microsoft.Extensions.Options;
using RepoOps.Worker.Models;
using RepoOps.Worker.Options;

namespace RepoOps.Worker.Services;

public sealed class MaintenanceWorkflowService(
    ILogger<MaintenanceWorkflowService> logger,
    MaintenanceReportBuilder reportBuilder,
    MaintenanceDigestRenderer digestRenderer,
    MaintenanceReportPersistenceService persistenceService,
    MaintenanceObservabilityBuilder observabilityBuilder,
    RunHistoryPersistenceService runHistoryPersistenceService,
    SupervisorDecisionWorkflowService supervisorDecisionWorkflowService,
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
        var startedAtUtc = DateTimeOffset.UtcNow;
        var runId = $"run-{startedAtUtc:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}";

        await executionLock.WaitAsync(timeoutCts.Token);

        try
        {
            using var scope = logger.BeginScope(new Dictionary<string, object>
            {
                ["RunId"] = runId,
                ["InputSource"] = request.InputSource
            });

            logger.LogInformation(
                "Début d'un cycle du worker .NET via {InputSource}, lancement Renovate demandé : {TriggerRenovateExecution}",
                request.InputSource,
                request.TriggerRenovateExecution);

            var report = await reportBuilder.BuildAsync(request, timeoutCts.Token);
            var finishedAtUtc = DateTimeOffset.UtcNow;
            var observability = observabilityBuilder.Build(report, runId, startedAtUtc, finishedAtUtc);

            report = new MaintenanceRunReport
            {
                Summary = report.Summary,
                Observability = observability,
                RenovateExecution = report.RenovateExecution,
                PullRequestStatuses = report.PullRequestStatuses,
                Vulnerabilities = report.Vulnerabilities,
                AutoMerge = report.AutoMerge,
                Messages = report.Messages,
                Recommendations = report.Recommendations,
                Digest = digestRenderer.Render(report)
            };

            await persistenceService.PersistAsync(report, timeoutCts.Token);
            await runHistoryPersistenceService.PersistAsync(report, timeoutCts.Token);
            await supervisorDecisionWorkflowService.RunAsync(report, emitJsonToStdout: false, timeoutCts.Token);

            if (emitJsonToStdout)
            {
                Console.Out.WriteLine(persistenceService.Serialize(report));
            }

            logger.LogInformation(
                "Cycle terminé avec le statut {Status} en {DurationMilliseconds} ms. PR analysées : {AnalyzedPullRequests}, auto-mergées : {AutoMergedPullRequests}, bloquées : {BlockedPullRequests}, erreurs : {ErrorCount}. Rapport écrit dans {ReportOutputPath}, texte dans {TextOutputPath} et HTML dans {HtmlOutputPath}",
                report.Summary.Status,
                report.Observability.DurationMilliseconds,
                report.Observability.Metrics.AnalyzedPullRequests,
                report.Observability.Metrics.AutoMergedPullRequests,
                report.Observability.Metrics.BlockedPullRequests,
                report.Observability.Metrics.ErrorCount,
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
