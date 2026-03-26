using System.Text.Json;
using Microsoft.Extensions.Options;
using RepoOps.Worker.Models;
using RepoOps.Worker.Options;

namespace RepoOps.Worker.Services;

public sealed class RunHistoryPersistenceService(IOptions<RepoOpsWorkerOptions> options)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task PersistAsync(MaintenanceRunReport report, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(report);

        var settings = options.Value;
        var historyDirectoryPath = Path.GetFullPath(settings.RunHistoryDirectoryPath);
        Directory.CreateDirectory(historyDirectoryPath);

        var historyFilePath = BuildHistoryFilePath(historyDirectoryPath, report);
        await File.WriteAllTextAsync(
            historyFilePath,
            JsonSerializer.Serialize(report, JsonOptions),
            cancellationToken);

        var index = await LoadIndexAsync(settings.RunHistoryIndexPath, cancellationToken);
        var retainedRuns = index.Runs
            .Where(run => !string.Equals(run.RunId, report.Observability.RunId, StringComparison.Ordinal))
            .Prepend(BuildEntry(report, historyFilePath))
            .OrderByDescending(run => run.RunDateUtc)
            .ToList();

        if (settings.RunHistoryRetentionCount > 0 && retainedRuns.Count > settings.RunHistoryRetentionCount)
        {
            var discardedRuns = retainedRuns.Skip(settings.RunHistoryRetentionCount).ToList();
            retainedRuns = retainedRuns.Take(settings.RunHistoryRetentionCount).ToList();

            foreach (var discardedRun in discardedRuns)
            {
                TryDeleteHistoryFile(discardedRun.ReportPath, historyDirectoryPath);
            }
        }

        var updatedIndex = new RunHistoryIndex
        {
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            Runs = retainedRuns
        };

        await WriteFileAsync(
            settings.RunHistoryIndexPath,
            JsonSerializer.Serialize(updatedIndex, JsonOptions),
            cancellationToken);
    }

    public async Task<RunHistoryViewResult> LoadRecentAsync(int requestedCount, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var effectiveCount = requestedCount > 0 ? requestedCount : settings.HistoryViewCount;
        var index = await LoadIndexAsync(settings.RunHistoryIndexPath, cancellationToken);

        return new RunHistoryViewResult
        {
            RequestedCount = effectiveCount,
            Runs = index.Runs
                .OrderByDescending(run => run.RunDateUtc)
                .Take(effectiveCount)
                .ToArray()
        };
    }

    public string Serialize(RunHistoryViewResult result) => JsonSerializer.Serialize(result, JsonOptions);

    private static string BuildHistoryFilePath(string historyDirectoryPath, MaintenanceRunReport report)
    {
        var runDate = report.Summary.RunDateUtc.ToUniversalTime();
        var safeRunId = string.IsNullOrWhiteSpace(report.Observability.RunId)
            ? Guid.NewGuid().ToString("N")
            : report.Observability.RunId;
        var fileName = $"{runDate:yyyyMMdd-HHmmss}-{safeRunId}.json";
        return Path.Combine(historyDirectoryPath, fileName);
    }

    private static RunHistoryEntry BuildEntry(MaintenanceRunReport report, string historyFilePath)
    {
        return new RunHistoryEntry
        {
            RunId = report.Observability.RunId,
            RunDateUtc = report.Summary.RunDateUtc,
            Status = report.Summary.Status,
            Mode = report.Summary.Mode,
            InputSource = report.Summary.InputSource,
            DurationMilliseconds = report.Observability.DurationMilliseconds,
            Metrics = report.Observability.Metrics,
            ReportPath = Path.GetFullPath(historyFilePath)
        };
    }

    private static async Task<RunHistoryIndex> LoadIndexAsync(string indexPath, CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(indexPath);
        if (!File.Exists(fullPath))
        {
            return new RunHistoryIndex();
        }

        var json = await File.ReadAllTextAsync(fullPath, cancellationToken);
        return JsonSerializer.Deserialize<RunHistoryIndex>(json, JsonOptions) ?? new RunHistoryIndex();
    }

    private static async Task WriteFileAsync(
        string outputPath,
        string content,
        CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(outputPath);
        var directoryPath = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        await File.WriteAllTextAsync(fullPath, content, cancellationToken);
    }

    private static void TryDeleteHistoryFile(string filePath, string allowedRootPath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        var fullPath = Path.GetFullPath(filePath);
        var fullRootPath = Path.GetFullPath(allowedRootPath);

        if (!fullPath.StartsWith(fullRootPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}
