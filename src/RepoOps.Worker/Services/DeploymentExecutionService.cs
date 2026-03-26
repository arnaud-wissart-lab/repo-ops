using System.Text.Json;
using Microsoft.Extensions.Options;
using RepoOps.Worker.Models;
using RepoOps.Worker.Options;

namespace RepoOps.Worker.Services;

public sealed class DeploymentExecutionService(
    IOptions<DeploymentOptions> options,
    IProcessCommandRunner processCommandRunner,
    ILogger<DeploymentExecutionService> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<DeploymentExecutionResult> ExecuteAsync(
        DeploymentRunRequest? request,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var effectiveRequest = request ?? new DeploymentRunRequest();
        var dryRunEnabled = effectiveRequest.DryRun ?? settings.DryRunEnabled;
        var startedAtUtc = DateTimeOffset.UtcNow;
        var commandLine = BuildCommandLine(settings, dryRunEnabled);
        var workingDirectory = ResolveWorkingDirectory(settings.WorkingDirectory);

        if (!settings.Enabled)
        {
            return await PersistAsync(new DeploymentExecutionResult
            {
                Status = "Disabled",
                RequestedBy = effectiveRequest.RequestedBy,
                TargetName = settings.TargetName,
                DryRunEnabled = dryRunEnabled,
                StartedAtUtc = startedAtUtc,
                FinishedAtUtc = DateTimeOffset.UtcNow,
                DurationSeconds = 0,
                Command = commandLine,
                WorkingDirectory = workingDirectory,
                Summary = "Le déploiement local est désactivé par configuration.",
                Errors = ["Activez DEPLOYMENT_ENABLED pour autoriser le bouton de déploiement."]
            }, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            return await PersistAsync(new DeploymentExecutionResult
            {
                Status = "Failed",
                RequestedBy = effectiveRequest.RequestedBy,
                TargetName = settings.TargetName,
                DryRunEnabled = dryRunEnabled,
                StartedAtUtc = startedAtUtc,
                FinishedAtUtc = DateTimeOffset.UtcNow,
                DurationSeconds = (DateTimeOffset.UtcNow - startedAtUtc).TotalSeconds,
                Command = commandLine,
                Summary = "Impossible de localiser le répertoire du dépôt pour déclencher le déploiement.",
                Errors = ["Le répertoire contenant docker-compose.yml n'a pas été trouvé."]
            }, cancellationToken);
        }

        try
        {
            using var timeoutCancellationTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));

            var result = await processCommandRunner.RunAsync(
                settings.Command,
                BuildArguments(settings, dryRunEnabled),
                workingDirectory,
                timeoutCancellationTokenSource.Token);

            var finishedAtUtc = DateTimeOffset.UtcNow;
            var logs = ExtractLines(result.StandardOutput);
            var errors = ExtractLines(result.StandardError);
            var status = result.ExitCode == 0
                ? dryRunEnabled ? "DryRun" : "Succeeded"
                : "Failed";

            return await PersistAsync(new DeploymentExecutionResult
            {
                Status = status,
                RequestedBy = effectiveRequest.RequestedBy,
                TargetName = settings.TargetName,
                DryRunEnabled = dryRunEnabled,
                StartedAtUtc = startedAtUtc,
                FinishedAtUtc = finishedAtUtc,
                DurationSeconds = (finishedAtUtc - startedAtUtc).TotalSeconds,
                Command = commandLine,
                WorkingDirectory = workingDirectory,
                ExitCode = result.ExitCode,
                Summary = BuildSummary(status, settings.TargetName, dryRunEnabled, result.ExitCode),
                Logs = logs,
                Errors = errors
            }, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return await PersistAsync(new DeploymentExecutionResult
            {
                Status = "Failed",
                RequestedBy = effectiveRequest.RequestedBy,
                TargetName = settings.TargetName,
                DryRunEnabled = dryRunEnabled,
                StartedAtUtc = startedAtUtc,
                FinishedAtUtc = DateTimeOffset.UtcNow,
                DurationSeconds = (DateTimeOffset.UtcNow - startedAtUtc).TotalSeconds,
                Command = commandLine,
                WorkingDirectory = workingDirectory,
                Summary = $"Le déploiement a dépassé le délai autorisé de {settings.TimeoutSeconds} seconde(s).",
                Errors = [$"Timeout après {settings.TimeoutSeconds} seconde(s)."]
            }, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Le déclenchement du déploiement local a échoué");

            return await PersistAsync(new DeploymentExecutionResult
            {
                Status = "Failed",
                RequestedBy = effectiveRequest.RequestedBy,
                TargetName = settings.TargetName,
                DryRunEnabled = dryRunEnabled,
                StartedAtUtc = startedAtUtc,
                FinishedAtUtc = DateTimeOffset.UtcNow,
                DurationSeconds = (DateTimeOffset.UtcNow - startedAtUtc).TotalSeconds,
                Command = commandLine,
                WorkingDirectory = workingDirectory,
                Summary = $"Le déploiement local n'a pas pu être exécuté : {exception.Message}",
                Errors = [exception.Message]
            }, cancellationToken);
        }
    }

    private async Task<DeploymentExecutionResult> PersistAsync(
        DeploymentExecutionResult result,
        CancellationToken cancellationToken)
    {
        var outputPath = Path.GetFullPath(options.Value.OutputPath);
        var directoryPath = Path.GetDirectoryName(outputPath);

        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        await File.WriteAllTextAsync(
            outputPath,
            JsonSerializer.Serialize(result, JsonOptions),
            cancellationToken);

        return result;
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

    private static string BuildArguments(DeploymentOptions settings, bool dryRunEnabled)
    {
        if (!dryRunEnabled || string.IsNullOrWhiteSpace(settings.DryRunArguments))
        {
            return settings.Arguments;
        }

        return string.IsNullOrWhiteSpace(settings.Arguments)
            ? settings.DryRunArguments
            : $"{settings.Arguments} {settings.DryRunArguments}";
    }

    private static string BuildCommandLine(DeploymentOptions settings, bool dryRunEnabled)
    {
        var arguments = BuildArguments(settings, dryRunEnabled);
        return string.IsNullOrWhiteSpace(arguments)
            ? settings.Command
            : $"{settings.Command} {arguments}";
    }

    private static IReadOnlyList<string> ExtractLines(string content)
    {
        return content
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
    }

    private static string BuildSummary(string status, string targetName, bool dryRunEnabled, int exitCode)
    {
        return status switch
        {
            "DryRun" => $"Le déploiement a été simulé avec succès pour {targetName}. Code de sortie : {exitCode}.",
            "Succeeded" => $"Le déploiement a été exécuté avec succès sur {targetName}. Code de sortie : {exitCode}.",
            _ => $"Le déploiement a échoué sur {targetName}. Code de sortie : {exitCode}."
        };
    }
}
