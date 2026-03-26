using Microsoft.Extensions.Options;
using RepoOps.Worker.Models;
using RepoOps.Worker.Options;

namespace RepoOps.Worker.Services;

public sealed class RunHistoryWorkflowService(
    ILogger<RunHistoryWorkflowService> logger,
    RunHistoryPersistenceService persistenceService,
    RunHistoryDigestRenderer digestRenderer,
    IOptions<RepoOpsWorkerOptions> options)
{
    public async Task<RunHistoryViewResult> RunAsync(
        int requestedCount,
        bool emitJsonToStdout,
        CancellationToken cancellationToken)
    {
        var result = await persistenceService.LoadRecentAsync(requestedCount, cancellationToken);
        result = new RunHistoryViewResult
        {
            GeneratedAtUtc = result.GeneratedAtUtc,
            RequestedCount = result.RequestedCount,
            Runs = result.Runs,
            Digest = digestRenderer.Render(result)
        };

        if (emitJsonToStdout)
        {
            Console.Out.WriteLine(persistenceService.Serialize(result));
        }
        else
        {
            Console.Out.WriteLine(result.Digest.PlainTextBody);
        }

        logger.LogInformation(
            "Consultation de l'historique effectuée sur {RunCount} run(s) depuis {IndexPath}",
            result.Runs.Count,
            options.Value.RunHistoryIndexPath);

        return result;
    }
}
