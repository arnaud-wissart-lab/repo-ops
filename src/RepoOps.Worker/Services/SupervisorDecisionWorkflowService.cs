using Microsoft.Extensions.Options;
using RepoOps.Worker.Models;
using RepoOps.Worker.Options;

namespace RepoOps.Worker.Services;

public sealed class SupervisorDecisionWorkflowService(
    ILogger<SupervisorDecisionWorkflowService> logger,
    SupervisorDecisionEngine decisionEngine,
    SupervisorDecisionDigestRenderer digestRenderer,
    SupervisorDecisionPersistenceService persistenceService,
    MaintenanceReportPersistenceService reportPersistenceService,
    IOptions<RepoOpsWorkerOptions> options)
{
    public async Task<SupervisorDecisionResult> RunAsync(
        MaintenanceRunReport report,
        bool emitJsonToStdout,
        CancellationToken cancellationToken)
    {
        var result = decisionEngine.Evaluate(report);
        result = new SupervisorDecisionResult
        {
            GeneratedAtUtc = result.GeneratedAtUtc,
            SourceReportStatus = result.SourceReportStatus,
            Summary = result.Summary,
            Actions = result.Actions,
            Notes = result.Notes,
            Digest = digestRenderer.Render(result)
        };

        await persistenceService.PersistAsync(result, cancellationToken);

        if (emitJsonToStdout)
        {
            Console.Out.WriteLine(persistenceService.Serialize(result));
        }

        logger.LogInformation(
            "Décisions superviseur écrites dans {SupervisorOutputPath} et {SupervisorDigestOutputPath}",
            options.Value.SupervisorOutputPath,
            options.Value.SupervisorDigestOutputPath);

        return result;
    }

    public async Task<SupervisorDecisionResult> RunFromReportPathAsync(
        string? reportPath,
        bool emitJsonToStdout,
        CancellationToken cancellationToken)
    {
        var effectivePath = string.IsNullOrWhiteSpace(reportPath)
            ? options.Value.ReportOutputPath
            : reportPath;
        var report = await reportPersistenceService.LoadAsync(effectivePath, cancellationToken);

        return await RunAsync(report, emitJsonToStdout, cancellationToken);
    }
}
