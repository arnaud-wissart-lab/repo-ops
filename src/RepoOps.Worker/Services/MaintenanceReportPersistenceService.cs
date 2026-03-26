using System.Text.Json;
using Microsoft.Extensions.Options;
using RepoOps.Worker.Models;
using RepoOps.Worker.Options;

namespace RepoOps.Worker.Services;

public sealed class MaintenanceReportPersistenceService(IOptions<RepoOpsWorkerOptions> options)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task PersistAsync(MaintenanceRunReport report, CancellationToken cancellationToken)
    {
        var settings = options.Value;

        await WriteFileAsync(
            settings.ReportOutputPath,
            JsonSerializer.Serialize(report, JsonOptions),
            cancellationToken);

        await WriteFileAsync(
            settings.SummaryTextOutputPath,
            report.Digest.PlainTextBody,
            cancellationToken);

        await WriteFileAsync(
            settings.SummaryHtmlOutputPath,
            report.Digest.HtmlBody,
            cancellationToken);
    }

    public async Task<MaintenanceRunReport> LoadAsync(string reportPath, CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(reportPath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"Le rapport source '{fullPath}' est introuvable.",
                fullPath);
        }

        var json = await File.ReadAllTextAsync(fullPath, cancellationToken);
        var report = JsonSerializer.Deserialize<MaintenanceRunReport>(json, JsonOptions);

        if (report is null)
        {
            throw new InvalidOperationException($"Le rapport source '{fullPath}' est invalide ou vide.");
        }

        return report;
    }

    public string Serialize(MaintenanceRunReport report) => JsonSerializer.Serialize(report, JsonOptions);

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
}
