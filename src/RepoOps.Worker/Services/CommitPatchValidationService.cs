using RepoOps.Worker.Options;

namespace RepoOps.Worker.Services;

public sealed class CommitPatchValidationService
{
    public CommitPatchValidationResult Validate(
        string proposedUnifiedDiff,
        string workspacePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspacePath);

        if (string.IsNullOrWhiteSpace(proposedUnifiedDiff))
        {
            return new CommitPatchValidationResult
            {
                Errors = ["Le patch unifié est vide."]
            };
        }

        var lines = proposedUnifiedDiff.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var entries = new List<PatchEntry>();
        PatchEntry? currentEntry = null;

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];

            if (line.StartsWith("diff --git ", StringComparison.Ordinal))
            {
                currentEntry = ParseDiffHeader(line);
                if (currentEntry is null)
                {
                    return new CommitPatchValidationResult
                    {
                        Errors = [$"Le patch contient un en-tête diff invalide à la ligne {index + 1}."]
                    };
                }

                entries.Add(currentEntry);
                continue;
            }

            if (currentEntry is null)
            {
                continue;
            }

            if (line.StartsWith("new file mode ", StringComparison.Ordinal))
            {
                currentEntry.ChangeType = PatchChangeType.Added;
                continue;
            }

            if (line.StartsWith("deleted file mode ", StringComparison.Ordinal))
            {
                currentEntry.ChangeType = PatchChangeType.Deleted;
                continue;
            }

            if (line.StartsWith("rename from ", StringComparison.Ordinal)
                || line.StartsWith("rename to ", StringComparison.Ordinal))
            {
                currentEntry.ChangeType = PatchChangeType.Unsupported;
                continue;
            }

            if (line.StartsWith("--- ", StringComparison.Ordinal))
            {
                currentEntry.OriginalPath = NormalizePatchPath(line["--- ".Length..]);
                if (string.Equals(currentEntry.OriginalPath, "/dev/null", StringComparison.Ordinal))
                {
                    currentEntry.ChangeType = PatchChangeType.Added;
                }

                continue;
            }

            if (line.StartsWith("+++ ", StringComparison.Ordinal))
            {
                currentEntry.NewPath = NormalizePatchPath(line["+++ ".Length..]);
                if (string.Equals(currentEntry.NewPath, "/dev/null", StringComparison.Ordinal))
                {
                    currentEntry.ChangeType = PatchChangeType.Deleted;
                }
            }
        }

        if (entries.Count == 0)
        {
            return new CommitPatchValidationResult
            {
                Errors = ["Le patch ne contient aucun bloc diff exploitable."]
            };
        }

        var errors = new List<string>();
        var modifiedFiles = new List<string>();
        var diffSummary = new List<string>();

        foreach (var entry in entries)
        {
            if (entry.ChangeType == PatchChangeType.Unsupported)
            {
                errors.Add($"Le patch contient une opération non prise en charge pour '{entry.DisplayPath}'.");
                continue;
            }

            var effectivePath = entry.ChangeType == PatchChangeType.Deleted
                ? entry.OriginalPath
                : entry.NewPath;

            if (string.IsNullOrWhiteSpace(effectivePath)
                || !IsSafeRelativePath(effectivePath))
            {
                errors.Add($"Le chemin '{entry.DisplayPath}' est ambigu ou invalide.");
                continue;
            }

            var fullPath = Path.GetFullPath(Path.Combine(workspacePath, effectivePath));
            if (!fullPath.StartsWith(Path.GetFullPath(workspacePath), StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Le chemin '{effectivePath}' sort du workspace autorisé.");
                continue;
            }

            var exists = File.Exists(fullPath);
            switch (entry.ChangeType)
            {
                case PatchChangeType.Modified when !exists:
                    errors.Add($"Le fichier à modifier '{effectivePath}' est introuvable.");
                    break;
                case PatchChangeType.Deleted when !exists:
                    errors.Add($"Le fichier à supprimer '{effectivePath}' est introuvable.");
                    break;
                case PatchChangeType.Added:
                    var parentPath = Path.GetDirectoryName(fullPath);
                    if (!string.IsNullOrWhiteSpace(parentPath) && !Directory.Exists(parentPath))
                    {
                        errors.Add($"Le dossier parent de '{effectivePath}' est introuvable.");
                    }
                    break;
            }

            modifiedFiles.Add(effectivePath);
            diffSummary.Add($"{FormatChangeType(entry.ChangeType)} {effectivePath}");
        }

        return new CommitPatchValidationResult
        {
            IsValid = errors.Count == 0,
            ModifiedFiles = modifiedFiles.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            DiffSummary = diffSummary,
            Errors = errors
        };
    }

    private static PatchEntry? ParseDiffHeader(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 4)
        {
            return null;
        }

        return new PatchEntry
        {
            OriginalPath = NormalizePatchPath(parts[2]),
            NewPath = NormalizePatchPath(parts[3]),
            ChangeType = PatchChangeType.Modified
        };
    }

    private static string NormalizePatchPath(string path)
    {
        var trimmed = path.Trim();
        if (string.Equals(trimmed, "/dev/null", StringComparison.Ordinal))
        {
            return trimmed;
        }

        if (trimmed.StartsWith("a/", StringComparison.Ordinal)
            || trimmed.StartsWith("b/", StringComparison.Ordinal))
        {
            return trimmed[2..];
        }

        return trimmed;
    }

    private static bool IsSafeRelativePath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return false;
        }

        var segments = path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return segments.All(segment => !string.Equals(segment, "..", StringComparison.Ordinal));
    }

    private static string FormatChangeType(PatchChangeType changeType) => changeType switch
    {
        PatchChangeType.Added => "A",
        PatchChangeType.Deleted => "D",
        _ => "M"
    };

    private sealed class PatchEntry
    {
        public string OriginalPath { get; set; } = string.Empty;

        public string NewPath { get; set; } = string.Empty;

        public PatchChangeType ChangeType { get; set; } = PatchChangeType.Modified;

        public string DisplayPath => !string.IsNullOrWhiteSpace(NewPath) && !string.Equals(NewPath, "/dev/null", StringComparison.Ordinal)
            ? NewPath
            : OriginalPath;
    }

    private enum PatchChangeType
    {
        Added,
        Modified,
        Deleted,
        Unsupported
    }
}
