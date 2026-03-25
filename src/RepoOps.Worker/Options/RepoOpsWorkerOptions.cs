namespace RepoOps.Worker.Options;

public sealed class RepoOpsWorkerOptions
{
    public const string SectionName = "RepoOps:Worker";

    public bool ContinuousModeEnabled { get; set; } = true;

    public int LoopIntervalSeconds { get; set; } = 300;

    public string InputSource { get; set; } = "worker-dotnet";

    public string ReportOutputPath { get; set; } = "reports/worker-summary.json";

    public string SummaryTextOutputPath { get; set; } = "reports/worker-summary.txt";
}
