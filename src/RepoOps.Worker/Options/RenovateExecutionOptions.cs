namespace RepoOps.Worker.Options;

public sealed class RenovateExecutionOptions
{
    public const string SectionName = "RepoOps:Renovate";

    public string Command { get; set; } = "docker";

    public string Arguments { get; set; } = "compose --profile maintenance run --rm renovate";

    public int TimeoutSeconds { get; set; } = 1800;

    public int MaxCapturedLines { get; set; } = 200;

    public string OutputPath { get; set; } = "reports/renovate-execution.json";

    public string WorkingDirectory { get; set; } = string.Empty;
}
