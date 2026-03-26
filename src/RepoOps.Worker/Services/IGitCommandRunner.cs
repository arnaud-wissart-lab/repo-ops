namespace RepoOps.Worker.Services;

public interface IGitCommandRunner
{
    Task<GitCommandResult> RunAsync(
        string workingDirectory,
        string arguments,
        string? standardInput,
        CancellationToken cancellationToken);
}

public sealed record GitCommandResult(
    int ExitCode,
    string StandardOutput,
    string StandardError);
