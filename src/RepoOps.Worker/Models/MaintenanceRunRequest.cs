namespace RepoOps.Worker.Models;

public sealed class MaintenanceRunRequest
{
    public string InputSource { get; init; } = "http-api";

    public bool TriggerRenovateExecution { get; init; }
}
