using RepoOps.Worker.Services;
using Xunit;

namespace RepoOps.Worker.Tests;

public sealed class CommitPatchValidationServiceTests : IDisposable
{
    private readonly string workspacePath;
    private readonly CommitPatchValidationService service = new();

    public CommitPatchValidationServiceTests()
    {
        workspacePath = Path.Combine(Path.GetTempPath(), $"repo-ops-patch-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workspacePath);
        File.WriteAllText(Path.Combine(workspacePath, "README.md"), "ancien");
    }

    [Fact]
    public void PatchValide_ModificationSimple_AccepteLeDiff()
    {
        var result = service.Validate(
            """
            diff --git a/README.md b/README.md
            --- a/README.md
            +++ b/README.md
            @@ -1 +1 @@
            -ancien
            +nouveau
            """,
            workspacePath);

        Assert.True(result.IsValid);
        Assert.Contains("README.md", result.ModifiedFiles);
        Assert.Contains(result.DiffSummary, line => line.StartsWith("M README.md", StringComparison.Ordinal));
    }

    [Fact]
    public void PatchInvalide_CheminAmbigu_RefuseLeDiff()
    {
        var result = service.Validate(
            """
            diff --git a/../README.md b/../README.md
            --- a/../README.md
            +++ b/../README.md
            @@ -1 +1 @@
            -ancien
            +nouveau
            """,
            workspacePath);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("ambigu", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PatchInvalide_FichierIntrouvable_RefuseLaModification()
    {
        var result = service.Validate(
            """
            diff --git a/MISSING.md b/MISSING.md
            --- a/MISSING.md
            +++ b/MISSING.md
            @@ -1 +1 @@
            -ancien
            +nouveau
            """,
            workspacePath);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("introuvable", StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        if (Directory.Exists(workspacePath))
        {
            Directory.Delete(workspacePath, recursive: true);
        }
    }
}
