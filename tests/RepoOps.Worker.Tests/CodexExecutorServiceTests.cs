using Microsoft.Extensions.Logging.Abstractions;
using RepoOps.Worker.Clients;
using RepoOps.Worker.Models;
using RepoOps.Worker.Services;
using Xunit;

namespace RepoOps.Worker.Tests;

public sealed class CodexExecutorServiceTests
{
    [Fact]
    public async Task ReviewPrompt_GeneratesAnalysisResponse()
    {
        var service = CreateService(new FakeCodexClient(prompt => new CodexClientResponse
        {
            ResponseType = CodexResponseType.Analysis,
            ConfidenceLevel = CodexConfidenceLevel.Medium,
            Summary = $"Analyse prête pour {prompt.Repository}",
            ResponseText = "Réponse simulée d'analyse.",
            RequiresHumanValidation = true,
            ReadyForExecution = false
        }));

        var result = await service.ExecuteAsync(CreatePromptResult(new GeneratedPrompt
        {
            ActionType = SupervisorActionType.Review,
            Repository = "owner/repo-a",
            PullRequestNumber = 120,
            PromptType = "review",
            PromptText = "Prompt de revue"
        }), CancellationToken.None);

        var response = Assert.Single(result.Responses);
        Assert.Equal("owner-repo-a-120-review", response.ActionId);
        Assert.Equal(CodexResponseType.Analysis, response.ResponseType);
        Assert.True(response.RequiresHumanValidation);
        Assert.False(response.ReadyForExecution);
        Assert.Equal(1, result.Summary.AnalysisResponses);
    }

    [Fact]
    public async Task FixPrompt_GeneratesProposedFixResponse()
    {
        var service = CreateService(new FakeCodexClient(_ => new CodexClientResponse
        {
            ResponseType = CodexResponseType.ProposedFix,
            ConfidenceLevel = CodexConfidenceLevel.High,
            Summary = "Correctif proposé.",
            ResponseText = "Réponse simulée de correction.",
            RequiresHumanValidation = true,
            ReadyForExecution = false
        }));

        var result = await service.ExecuteAsync(CreatePromptResult(new GeneratedPrompt
        {
            ActionType = SupervisorActionType.FixRequired,
            Repository = "owner/repo-a",
            PullRequestNumber = 121,
            PromptType = "fix-required",
            PromptText = "Prompt de correction"
        }), CancellationToken.None);

        var response = Assert.Single(result.Responses);
        Assert.Equal(CodexResponseType.ProposedFix, response.ResponseType);
        Assert.Equal(CodexConfidenceLevel.High, response.ConfidenceLevel);
        Assert.Equal(1, result.Summary.ProposedFixResponses);
        Assert.Equal(1, result.Summary.HighConfidenceResponses);
    }

    [Fact]
    public async Task MultiplePrompts_GenerateConsistentSummary()
    {
        var service = CreateService(new FakeCodexClient(prompt => prompt.PromptType switch
        {
            "fix-required" => new CodexClientResponse
            {
                ResponseType = CodexResponseType.ProposedFix,
                ConfidenceLevel = CodexConfidenceLevel.Medium,
                Summary = "Correction simulée.",
                ResponseText = "Réponse simulée de correction.",
                RequiresHumanValidation = true,
                ReadyForExecution = false
            },
            _ => new CodexClientResponse
            {
                ResponseType = CodexResponseType.Analysis,
                ConfidenceLevel = CodexConfidenceLevel.Low,
                Summary = "Analyse simulée.",
                ResponseText = "Réponse simulée d'analyse.",
                RequiresHumanValidation = true,
                ReadyForExecution = false
            }
        }));

        var result = await service.ExecuteAsync(new GeneratedPromptResult
        {
            SourceReportStatus = "Partial",
            Prompts =
            [
                new GeneratedPrompt
                {
                    ActionType = SupervisorActionType.Review,
                    Repository = "owner/repo-a",
                    PromptType = "review",
                    PromptText = "Prompt de revue"
                },
                new GeneratedPrompt
                {
                    ActionType = SupervisorActionType.FixRequired,
                    Repository = "owner/repo-b",
                    PromptType = "fix-required",
                    PromptText = "Prompt de correction"
                }
            ]
        }, CancellationToken.None);

        Assert.Equal(2, result.Summary.TotalResponses);
        Assert.Equal(1, result.Summary.AnalysisResponses);
        Assert.Equal(1, result.Summary.ProposedFixResponses);
        Assert.Equal(2, result.Summary.RequiresHumanValidationResponses);
        Assert.Contains(result.Notes, note => note.Contains("validation humaine", StringComparison.OrdinalIgnoreCase));
    }

    private static CodexExecutorService CreateService(ICodexClient client)
    {
        return new CodexExecutorService(NullLogger<CodexExecutorService>.Instance, client);
    }

    private static GeneratedPromptResult CreatePromptResult(GeneratedPrompt prompt)
    {
        return new GeneratedPromptResult
        {
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            SourceReportStatus = "Success",
            Prompts = [prompt]
        };
    }

    private sealed class FakeCodexClient(Func<GeneratedPrompt, CodexClientResponse> factory) : ICodexClient
    {
        public string Mode => "Fake";

        public Task<CodexClientResponse> ExecuteAsync(GeneratedPrompt prompt, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(factory(prompt));
        }
    }
}
