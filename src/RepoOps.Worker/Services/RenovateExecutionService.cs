using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RepoOps.Worker.Models;
using RepoOps.Worker.Options;

namespace RepoOps.Worker.Services;

public sealed class RenovateExecutionService(
    IOptions<RenovateExecutionOptions> options,
    ILogger<RenovateExecutionService> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<RenovateExecutionDetails> ResolveAsync(
        bool triggerRequested,
        CancellationToken cancellationToken)
    {
        if (triggerRequested)
        {
            var details = await ExecuteAsync(cancellationToken);
            await PersistAsync(details, cancellationToken);
            return details;
        }

        return await LoadLatestOrDefaultAsync(cancellationToken);
    }

    private async Task<RenovateExecutionDetails> ExecuteAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var startedAtUtc = DateTimeOffset.UtcNow;
        var stdoutLines = new List<string>();
        var stderrLines = new List<string>();
        var workingDirectory = ResolveWorkingDirectory(settings.WorkingDirectory);
        var commandLine = BuildCommandLine(settings);

        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            return BuildFailure(
                startedAtUtc,
                commandLine,
                "Impossible de localiser le répertoire contenant docker-compose.yml pour déclencher Renovate.",
                []);
        }

        using var process = new Process
        {
            StartInfo = CreateStartInfo(settings, workingDirectory)
        };

        try
        {
            if (!process.Start())
            {
                return BuildFailure(
                    startedAtUtc,
                    commandLine,
                    "Le processus Renovate n'a pas pu être démarré.",
                    []);
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Échec de démarrage du processus Renovate");
            return BuildFailure(
                startedAtUtc,
                commandLine,
                $"Le processus Renovate n'a pas pu être démarré : {exception.Message}",
                []);
        }

        process.OutputDataReceived += (_, eventArgs) =>
        {
            if (!string.IsNullOrWhiteSpace(eventArgs.Data))
            {
                CaptureLine(stdoutLines, eventArgs.Data, settings.MaxCapturedLines);
            }
        };

        process.ErrorDataReceived += (_, eventArgs) =>
        {
            if (!string.IsNullOrWhiteSpace(eventArgs.Data))
            {
                CaptureLine(stderrLines, eventArgs.Data, settings.MaxCapturedLines);
            }
        };

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var timeoutCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));

        try
        {
            await process.WaitForExitAsync(timeoutCancellationTokenSource.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryKill(process);

            return new RenovateExecutionDetails
            {
                Status = "Failed",
                TriggerRequested = true,
                StartedAtUtc = startedAtUtc,
                FinishedAtUtc = DateTimeOffset.UtcNow,
                DurationSeconds = (DateTimeOffset.UtcNow - startedAtUtc).TotalSeconds,
                Mode = "worker-explicit-command",
                Command = commandLine,
                ExitCode = null,
                Summary = $"Renovate a dépassé le timeout configuré de {settings.TimeoutSeconds} seconde(s).",
                Logs = stdoutLines,
                Errors = AppendError(stderrLines, $"Timeout après {settings.TimeoutSeconds} seconde(s).")
            };
        }

        var finishedAtUtc = DateTimeOffset.UtcNow;
        var status = ResolveStatus(process.ExitCode, stdoutLines, stderrLines);

        return new RenovateExecutionDetails
        {
            Status = status,
            TriggerRequested = true,
            StartedAtUtc = startedAtUtc,
            FinishedAtUtc = finishedAtUtc,
            DurationSeconds = (finishedAtUtc - startedAtUtc).TotalSeconds,
            Mode = "worker-explicit-command",
            Command = commandLine,
            ExitCode = process.ExitCode,
            Summary = BuildSummary(status, process.ExitCode),
            Logs = stdoutLines,
            Errors = stderrLines
        };
    }

    private async Task<RenovateExecutionDetails> LoadLatestOrDefaultAsync(CancellationToken cancellationToken)
    {
        var outputPath = Path.GetFullPath(options.Value.OutputPath);

        if (!File.Exists(outputPath))
        {
            return new RenovateExecutionDetails
            {
                Status = "NotTriggered",
                TriggerRequested = false,
                IncludedFromLatestKnownExecution = false,
                Mode = "daily-report-no-trigger",
                Summary = "Renovate n'a pas été déclenché dans ce cycle et aucune exécution antérieure n'est disponible."
            };
        }

        try
        {
            await using var stream = File.OpenRead(outputPath);
            var details = await JsonSerializer.DeserializeAsync<RenovateExecutionDetails>(stream, JsonOptions, cancellationToken);

            if (details is null)
            {
                return new RenovateExecutionDetails
                {
                    Status = "NotTriggered",
                    TriggerRequested = false,
                    IncludedFromLatestKnownExecution = false,
                    Mode = "daily-report-no-trigger",
                    Summary = "Renovate n'a pas été déclenché dans ce cycle et le dernier artefact connu est vide."
                };
            }

            return new RenovateExecutionDetails
            {
                Status = details.Status,
                TriggerRequested = false,
                IncludedFromLatestKnownExecution = true,
                StartedAtUtc = details.StartedAtUtc,
                FinishedAtUtc = details.FinishedAtUtc,
                DurationSeconds = details.DurationSeconds,
                Mode = "daily-report-last-known",
                Command = details.Command,
                ExitCode = details.ExitCode,
                Summary = $"Renovate n'a pas été déclenché dans ce cycle. Dernier résultat connu : {details.Summary}",
                Logs = details.Logs,
                Errors = details.Errors
            };
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Impossible de relire le dernier artefact d'exécution Renovate");
            return new RenovateExecutionDetails
            {
                Status = "NotTriggered",
                TriggerRequested = false,
                IncludedFromLatestKnownExecution = false,
                Mode = "daily-report-no-trigger",
                Summary = $"Renovate n'a pas été déclenché dans ce cycle et le dernier artefact connu est illisible : {exception.Message}",
                Errors = [exception.Message]
            };
        }
    }

    private async Task PersistAsync(RenovateExecutionDetails details, CancellationToken cancellationToken)
    {
        var outputPath = Path.GetFullPath(options.Value.OutputPath);
        var directoryPath = Path.GetDirectoryName(outputPath);

        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        await File.WriteAllTextAsync(
            outputPath,
            JsonSerializer.Serialize(details, JsonOptions),
            cancellationToken);
    }

    private static ProcessStartInfo CreateStartInfo(RenovateExecutionOptions settings, string workingDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = settings.Command,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var token in TokenizeArguments(settings.Arguments))
        {
            startInfo.ArgumentList.Add(token);
        }

        return startInfo;
    }

    private static string ResolveWorkingDirectory(string configuredWorkingDirectory)
    {
        if (!string.IsNullOrWhiteSpace(configuredWorkingDirectory))
        {
            var fullConfiguredPath = Path.GetFullPath(configuredWorkingDirectory);
            return Directory.Exists(fullConfiguredPath) ? fullConfiguredPath : string.Empty;
        }

        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        while (currentDirectory is not null)
        {
            if (File.Exists(Path.Combine(currentDirectory.FullName, "docker-compose.yml")))
            {
                return currentDirectory.FullName;
            }

            currentDirectory = currentDirectory.Parent;
        }

        return string.Empty;
    }

    private static string BuildCommandLine(RenovateExecutionOptions settings)
    {
        return string.IsNullOrWhiteSpace(settings.Arguments)
            ? settings.Command
            : $"{settings.Command} {settings.Arguments}";
    }

    private static string ResolveStatus(
        int exitCode,
        IReadOnlyList<string> stdoutLines,
        IReadOnlyList<string> stderrLines)
    {
        if (exitCode != 0)
        {
            return "Failed";
        }

        var combinedOutput = string.Join(
            '\n',
            stdoutLines.Concat(stderrLines)).ToLowerInvariant();

        if (ContainsAny(combinedOutput,
            "pull request created",
            "pull request updated",
            "branch created",
            "branch updated",
            "pr created",
            "pr updated"))
        {
            return "PullRequestsUpdated";
        }

        if (ContainsAny(combinedOutput,
            "no updates",
            "no update",
            "no new upgrades",
            "0 flattened updates found",
            "nothing to update"))
        {
            return "NoUpdatesDetected";
        }

        return "Succeeded";
    }

    private static string BuildSummary(string status, int exitCode)
    {
        return status switch
        {
            "PullRequestsUpdated" => $"Renovate s'est exécuté avec succès et a signalé des créations ou mises à jour de PR. Code de sortie : {exitCode}.",
            "NoUpdatesDetected" => $"Renovate s'est exécuté avec succès et n'a détecté aucune mise à jour exploitable. Code de sortie : {exitCode}.",
            "Succeeded" => $"Renovate s'est exécuté avec succès. Code de sortie : {exitCode}.",
            _ => $"Renovate a échoué avec le code de sortie {exitCode}."
        };
    }

    private static bool ContainsAny(string haystack, params string[] needles)
    {
        return needles.Any(needle => haystack.Contains(needle, StringComparison.Ordinal));
    }

    private static void CaptureLine(ICollection<string> target, string line, int maxCapturedLines)
    {
        if (target.Count >= maxCapturedLines)
        {
            return;
        }

        target.Add(line);
    }

    private static IReadOnlyList<string> AppendError(
        IReadOnlyList<string> errors,
        string message)
    {
        return errors.Concat([message]).ToArray();
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Rien à faire ici, l'erreur est déjà reflétée dans le rapport.
        }
    }

    private static IReadOnlyList<string> TokenizeArguments(string arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            return [];
        }

        var tokens = new List<string>();
        var builder = new StringBuilder();
        var insideQuotes = false;

        foreach (var character in arguments)
        {
            if (character == '"')
            {
                insideQuotes = !insideQuotes;
                continue;
            }

            if (char.IsWhiteSpace(character) && !insideQuotes)
            {
                if (builder.Length > 0)
                {
                    tokens.Add(builder.ToString());
                    builder.Clear();
                }

                continue;
            }

            builder.Append(character);
        }

        if (builder.Length > 0)
        {
            tokens.Add(builder.ToString());
        }

        return tokens;
    }

    private static RenovateExecutionDetails BuildFailure(
        DateTimeOffset startedAtUtc,
        string commandLine,
        string summary,
        IReadOnlyList<string> errors)
    {
        return new RenovateExecutionDetails
        {
            Status = "Failed",
            TriggerRequested = true,
            StartedAtUtc = startedAtUtc,
            FinishedAtUtc = DateTimeOffset.UtcNow,
            DurationSeconds = (DateTimeOffset.UtcNow - startedAtUtc).TotalSeconds,
            Mode = "worker-explicit-command",
            Command = commandLine,
            ExitCode = null,
            Summary = summary,
            Errors = errors
        };
    }
}
