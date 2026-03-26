namespace RepoOps.Worker.Models;

public sealed class DeploymentRunRequest
{
    public string RequestedBy { get; init; } = "demo-ui";

    public bool? DryRun { get; init; }
}
