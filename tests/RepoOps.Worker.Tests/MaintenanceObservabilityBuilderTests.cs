using RepoOps.Worker.Models;
using RepoOps.Worker.Services;
using Xunit;

namespace RepoOps.Worker.Tests;

public sealed class MaintenanceObservabilityBuilderTests
{
    private readonly MaintenanceObservabilityBuilder builder = new();

    [Fact]
    public void Build_CalculeLesMetriquesAttendues()
    {
        var report = new MaintenanceRunReport
        {
            Summary = new MaintenanceExecutionSummary
            {
                Status = "Partial",
                RunDateUtc = new DateTimeOffset(2026, 3, 26, 8, 0, 0, TimeSpan.Zero)
            },
            PullRequestStatuses = new PullRequestStatuses
            {
                MergedRecently = ["owner/repo-a#1"],
                ClosedWithoutMerge = ["owner/repo-a#2"]
            },
            AutoMerge = new AutoMergeSummary
            {
                AutoMergedPullRequests = ["owner/repo-a#1"],
                BlockedPullRequests = ["owner/repo-a#3"],
                Evaluations =
                [
                    new PullRequestMergeEvaluation { Repository = "owner/repo-a", Number = 3 },
                    new PullRequestMergeEvaluation { Repository = "owner/repo-a", Number = 4 }
                ]
            },
            RenovateExecution = new RenovateExecutionDetails
            {
                Status = "Failed",
                Errors = ["Erreur 1", "Erreur 2"]
            }
        };

        var result = builder.Build(
            report,
            "run-123",
            new DateTimeOffset(2026, 3, 26, 8, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 3, 26, 8, 0, 5, TimeSpan.Zero));

        Assert.Equal("run-123", result.RunId);
        Assert.Equal(5000, result.DurationMilliseconds);
        Assert.Equal(4, result.Metrics.AnalyzedPullRequests);
        Assert.Equal(1, result.Metrics.AutoMergedPullRequests);
        Assert.Equal(1, result.Metrics.BlockedPullRequests);
        Assert.Equal(3, result.Metrics.ErrorCount);
    }
}
