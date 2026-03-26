using RepoOps.Worker.Models;

namespace RepoOps.Worker.Clients;

public interface ICodexClient
{
    string Mode { get; }

    Task<CodexClientResponse> ExecuteAsync(
        GeneratedPrompt prompt,
        CancellationToken cancellationToken);
}
