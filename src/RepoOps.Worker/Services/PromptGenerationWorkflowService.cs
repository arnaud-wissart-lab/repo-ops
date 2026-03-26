using Microsoft.Extensions.Options;
using RepoOps.Worker.Models;
using RepoOps.Worker.Options;

namespace RepoOps.Worker.Services;

public sealed class PromptGenerationWorkflowService(
    ILogger<PromptGenerationWorkflowService> logger,
    PromptGeneratorService promptGeneratorService,
    PromptDigestRenderer digestRenderer,
    PromptPersistenceService persistenceService,
    SupervisorDecisionPersistenceService decisionPersistenceService,
    MaintenanceReportPersistenceService reportPersistenceService,
    SupervisorDecisionEngine decisionEngine,
    IOptions<RepoOpsWorkerOptions> options)
{
    public async Task<GeneratedPromptResult> RunAsync(
        SupervisorDecisionResult decisions,
        bool emitJsonToStdout,
        CancellationToken cancellationToken)
    {
        var result = promptGeneratorService.Generate(decisions);
        result = new GeneratedPromptResult
        {
            GeneratedAtUtc = result.GeneratedAtUtc,
            SourceDecisionGeneratedAtUtc = result.SourceDecisionGeneratedAtUtc,
            SourceReportStatus = result.SourceReportStatus,
            Summary = result.Summary,
            Prompts = result.Prompts,
            Notes = result.Notes,
            Digest = digestRenderer.Render(result)
        };

        await persistenceService.PersistAsync(result, cancellationToken);

        if (emitJsonToStdout)
        {
            Console.Out.WriteLine(persistenceService.Serialize(result));
        }

        logger.LogInformation(
            "Prompts superviseur écrits dans {PromptOutputPath} et {PromptDigestOutputPath}",
            options.Value.SupervisorPromptOutputPath,
            options.Value.SupervisorPromptDigestOutputPath);

        return result;
    }

    public async Task<GeneratedPromptResult> RunFromDecisionPathAsync(
        string? decisionsPath,
        bool emitJsonToStdout,
        CancellationToken cancellationToken)
    {
        var effectivePath = string.IsNullOrWhiteSpace(decisionsPath)
            ? options.Value.SupervisorOutputPath
            : decisionsPath;
        var decisions = await decisionPersistenceService.LoadAsync(effectivePath, cancellationToken);

        return await RunAsync(decisions, emitJsonToStdout, cancellationToken);
    }

    public async Task<GeneratedPromptResult> RunFromReportPathAsync(
        string? reportPath,
        bool emitJsonToStdout,
        CancellationToken cancellationToken)
    {
        var effectivePath = string.IsNullOrWhiteSpace(reportPath)
            ? options.Value.ReportOutputPath
            : reportPath;
        var report = await reportPersistenceService.LoadAsync(effectivePath, cancellationToken);
        var decisions = decisionEngine.Evaluate(report);

        return await RunAsync(decisions, emitJsonToStdout, cancellationToken);
    }
}
