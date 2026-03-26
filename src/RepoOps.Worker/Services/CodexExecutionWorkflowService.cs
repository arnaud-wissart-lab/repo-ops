using Microsoft.Extensions.Options;
using RepoOps.Worker.Models;
using RepoOps.Worker.Options;

namespace RepoOps.Worker.Services;

public sealed class CodexExecutionWorkflowService(
    ILogger<CodexExecutionWorkflowService> logger,
    CodexExecutorService executorService,
    CodexExecutionDigestRenderer digestRenderer,
    CodexExecutionPersistenceService persistenceService,
    PromptPersistenceService promptPersistenceService,
    IOptions<CodexExecutorOptions> options)
{
    public async Task<CodexExecutionResult> RunAsync(
        GeneratedPromptResult prompts,
        bool emitJsonToStdout,
        CancellationToken cancellationToken)
    {
        var result = await executorService.ExecuteAsync(prompts, cancellationToken);
        result = new CodexExecutionResult
        {
            GeneratedAtUtc = result.GeneratedAtUtc,
            SourcePromptGeneratedAtUtc = result.SourcePromptGeneratedAtUtc,
            SourceReportStatus = result.SourceReportStatus,
            ExecutorMode = result.ExecutorMode,
            Summary = result.Summary,
            Responses = result.Responses,
            Notes = result.Notes,
            Digest = digestRenderer.Render(result)
        };

        await persistenceService.PersistAsync(result, cancellationToken);

        if (emitJsonToStdout)
        {
            Console.Out.WriteLine(persistenceService.Serialize(result));
        }

        logger.LogInformation(
            "Réponses superviseur écrites dans {OutputPath} et {DigestOutputPath}",
            options.Value.OutputPath,
            options.Value.DigestOutputPath);

        return result;
    }

    public async Task<CodexExecutionResult> RunFromPromptPathAsync(
        string? promptPath,
        bool emitJsonToStdout,
        CancellationToken cancellationToken)
    {
        var effectivePath = string.IsNullOrWhiteSpace(promptPath)
            ? options.Value.InputPromptPath
            : promptPath;
        var prompts = await promptPersistenceService.LoadAsync(effectivePath, cancellationToken);

        return await RunAsync(prompts, emitJsonToStdout, cancellationToken);
    }
}
