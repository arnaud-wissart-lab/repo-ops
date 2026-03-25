namespace RepoOps.Worker.Options;

public sealed class RepoOpsWorkerOptions
{
    public const string SectionName = "RepoOps:Worker";

    public bool ContinuousModeEnabled { get; set; } = true;

    public bool RunOnStartup { get; set; } = true;

    public int LoopIntervalSeconds { get; set; } = 30;

    public string InputSource { get; set; } = "worker-dotnet";

    public bool EmitJsonToStdout { get; set; }

    public string ReportOutputPath { get; set; } = "reports/worker-summary.json";

    public string SummaryTextOutputPath { get; set; } = "reports/worker-summary.txt";

    public string SummaryHtmlOutputPath { get; set; } = "reports/worker-summary.html";

    public string TriggerFilePath { get; set; } = "runtime/daily-maintenance.trigger";
}
