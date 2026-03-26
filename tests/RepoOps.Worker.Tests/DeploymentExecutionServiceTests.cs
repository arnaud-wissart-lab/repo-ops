using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RepoOps.Worker.Models;
using RepoOps.Worker.Options;
using RepoOps.Worker.Services;
using Xunit;

namespace RepoOps.Worker.Tests;

public sealed class DeploymentExecutionServiceTests
{
    [Fact]
    public async Task ExecuteAsync_RefuseUnDeploiementDesactive()
    {
        var settings = new DeploymentOptions
        {
            Enabled = false,
            TargetName = "Machine locale de test"
        };

        var service = new DeploymentExecutionService(
            Microsoft.Extensions.Options.Options.Create(settings),
            new RecordingProcessCommandRunner(),
            NullLogger<DeploymentExecutionService>.Instance);

        var result = await service.ExecuteAsync(
            new DeploymentRunRequest { RequestedBy = "test" },
            CancellationToken.None);

        Assert.Equal("Disabled", result.Status);
        Assert.Equal("Skipped", result.VerificationStatus);
        Assert.Contains("désactivé", result.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_AjouteLesArgumentsDeDryRun()
    {
        var workingDirectory = CreateTemporaryDirectory();

        try
        {
            var runner = new RecordingProcessCommandRunner();
            var settings = new DeploymentOptions
            {
                Enabled = true,
                DryRunEnabled = true,
                VerificationUrl = "https://repoops.arnaudwissart.fr",
                Command = "powershell",
                Arguments = "-File scripts/deploy-local.ps1",
                DryRunArguments = "-DryRun",
                WorkingDirectory = workingDirectory,
                OutputPath = Path.Combine(workingDirectory, "deployment-execution.json")
            };

            var service = new DeploymentExecutionService(
                Microsoft.Extensions.Options.Options.Create(settings),
                runner,
                NullLogger<DeploymentExecutionService>.Instance);

            var result = await service.ExecuteAsync(
                new DeploymentRunRequest { RequestedBy = "test" },
                CancellationToken.None);

            Assert.Equal("DryRun", result.Status);
            Assert.Equal("Skipped", result.VerificationStatus);
            Assert.Contains("dry-run", result.VerificationMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("powershell", runner.LastFileName);
            Assert.Contains("-DryRun", runner.LastArguments, StringComparison.Ordinal);
            Assert.True(File.Exists(settings.OutputPath));
        }
        finally
        {
            Directory.Delete(workingDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_ConserveLeModeReelSiDryRunDesactive()
    {
        var workingDirectory = CreateTemporaryDirectory();

        try
        {
            var runner = new RecordingProcessCommandRunner();
            var settings = new DeploymentOptions
            {
                Enabled = true,
                DryRunEnabled = false,
                Command = "powershell",
                Arguments = "-File scripts/deploy-local.ps1",
                DryRunArguments = "-DryRun",
                WorkingDirectory = workingDirectory,
                OutputPath = Path.Combine(workingDirectory, "deployment-execution.json")
            };

            var service = new DeploymentExecutionService(
                Microsoft.Extensions.Options.Options.Create(settings),
                runner,
                NullLogger<DeploymentExecutionService>.Instance);

            var result = await service.ExecuteAsync(
                new DeploymentRunRequest { RequestedBy = "test" },
                CancellationToken.None);

            Assert.Equal("Succeeded", result.Status);
            Assert.DoesNotContain("-DryRun", runner.LastArguments, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(workingDirectory, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"repo-ops-deploy-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private sealed class RecordingProcessCommandRunner : IProcessCommandRunner
    {
        public string LastFileName { get; private set; } = string.Empty;

        public string LastArguments { get; private set; } = string.Empty;

        public Task<ProcessCommandResult> RunAsync(
            string fileName,
            string arguments,
            string workingDirectory,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastFileName = fileName;
            LastArguments = arguments;

            return Task.FromResult(new ProcessCommandResult(
                0,
                "[deploy] Simulation ok",
                string.Empty));
        }
    }
}
