namespace RepoOps.Worker.Models;

public sealed class MaintenanceMessages
{
    public IReadOnlyList<string> Logs { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Notes { get; init; } = Array.Empty<string>();
}
