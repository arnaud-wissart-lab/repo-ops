using Microsoft.Extensions.Options;
using RepoOps.Worker.Models;
using RepoOps.Worker.Options;
using RepoOps.Worker.Services;
using Xunit;

namespace RepoOps.Worker.Tests;

public sealed class RunHistoryPersistenceServiceTests : IDisposable
{
    private readonly string rootPath;
    private readonly RepoOpsWorkerOptions options;
    private readonly RunHistoryPersistenceService service;

    public RunHistoryPersistenceServiceTests()
    {
        rootPath = Path.Combine(Path.GetTempPath(), $"repo-ops-history-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(rootPath);

        options = new RepoOpsWorkerOptions
        {
            RunHistoryDirectoryPath = Path.Combine(rootPath, "history"),
            RunHistoryIndexPath = Path.Combine(rootPath, "history", "index.json"),
            RunHistoryRetentionCount = 2,
            HistoryViewCount = 10
        };

        service = new RunHistoryPersistenceService(Microsoft.Extensions.Options.Options.Create(options));
    }

    [Fact]
    public async Task PersistAsync_EcritLeRapportEtMetAJourLIndex()
    {
        var report = BuildReport("run-1", new DateTimeOffset(2026, 3, 26, 9, 0, 0, TimeSpan.Zero), "Success");

        await service.PersistAsync(report, CancellationToken.None);
        var result = await service.LoadRecentAsync(5, CancellationToken.None);

        var entry = Assert.Single(result.Runs);
        Assert.Equal("run-1", entry.RunId);
        Assert.Equal("Success", entry.Status);
        Assert.True(File.Exists(entry.ReportPath));
    }

    [Fact]
    public async Task PersistAsync_RespecteLaRetention()
    {
        await service.PersistAsync(BuildReport("run-1", new DateTimeOffset(2026, 3, 26, 9, 0, 0, TimeSpan.Zero), "Success"), CancellationToken.None);
        await service.PersistAsync(BuildReport("run-2", new DateTimeOffset(2026, 3, 26, 10, 0, 0, TimeSpan.Zero), "Partial"), CancellationToken.None);
        await service.PersistAsync(BuildReport("run-3", new DateTimeOffset(2026, 3, 26, 11, 0, 0, TimeSpan.Zero), "Failed"), CancellationToken.None);

        var result = await service.LoadRecentAsync(5, CancellationToken.None);

        Assert.Equal(2, result.Runs.Count);
        Assert.Equal("run-3", result.Runs[0].RunId);
        Assert.Equal("run-2", result.Runs[1].RunId);
    }

    public void Dispose()
    {
        if (Directory.Exists(rootPath))
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    private static MaintenanceRunReport BuildReport(
        string runId,
        DateTimeOffset runDateUtc,
        string status)
    {
        return new MaintenanceRunReport
        {
            Summary = new MaintenanceExecutionSummary
            {
                Status = status,
                RunDateUtc = runDateUtc,
                InputSource = "test"
            },
            Observability = new MaintenanceObservability
            {
                RunId = runId,
                StartedAtUtc = runDateUtc,
                FinishedAtUtc = runDateUtc.AddSeconds(2),
                DurationMilliseconds = 2000,
                Metrics = new MaintenanceRunMetrics
                {
                    AnalyzedPullRequests = 3,
                    AutoMergedPullRequests = 1,
                    BlockedPullRequests = 1,
                    ErrorCount = status == "Failed" ? 1 : 0
                }
            }
        };
    }
}
