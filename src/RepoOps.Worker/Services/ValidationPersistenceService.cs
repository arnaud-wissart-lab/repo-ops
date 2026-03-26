using System.Text.Json;
using Microsoft.Extensions.Options;
using RepoOps.Worker.Models;
using RepoOps.Worker.Options;

namespace RepoOps.Worker.Services;

public sealed class ValidationPersistenceService(IOptions<ValidationEngineOptions> options)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task PersistAsync(ValidationResult result, CancellationToken cancellationToken)
    {
        var settings = options.Value;

        await WriteFileAsync(
            settings.OutputPath,
            JsonSerializer.Serialize(result, JsonOptions),
            cancellationToken);

        await WriteFileAsync(
            settings.DigestOutputPath,
            result.Digest.PlainTextBody,
            cancellationToken);
    }

    public async Task<CodexExecutionResult> LoadResponsesAsync(string responsePath, CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(responsePath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"Le fichier de réponses Codex '{fullPath}' est introuvable.",
                fullPath);
        }

        var json = await File.ReadAllTextAsync(fullPath, cancellationToken);
        var result = JsonSerializer.Deserialize<CodexExecutionResult>(json, JsonOptions);

        if (result is null)
        {
            throw new InvalidOperationException($"Le fichier de réponses Codex '{fullPath}' est invalide ou vide.");
        }

        return result;
    }

    public async Task<IReadOnlyList<ValidationInputRecord>> LoadValidationInputsAsync(string validationPath, CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(validationPath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"Le fichier de validation '{fullPath}' est introuvable.",
                fullPath);
        }

        var json = await File.ReadAllTextAsync(fullPath, cancellationToken);

        var result = JsonSerializer.Deserialize<ValidationResult>(json, JsonOptions);
        if (result?.Decisions?.Count > 0)
        {
            return result.Decisions
                .Select(decision => new ValidationInputRecord
                {
                    ActionId = decision.ActionId,
                    Decision = decision.Decision,
                    Comment = decision.Comment,
                    TimestampUtc = decision.TimestampUtc
                })
                .ToArray();
        }

        var envelope = JsonSerializer.Deserialize<ValidationInputEnvelope>(json, JsonOptions);
        if (envelope?.Decisions?.Count > 0)
        {
            return envelope.Decisions;
        }

        throw new InvalidOperationException($"Le fichier de validation '{fullPath}' est invalide ou ne contient aucune décision.");
    }

    public string Serialize(ValidationResult result) => JsonSerializer.Serialize(result, JsonOptions);

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

    private sealed class ValidationInputEnvelope
    {
        public IReadOnlyList<ValidationInputRecord> Decisions { get; init; } = Array.Empty<ValidationInputRecord>();
    }
}
