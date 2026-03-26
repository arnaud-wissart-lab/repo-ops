using System.Text.Json;
using Microsoft.Extensions.Options;
using RepoOps.Worker.Models;
using RepoOps.Worker.Options;

namespace RepoOps.Worker.Services;

public sealed class SupervisorDecisionPersistenceService(IOptions<RepoOpsWorkerOptions> options)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task PersistAsync(SupervisorDecisionResult result, CancellationToken cancellationToken)
    {
        var settings = options.Value;

        await WriteFileAsync(
            settings.SupervisorOutputPath,
            JsonSerializer.Serialize(result, JsonOptions),
            cancellationToken);

        await WriteFileAsync(
            settings.SupervisorDigestOutputPath,
            result.Digest.PlainTextBody,
            cancellationToken);
    }

    public string Serialize(SupervisorDecisionResult result) => JsonSerializer.Serialize(result, JsonOptions);

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
