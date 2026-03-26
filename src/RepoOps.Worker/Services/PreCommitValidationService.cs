using System.Text;
using Microsoft.Extensions.Options;
using RepoOps.Worker.Models;
using RepoOps.Worker.Options;

namespace RepoOps.Worker.Services;

public sealed class PreCommitValidationService(
    IProcessCommandRunner processCommandRunner,
    ILogger<PreCommitValidationService> logger,
    IOptions<CommitEngineOptions> options)
{
    public async Task<PreCommitValidationResult> ValidateAsync(
        string workspacePath,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!settings.PreCommitValidationEnabled)
        {
            return new PreCommitValidationResult
            {
                Status = CommitValidationStatus.Skipped,
                Output = "La validation avant commit est désactivée."
            };
        }

        var command = ResolveCommand(workspacePath, settings);
        if (command is null)
        {
            return new PreCommitValidationResult
            {
                Status = CommitValidationStatus.NotRun,
                Output = "Aucune validation adaptée n'a été détectée pour ce dépôt."
            };
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(settings.PreCommitValidationTimeoutSeconds));

        try
        {
            var result = await processCommandRunner.RunAsync(
                command.Value.FileName,
                command.Value.Arguments,
                workspacePath,
                timeoutCts.Token);
            var output = BuildOutput(result);

            if (result.ExitCode != 0)
            {
                logger.LogWarning(
                    "Validation avant commit en échec pour {WorkspacePath} avec {Command}",
                    workspacePath,
                    $"{command.Value.FileName} {command.Value.Arguments}".Trim());

                return new PreCommitValidationResult
                {
                    Status = CommitValidationStatus.Failed,
                    Command = $"{command.Value.FileName} {command.Value.Arguments}".Trim(),
                    Output = output
                };
            }

            return new PreCommitValidationResult
            {
                Status = CommitValidationStatus.Succeeded,
                Command = $"{command.Value.FileName} {command.Value.Arguments}".Trim(),
                Output = output
            };
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new PreCommitValidationResult
            {
                Status = CommitValidationStatus.Failed,
                Command = $"{command.Value.FileName} {command.Value.Arguments}".Trim(),
                Output = $"Le délai de validation de {settings.PreCommitValidationTimeoutSeconds} seconde(s) a été dépassé."
            };
        }
    }

    private static (string FileName, string Arguments)? ResolveCommand(
        string workspacePath,
        CommitEngineOptions settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.PreCommitValidationCommand))
        {
            return (settings.PreCommitValidationCommand, settings.PreCommitValidationArguments);
        }

        var hasDotNetProject = Directory.EnumerateFiles(workspacePath, "*.sln", SearchOption.AllDirectories).Any()
            || Directory.EnumerateFiles(workspacePath, "*.csproj", SearchOption.AllDirectories).Any();

        return hasDotNetProject
            ? ("dotnet", "build --nologo")
            : null;
    }

    private static string BuildOutput(ProcessCommandResult result)
    {
        var builder = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            builder.AppendLine(result.StandardOutput.TrimEnd());
        }

        if (!string.IsNullOrWhiteSpace(result.StandardError))
        {
            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.AppendLine(result.StandardError.TrimEnd());
        }

        if (builder.Length == 0)
        {
            builder.Append("La commande n'a produit aucune sortie.");
        }

        return builder.ToString().TrimEnd();
    }
}
