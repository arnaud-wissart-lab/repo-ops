namespace RepoOps.Worker.Services;

public interface IProcessCommandRunner
{
    Task<ProcessCommandResult> RunAsync(
        string fileName,
        string arguments,
        string workingDirectory,
        CancellationToken cancellationToken);
}

public sealed record ProcessCommandResult(
    int ExitCode,
    string StandardOutput,
    string StandardError);
