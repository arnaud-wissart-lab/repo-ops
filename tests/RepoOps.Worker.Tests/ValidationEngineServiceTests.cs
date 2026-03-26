using Microsoft.Extensions.Logging.Abstractions;
using RepoOps.Worker.Models;
using RepoOps.Worker.Services;
using Xunit;

namespace RepoOps.Worker.Tests;

public sealed class ValidationEngineServiceTests
{
    [Fact]
    public void ApprovedDecision_SetsReadyForExecution()
    {
        var service = CreateService();
        var result = service.Apply(
            CreateResponses(new CodexExecutionResponse
            {
                ActionId = "owner-repo-42-review",
                Repository = "owner/repo",
                PullRequestNumber = 42,
                ResponseType = CodexResponseType.Analysis,
                RequiresHumanValidation = true,
                ReadyForExecution = false,
                Summary = "Analyse prête."
            }),
            [
                new ValidationInputRecord
                {
                    ActionId = "owner-repo-42-review",
                    Decision = ValidationDecisionType.Approved,
                    Comment = "Validation humaine explicite.",
                    TimestampUtc = DateTimeOffset.UtcNow
                }
            ]);

        var decision = Assert.Single(result.Decisions);
        Assert.Equal(ValidationDecisionType.Approved, decision.Decision);
        Assert.True(decision.ReadyForExecution);
        Assert.Equal(1, result.Summary.ApprovedActions);
    }

    [Fact]
    public void MissingDecision_DefaultsToNeedsReview()
    {
        var service = CreateService();
        var result = service.Apply(
            CreateResponses(new CodexExecutionResponse
            {
                ActionId = "owner-repo-43-fix-required",
                Repository = "owner/repo",
                PullRequestNumber = 43,
                ResponseType = CodexResponseType.ProposedFix,
                Summary = "Correctif simulé."
            }),
            []);

        var decision = Assert.Single(result.Decisions);
        Assert.Equal(ValidationDecisionType.NeedsReview, decision.Decision);
        Assert.False(decision.ReadyForExecution);
        Assert.Contains("Aucune décision humaine", decision.Comment, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MultipleDecisions_AreSummarizedCorrectly()
    {
        var service = CreateService();
        var result = service.Apply(
            new CodexExecutionResult
            {
                SourceReportStatus = "Partial",
                Responses =
                [
                    new CodexExecutionResponse
                    {
                        ActionId = "a-1",
                        Repository = "owner/repo-a",
                        ResponseType = CodexResponseType.Analysis,
                        Summary = "Analyse."
                    },
                    new CodexExecutionResponse
                    {
                        ActionId = "b-2",
                        Repository = "owner/repo-b",
                        ResponseType = CodexResponseType.ProposedFix,
                        Summary = "Correctif."
                    }
                ]
            },
            [
                new ValidationInputRecord
                {
                    ActionId = "a-1",
                    Decision = ValidationDecisionType.Approved,
                    Comment = "Accord manuel.",
                    TimestampUtc = DateTimeOffset.UtcNow
                },
                new ValidationInputRecord
                {
                    ActionId = "b-2",
                    Decision = ValidationDecisionType.Rejected,
                    Comment = "Rejet manuel.",
                    TimestampUtc = DateTimeOffset.UtcNow
                }
            ]);

        Assert.Equal(2, result.Summary.TotalActions);
        Assert.Equal(1, result.Summary.ApprovedActions);
        Assert.Equal(1, result.Summary.RejectedActions);
        Assert.Equal(0, result.Summary.NeedsReviewActions);
        Assert.Contains(result.Notes, note => note.Contains("statut Partial", StringComparison.OrdinalIgnoreCase));
    }

    private static ValidationEngineService CreateService()
    {
        return new ValidationEngineService(NullLogger<ValidationEngineService>.Instance);
    }

    private static CodexExecutionResult CreateResponses(CodexExecutionResponse response)
    {
        return new CodexExecutionResult
        {
            SourceReportStatus = "Success",
            Responses = [response]
        };
    }
}
