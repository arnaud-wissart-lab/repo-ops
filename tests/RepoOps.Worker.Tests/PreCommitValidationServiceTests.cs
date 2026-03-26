using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RepoOps.Worker.Models;
using RepoOps.Worker.Options;
using RepoOps.Worker.Services;
using Xunit;

namespace RepoOps.Worker.Tests;

public sealed class PreCommitValidationServiceTests : IDisposable
{
    private readonly string workspacePath;

    public PreCommitValidationServiceTests()
    {
        workspacePath = Path.Combine(Path.GetTempPath(), $"repo-ops-validation-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workspacePath);
    }

    [Fact]
    public async Task ValidationDotNet_Reussit_SiLaCommandePasse()
    {
        File.WriteAllText(Path.Combine(workspacePath, "sample.csproj"), "<Project />");
        var service = new PreCommitValidationService(
            new FakeProcessCommandRunner(_ => new ProcessCommandResult(0, "Build succeeded.", string.Empty)),
            NullLogger<PreCommitValidationService>.Instance,
            Microsoft.Extensions.Options.Options.Create(new CommitEngineOptions()));

        var result = await service.ValidateAsync(workspacePath, CancellationToken.None);

        Assert.Equal(CommitValidationStatus.Succeeded, result.Status);
        Assert.Equal("dotnet build --nologo", result.Command);
    }

    [Fact]
    public async Task ValidationConfigurable_Echoue_SiLaCommandeRetourneUneErreur()
    {
        var service = new PreCommitValidationService(
            new FakeProcessCommandRunner(_ => new ProcessCommandResult(1, string.Empty, "Build failed.")),
            NullLogger<PreCommitValidationService>.Instance,
            Microsoft.Extensions.Options.Options.Create(new CommitEngineOptions
            {
                PreCommitValidationCommand = "dotnet",
                PreCommitValidationArguments = "build"
            }));

        var result = await service.ValidateAsync(workspacePath, CancellationToken.None);

        Assert.Equal(CommitValidationStatus.Failed, result.Status);
        Assert.Contains("Build failed.", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ValidationIgnoree_SiAucunProjetEtAucuneCommande()
    {
        var service = new PreCommitValidationService(
            new FakeProcessCommandRunner(_ => throw new InvalidOperationException("Aucune exécution attendue.")),
            NullLogger<PreCommitValidationService>.Instance,
            Microsoft.Extensions.Options.Options.Create(new CommitEngineOptions()));

        var result = await service.ValidateAsync(workspacePath, CancellationToken.None);

        Assert.Equal(CommitValidationStatus.NotRun, result.Status);
    }

    public void Dispose()
    {
        if (Directory.Exists(workspacePath))
        {
            Directory.Delete(workspacePath, recursive: true);
        }
    }

    private sealed class FakeProcessCommandRunner(Func<(string FileName, string Arguments, string WorkingDirectory), ProcessCommandResult> factory) : IProcessCommandRunner
    {
        public Task<ProcessCommandResult> RunAsync(
            string fileName,
            string arguments,
            string workingDirectory,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(factory((fileName, arguments, workingDirectory)));
        }
    }
}
