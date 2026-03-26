using Microsoft.Extensions.Logging.Abstractions;
using RepoOps.Worker.Models;
using RepoOps.Worker.Services;
using Xunit;

namespace RepoOps.Worker.Tests;

public sealed class PromptGeneratorServiceTests
{
    [Fact]
    public void FixRequired_GeneratesCorrectionPrompt()
    {
        var service = CreateService();
        var decisions = CreateDecisionResult(new SupervisorAction
        {
            Type = SupervisorActionType.FixRequired,
            Repository = "owner/repo-a",
            PullRequestNumber = 101,
            PullRequestTitle = "fix checks",
            PullRequestUrl = "https://github.com/owner/repo-a/pull/101",
            ChecksStatus = PullRequestChecksStatus.Failed,
            Priority = SupervisorActionPriority.High,
            Reason = "Les checks sont en échec.",
            Recommendation = "Traiter la cause d'échec avant toute fusion."
        });

        var result = service.Generate(decisions);
        var prompt = Assert.Single(result.Prompts);

        Assert.Equal("fix-required", prompt.PromptType);
        Assert.Contains("Analyser la cause du blocage", prompt.PromptText, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("en échec", prompt.Context.ChecksStatus);
    }

    [Fact]
    public void Review_GeneratesAnalysisPrompt()
    {
        var service = CreateService();
        var decisions = CreateDecisionResult(new SupervisorAction
        {
            Type = SupervisorActionType.Review,
            Repository = "owner/repo-a",
            PullRequestNumber = 102,
            PullRequestTitle = "minor update",
            ChecksStatus = PullRequestChecksStatus.Success,
            Priority = SupervisorActionPriority.Medium,
            Reason = "La mise à jour est mineure et doit être revue.",
            Recommendation = "Analyser l'impact avant de statuer."
        });

        var result = service.Generate(decisions);
        var prompt = Assert.Single(result.Prompts);

        Assert.Equal("review", prompt.PromptType);
        Assert.Contains("Analyser la PR", prompt.PromptText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AutoMergeEligible_GeneratesValidationPrompt()
    {
        var service = CreateService();
        var decisions = CreateDecisionResult(new SupervisorAction
        {
            Type = SupervisorActionType.AutoMergeEligible,
            Repository = "owner/repo-a",
            PullRequestNumber = 103,
            PullRequestTitle = "patch update",
            ChecksStatus = PullRequestChecksStatus.Success,
            Priority = SupervisorActionPriority.Medium,
            Reason = "La PR est prête pour validation finale.",
            Recommendation = "Confirmer qu'elle peut être fusionnée."
        });

        var result = service.Generate(decisions);
        var prompt = Assert.Single(result.Prompts);

        Assert.Equal("auto-merge-eligible", prompt.PromptType);
        Assert.Contains("validation finale", prompt.PromptText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SecurityFixRequired_GeneratesPriorityPrompt()
    {
        var service = CreateService();
        var decisions = CreateDecisionResult(new SupervisorAction
        {
            Type = SupervisorActionType.FixRequired,
            Repository = "owner/repo-a",
            PullRequestNumber = 104,
            PullRequestTitle = "security update",
            ChecksStatus = PullRequestChecksStatus.Failed,
            Priority = SupervisorActionPriority.High,
            Reason = "Vulnérabilité critique ouverte.",
            Recommendation = "Traiter la vulnérabilité en priorité.",
            IsSecurityRelated = true,
            SecuritySeverity = "critical"
        });

        var result = service.Generate(decisions);
        var prompt = Assert.Single(result.Prompts);

        Assert.Equal("vulnerability-priority", prompt.PromptType);
        Assert.Contains("sécurité", prompt.PromptText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MultipleActions_GeneratesConsistentSummary()
    {
        var service = CreateService();
        var decisions = new SupervisorDecisionResult
        {
            SourceReportStatus = "Partial",
            Actions =
            [
                new SupervisorAction
                {
                    Type = SupervisorActionType.Review,
                    Repository = "owner/repo-a",
                    Priority = SupervisorActionPriority.Medium,
                    Reason = "Revue nécessaire.",
                    Recommendation = "Analyser l'impact."
                },
                new SupervisorAction
                {
                    Type = SupervisorActionType.AutoMergeEligible,
                    Repository = "owner/repo-b",
                    Priority = SupervisorActionPriority.High,
                    Reason = "Validation finale requise.",
                    Recommendation = "Confirmer la fusion."
                }
            ]
        };

        var result = service.Generate(decisions);

        Assert.Equal(2, result.Summary.TotalPrompts);
        Assert.Equal(1, result.Summary.ReviewPrompts);
        Assert.Equal(1, result.Summary.ValidationPrompts);
        Assert.Equal(1, result.Summary.HighPriorityPrompts);
    }

    private static PromptGeneratorService CreateService()
    {
        return new PromptGeneratorService(NullLogger<PromptGeneratorService>.Instance);
    }

    private static SupervisorDecisionResult CreateDecisionResult(SupervisorAction action)
    {
        return new SupervisorDecisionResult
        {
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            SourceReportStatus = "Success",
            Actions = [action]
        };
    }
}
