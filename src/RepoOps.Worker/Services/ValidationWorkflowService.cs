using Microsoft.Extensions.Options;
using RepoOps.Worker.Models;
using RepoOps.Worker.Options;

namespace RepoOps.Worker.Services;

public sealed class ValidationWorkflowService(
    ILogger<ValidationWorkflowService> logger,
    ValidationEngineService validationEngineService,
    ValidationDigestRenderer digestRenderer,
    ValidationPersistenceService persistenceService,
    IOptions<ValidationEngineOptions> options)
{
    public async Task<ValidationResult> RunAsync(
        CodexExecutionResult responses,
        IReadOnlyList<ValidationInputRecord> decisions,
        bool emitJsonToStdout,
        CancellationToken cancellationToken)
    {
        var result = validationEngineService.Apply(responses, decisions);
        result = new ValidationResult
        {
            GeneratedAtUtc = result.GeneratedAtUtc,
            SourceResponseGeneratedAtUtc = result.SourceResponseGeneratedAtUtc,
            SourceReportStatus = result.SourceReportStatus,
            ExecutorMode = result.ExecutorMode,
            Summary = result.Summary,
            Decisions = result.Decisions,
            Notes = result.Notes,
            Digest = digestRenderer.Render(result)
        };

        await persistenceService.PersistAsync(result, cancellationToken);

        if (emitJsonToStdout)
        {
            Console.Out.WriteLine(persistenceService.Serialize(result));
        }

        logger.LogInformation(
            "Validations humaines écrites dans {OutputPath} et {DigestOutputPath}",
            options.Value.OutputPath,
            options.Value.DigestOutputPath);

        return result;
    }

    public async Task<ValidationResult> RunFromPathsAsync(
        string? responsePath,
        string? validationPath,
        bool interactiveMode,
        bool emitJsonToStdout,
        CancellationToken cancellationToken)
    {
        var effectiveResponsePath = string.IsNullOrWhiteSpace(responsePath)
            ? options.Value.InputResponsePath
            : responsePath;
        var responses = await persistenceService.LoadResponsesAsync(effectiveResponsePath, cancellationToken);

        IReadOnlyList<ValidationInputRecord> decisions;
        if (interactiveMode)
        {
            decisions = CollectInteractiveDecisions(responses);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(validationPath))
            {
                throw new InvalidOperationException("Le mode non interactif exige un fichier de validation explicite.");
            }

            decisions = await persistenceService.LoadValidationInputsAsync(validationPath, cancellationToken);
        }

        return await RunAsync(responses, decisions, emitJsonToStdout, cancellationToken);
    }

    private static IReadOnlyList<ValidationInputRecord> CollectInteractiveDecisions(CodexExecutionResult responses)
    {
        var decisions = new List<ValidationInputRecord>(responses.Responses.Count);

        foreach (var response in responses.Responses)
        {
            Console.WriteLine("------------------------------------------------------------");
            Console.WriteLine($"Action : {response.ActionId}");
            Console.WriteLine($"Cible  : {response.Repository}{(response.PullRequestNumber is null ? string.Empty : $"#{response.PullRequestNumber}")}");
            Console.WriteLine($"Type   : {response.ResponseType}");
            Console.WriteLine($"Prompt : {response.PromptType}");
            Console.WriteLine($"Résumé : {response.Summary}");
            Console.WriteLine($"Confiance : {response.ConfidenceLevel}");
            Console.WriteLine("Décision [a=approved / r=rejected / n=needs-review] (défaut: n) :");

            var decisionInput = Console.ReadLine();
            var decision = ParseDecision(decisionInput);

            Console.WriteLine("Commentaire optionnel :");
            var comment = Console.ReadLine() ?? string.Empty;

            decisions.Add(new ValidationInputRecord
            {
                ActionId = response.ActionId,
                Decision = decision,
                Comment = comment,
                TimestampUtc = DateTimeOffset.UtcNow
            });
        }

        return decisions;
    }

    private static ValidationDecisionType ParseDecision(string? input)
    {
        return input?.Trim().ToLowerInvariant() switch
        {
            "a" or "approved" => ValidationDecisionType.Approved,
            "r" or "rejected" => ValidationDecisionType.Rejected,
            _ => ValidationDecisionType.NeedsReview
        };
    }
}
