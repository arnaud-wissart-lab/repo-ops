using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RepoOps.Worker.Clients;
using RepoOps.Worker.Models;
using RepoOps.Worker.Options;
using RepoOps.Worker.Services;
using Xunit;

namespace RepoOps.Worker.Tests;

public sealed class PullRequestDecisionServiceTests
{
    [Fact]
    public void PatchEligible_ReturnsAutoMerge()
    {
        var service = CreateDecisionService();
        var pullRequest = CreatePullRequest("owner/repo-a", 101, "chore(deps): update dependency x from 1.2.3 to 1.2.4", ["patch"]);

        var evaluation = service.Evaluate("owner/repo-a", pullRequest, PullRequestChecksStatus.Success, mergeable: true, mergeableState: "clean");

        Assert.Equal(MergeDecision.AutoMerge, evaluation.Decision);
        Assert.Equal("global", evaluation.PolicySource);
        Assert.Contains("éligible à l'auto-merge", evaluation.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MinorNonAutorisee_ReturnsManualReview()
    {
        var service = CreateDecisionService();
        var pullRequest = CreatePullRequest("owner/repo-a", 102, "chore(deps): update dependency x from 1.2.3 to 1.3.0", ["minor"]);

        var evaluation = service.Evaluate("owner/repo-a", pullRequest, PullRequestChecksStatus.Success, mergeable: true, mergeableState: "clean");

        Assert.Equal(MergeDecision.ManualReview, evaluation.Decision);
        Assert.Contains("ne sont pas autorisées", string.Join(" ", evaluation.Reasons), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Major_ReturnsManualReview()
    {
        var service = CreateDecisionService();
        var pullRequest = CreatePullRequest("owner/repo-a", 103, "chore(deps): update dependency x from 1.2.3 to 2.0.0", ["major"]);

        var evaluation = service.Evaluate("owner/repo-a", pullRequest, PullRequestChecksStatus.Success, mergeable: true, mergeableState: "clean");

        Assert.Equal(MergeDecision.ManualReview, evaluation.Decision);
        Assert.Contains("majeures", string.Join(" ", evaluation.Reasons), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Draft_ReturnsBlocked()
    {
        var service = CreateDecisionService();
        var pullRequest = CreatePullRequest("owner/repo-a", 104, "chore(deps): update dependency x from 1.2.3 to 1.2.4", ["patch"], draft: true);

        var evaluation = service.Evaluate("owner/repo-a", pullRequest, PullRequestChecksStatus.Success, mergeable: true, mergeableState: "clean");

        Assert.Equal(MergeDecision.Blocked, evaluation.Decision);
        Assert.Contains("brouillon", string.Join(" ", evaluation.Reasons), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ChecksPending_ReturnsBlocked()
    {
        var service = CreateDecisionService();
        var pullRequest = CreatePullRequest("owner/repo-a", 105, "chore(deps): update dependency x from 1.2.3 to 1.2.4", ["patch"]);

        var evaluation = service.Evaluate("owner/repo-a", pullRequest, PullRequestChecksStatus.Pending, mergeable: true, mergeableState: "clean");

        Assert.Equal(MergeDecision.Blocked, evaluation.Decision);
        Assert.Contains("en attente", string.Join(" ", evaluation.Reasons), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ChecksFailed_ReturnsBlocked()
    {
        var service = CreateDecisionService();
        var pullRequest = CreatePullRequest("owner/repo-a", 106, "chore(deps): update dependency x from 1.2.3 to 1.2.4", ["patch"]);

        var evaluation = service.Evaluate("owner/repo-a", pullRequest, PullRequestChecksStatus.Failed, mergeable: true, mergeableState: "clean");

        Assert.Equal(MergeDecision.Blocked, evaluation.Decision);
        Assert.Contains("en échec", string.Join(" ", evaluation.Reasons), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MergeableFalse_ReturnsBlocked()
    {
        var service = CreateDecisionService();
        var pullRequest = CreatePullRequest("owner/repo-a", 107, "chore(deps): update dependency x from 1.2.3 to 1.2.4", ["patch"]);

        var evaluation = service.Evaluate("owner/repo-a", pullRequest, PullRequestChecksStatus.Success, mergeable: false, mergeableState: "clean");

        Assert.Equal(MergeDecision.Blocked, evaluation.Decision);
        Assert.Contains("mergeable", string.Join(" ", evaluation.Reasons), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MergeableStateNonAcceptable_ReturnsBlocked()
    {
        var service = CreateDecisionService();
        var pullRequest = CreatePullRequest("owner/repo-a", 108, "chore(deps): update dependency x from 1.2.3 to 1.2.4", ["patch"]);

        var evaluation = service.Evaluate("owner/repo-a", pullRequest, PullRequestChecksStatus.Success, mergeable: true, mergeableState: "dirty");

        Assert.Equal(MergeDecision.Blocked, evaluation.Decision);
        Assert.Contains("n'est pas accepté", string.Join(" ", evaluation.Reasons), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DepotExclu_ReturnsManualReview()
    {
        var service = CreateDecisionService(new AutoMergeOptions
        {
            AllowedUpdateTypes = ["patch"],
            AllowedMergeableStates = ["clean"],
            RepositoryPolicies =
            [
                new RepositoryAutoMergePolicy
                {
                    Repository = "owner/repo-exclu",
                    AllowAutoMerge = false,
                    ReviewRequired = true
                }
            ]
        });

        var pullRequest = CreatePullRequest("owner/repo-exclu", 109, "chore(deps): update dependency x from 1.2.3 to 1.2.4", ["patch"]);
        var evaluation = service.Evaluate("owner/repo-exclu", pullRequest, PullRequestChecksStatus.Success, mergeable: true, mergeableState: "clean");

        Assert.Equal(MergeDecision.ManualReview, evaluation.Decision);
        Assert.Equal("repository:owner/repo-exclu", evaluation.PolicySource);
        Assert.Contains("revue manuelle", string.Join(" ", evaluation.Reasons), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DryRunActive_DoesNotAttemptNetworkMerge()
    {
        var apiClient = new GitHubApiClient(
            new HttpClient(),
            Microsoft.Extensions.Options.Options.Create(new GitHubOptions()),
            NullLogger<GitHubApiClient>.Instance);
        var service = new PullRequestAutoMergeService(
            apiClient,
            Microsoft.Extensions.Options.Options.Create(new AutoMergeOptions
            {
                Enabled = true,
                DryRunEnabled = true
            }),
            NullLogger<PullRequestAutoMergeService>.Instance);

        var evaluation = new PullRequestMergeEvaluation
        {
            Repository = "owner/repo-a",
            Number = 110,
            Decision = MergeDecision.AutoMerge,
            MergeMethod = "squash",
            Reasons = ["PR éligible."]
        };

        var result = await service.ExecuteAsync("owner", "repo-a", evaluation, CancellationToken.None);

        Assert.Equal(PullRequestMergeActionStatus.DryRun, result.ActionStatus);
        Assert.Equal(MergeDecision.AutoMerge, result.Decision);
        Assert.Contains("dry-run", result.Summary, StringComparison.OrdinalIgnoreCase);
    }

    private static PullRequestDecisionService CreateDecisionService(AutoMergeOptions? options = null)
    {
        return new PullRequestDecisionService(Microsoft.Extensions.Options.Options.Create(options ?? new AutoMergeOptions
        {
            AllowedUpdateTypes = ["patch"],
            AllowedMergeableStates = ["clean"]
        }));
    }

    private static GitHubPullRequestDto CreatePullRequest(
        string repository,
        int number,
        string title,
        string[] labels,
        bool draft = false)
    {
        return new GitHubPullRequestDto
        {
            Number = number,
            Title = title,
            Draft = draft,
            HtmlUrl = $"https://github.com/{repository}/pull/{number}",
            Labels = labels.Select(label => new GitHubLabelDto { Name = label }).ToArray(),
            User = new GitHubUserDto { Login = "renovate[bot]" },
            Head = new GitHubPullRequestHeadDto
            {
                Sha = "abc123",
                Ref = "renovate/example"
            }
        };
    }
}
