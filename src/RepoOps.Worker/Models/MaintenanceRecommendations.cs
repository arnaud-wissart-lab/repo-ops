namespace RepoOps.Worker.Models;

public sealed class MaintenanceRecommendations
{
    public IReadOnlyList<string> ManualActions { get; init; } = Array.Empty<string>();
}
