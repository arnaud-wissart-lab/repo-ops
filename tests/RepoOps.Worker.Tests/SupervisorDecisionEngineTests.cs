using Microsoft.Extensions.Logging.Abstractions;
using RepoOps.Worker.Models;
using RepoOps.Worker.Services;
using Xunit;

namespace RepoOps.Worker.Tests;

public sealed class SupervisorDecisionEngineTests
{
    [Fact]
    public void PatchEligible_ReturnsAutoMergeEligible()
    {
        var engine = CreateEngine();
        var report = CreateReport(CreateEvaluation(versionType: PullRequestVersionType.Patch, checksStatus: PullRequestChecksStatus.Success, decision: MergeDecision.AutoMerge));

        var result = engine.Evaluate(report);
        var action = Assert.Single(result.Actions);

        Assert.Equal(SupervisorActionType.AutoMergeEligible, action.Type);
        Assert.Equal(SupervisorActionPriority.Medium, action.Priority);
    }

    [Fact]
    public void Minor_ReturnsReview()
    {
        var engine = CreateEngine();
        var report = CreateReport(CreateEvaluation(versionType: PullRequestVersionType.Minor, checksStatus: PullRequestChecksStatus.Success, decision: MergeDecision.ManualReview));

        var result = engine.Evaluate(report);
        var action = Assert.Single(result.Actions);

        Assert.Equal(SupervisorActionType.Review, action.Type);
        Assert.Equal(SupervisorActionPriority.Medium, action.Priority);
    }

    [Fact]
    public void Major_ReturnsHighPriorityReview()
    {
        var engine = CreateEngine();
        var report = CreateReport(CreateEvaluation(versionType: PullRequestVersionType.Major, checksStatus: PullRequestChecksStatus.Success, decision: MergeDecision.ManualReview));

        var result = engine.Evaluate(report);
        var action = Assert.Single(result.Actions);

        Assert.Equal(SupervisorActionType.Review, action.Type);
        Assert.Equal(SupervisorActionPriority.High, action.Priority);
    }

    [Fact]
    public void ChecksFailed_ReturnsFixRequired()
    {
        var engine = CreateEngine();
        var report = CreateReport(CreateEvaluation(versionType: PullRequestVersionType.Patch, checksStatus: PullRequestChecksStatus.Failed, decision: MergeDecision.Blocked));

        var result = engine.Evaluate(report);
        var action = Assert.Single(result.Actions);

        Assert.Equal(SupervisorActionType.FixRequired, action.Type);
        Assert.Equal(SupervisorActionPriority.High, action.Priority);
    }

    [Fact]
    public void ChecksPending_ReturnsIgnore()
    {
        var engine = CreateEngine();
        var report = CreateReport(CreateEvaluation(versionType: PullRequestVersionType.Patch, checksStatus: PullRequestChecksStatus.Pending, decision: MergeDecision.Blocked));

        var result = engine.Evaluate(report);
        var action = Assert.Single(result.Actions);

        Assert.Equal(SupervisorActionType.Ignore, action.Type);
        Assert.Equal(SupervisorActionPriority.Low, action.Priority);
    }

    [Fact]
    public void CriticalSecurityPatch_RaisesPriorityToHigh()
    {
        var engine = CreateEngine();
        var report = CreateReport(CreateEvaluation(
            versionType: PullRequestVersionType.Patch,
            checksStatus: PullRequestChecksStatus.Success,
            decision: MergeDecision.AutoMerge,
            isSecurityUpdate: true,
            securitySeverity: "critical"));

        var result = engine.Evaluate(report);
        var action = Assert.Single(result.Actions);

        Assert.Equal(SupervisorActionType.AutoMergeEligible, action.Type);
        Assert.Equal(SupervisorActionPriority.High, action.Priority);
        Assert.Contains("vulnérabilité critique", action.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CriticalRepositoryWithoutPullRequest_AddsRepositoryFixRequired()
    {
        var engine = CreateEngine();
        var report = new MaintenanceRunReport
        {
            Summary = new MaintenanceExecutionSummary { Status = "Success" },
            Vulnerabilities = new VulnerabilitySummary
            {
                Status = "Success",
                CriticalCount = 1,
                Repositories =
                [
                    new RepositoryVulnerabilitySummary
                    {
                        Repository = "owner/repo-a",
                        Status = "Success",
                        CriticalCount = 1
                    }
                ]
            },
            AutoMerge = new AutoMergeSummary()
        };

        var result = engine.Evaluate(report);
        var action = Assert.Single(result.Actions);

        Assert.Equal(SupervisorActionType.FixRequired, action.Type);
        Assert.Equal(SupervisorActionPriority.High, action.Priority);
        Assert.Equal("owner/repo-a", action.Repository);
        Assert.Null(action.PullRequestNumber);
    }

    [Fact]
    public void EmptyReport_ProducesNoAction()
    {
        var engine = CreateEngine();
        var report = new MaintenanceRunReport
        {
            Summary = new MaintenanceExecutionSummary { Status = "Success" }
        };

        var result = engine.Evaluate(report);

        Assert.Empty(result.Actions);
        Assert.Contains(result.Notes, note => note.Contains("Aucune action", StringComparison.OrdinalIgnoreCase));
    }

    private static SupervisorDecisionEngine CreateEngine()
    {
        return new SupervisorDecisionEngine(NullLogger<SupervisorDecisionEngine>.Instance);
    }

    private static MaintenanceRunReport CreateReport(PullRequestMergeEvaluation evaluation)
    {
        return new MaintenanceRunReport
        {
            Summary = new MaintenanceExecutionSummary { Status = "Success" },
            AutoMerge = new AutoMergeSummary
            {
                Evaluations = [evaluation]
            },
            Vulnerabilities = new VulnerabilitySummary
            {
                Status = "Success"
            }
        };
    }

    private static PullRequestMergeEvaluation CreateEvaluation(
        PullRequestVersionType versionType,
        PullRequestChecksStatus checksStatus,
        MergeDecision decision,
        bool isSecurityUpdate = false,
        string securitySeverity = "")
    {
        return new PullRequestMergeEvaluation
        {
            Repository = "owner/repo-a",
            Number = 42,
            Title = "chore(deps): update dependency sample",
            HtmlUrl = "https://github.com/owner/repo-a/pull/42",
            VersionType = versionType,
            ChecksStatus = checksStatus,
            Mergeable = true,
            MergeableState = "clean",
            Decision = decision,
            IsSecurityUpdate = isSecurityUpdate,
            SecuritySeverity = securitySeverity
        };
    }
}
