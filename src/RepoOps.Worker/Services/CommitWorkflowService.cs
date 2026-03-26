using Microsoft.Extensions.Options;
using RepoOps.Worker.Models;
using RepoOps.Worker.Options;

namespace RepoOps.Worker.Services;

public sealed class CommitWorkflowService(
    ILogger<CommitWorkflowService> logger,
    CommitEngineService commitEngineService,
    CommitDigestRenderer digestRenderer,
    CommitExecutionPersistenceService persistenceService,
    ValidationPersistenceService validationPersistenceService,
    CodexExecutionPersistenceService codexExecutionPersistenceService,
    IOptions<CommitEngineOptions> options)
{
    public async Task<CommitExecutionResult> RunAsync(
        ValidationResult validationResult,
        CodexExecutionResult codexResponses,
        bool emitJsonToStdout,
        CancellationToken cancellationToken)
    {
        var result = await commitEngineService.ExecuteAsync(validationResult, codexResponses, cancellationToken);
        result = new CommitExecutionResult
        {
            GeneratedAtUtc = result.GeneratedAtUtc,
            SourceValidationGeneratedAtUtc = result.SourceValidationGeneratedAtUtc,
            SourceResponseGeneratedAtUtc = result.SourceResponseGeneratedAtUtc,
            DryRunEnabled = result.DryRunEnabled,
            Summary = result.Summary,
            Operations = result.Operations,
            Notes = result.Notes,
            Digest = digestRenderer.Render(result)
        };

        await persistenceService.PersistAsync(result, cancellationToken);

        if (emitJsonToStdout)
        {
            Console.Out.WriteLine(persistenceService.Serialize(result));
        }

        logger.LogInformation(
            "Exécutions Git écrites dans {OutputPath} et {DigestOutputPath}",
            options.Value.OutputPath,
            options.Value.DigestOutputPath);

        return result;
    }

    public async Task<CommitExecutionResult> RunFromPathsAsync(
        string? validationPath,
        string? responsePath,
        bool emitJsonToStdout,
        CancellationToken cancellationToken)
    {
        var effectiveValidationPath = string.IsNullOrWhiteSpace(validationPath)
            ? options.Value.InputValidationPath
            : validationPath;
        var effectiveResponsePath = string.IsNullOrWhiteSpace(responsePath)
            ? options.Value.InputResponsePath
            : responsePath;

        var validationResult = await validationPersistenceService.LoadResultAsync(effectiveValidationPath, cancellationToken);
        var responses = await codexExecutionPersistenceService.LoadAsync(effectiveResponsePath, cancellationToken);

        return await RunAsync(validationResult, responses, emitJsonToStdout, cancellationToken);
    }
}
