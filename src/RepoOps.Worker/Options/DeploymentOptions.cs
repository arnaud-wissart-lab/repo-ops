namespace RepoOps.Worker.Options;

public sealed class DeploymentOptions
{
    public const string SectionName = "RepoOps:Deployment";

    public bool Enabled { get; set; } = true;

    public bool DryRunEnabled { get; set; } = true;

    public string TargetName { get; set; } = "Machine locale";

    public string VerificationUrl { get; set; } = string.Empty;

    public string Command { get; set; } = "powershell";

    public string Arguments { get; set; } = "-NoProfile -ExecutionPolicy Bypass -File scripts/deploy-local.ps1";

    public string DryRunArguments { get; set; } = "-DryRun";

    public int TimeoutSeconds { get; set; } = 1200;

    public string WorkingDirectory { get; set; } = string.Empty;

    public string OutputPath { get; set; } = "reports/deployment-execution.json";
}
