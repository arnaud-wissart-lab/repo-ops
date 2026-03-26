namespace RepoOps.Worker.Options;

public sealed class RepoOpsWorkerOptions
{
    public const string SectionName = "RepoOps:Worker";

    public int HttpPort { get; set; } = 8080;

    public int ExecutionTimeoutSeconds { get; set; } = 1800;

    public string InputSource { get; set; } = "worker-dotnet";

    public bool TriggerRenovateExecution { get; set; }

    public bool EmitJsonToStdout { get; set; }

    public string ReportOutputPath { get; set; } = "reports/worker-summary.json";

    public string SummaryTextOutputPath { get; set; } = "reports/worker-summary.txt";

    public string SummaryHtmlOutputPath { get; set; } = "reports/worker-summary.html";

    public string SupervisorOutputPath { get; set; } = "reports/supervisor-decisions.json";

    public string SupervisorDigestOutputPath { get; set; } = "reports/supervisor-decisions.txt";

    public string SupervisorPromptOutputPath { get; set; } = "reports/supervisor-prompts.json";

    public string SupervisorPromptDigestOutputPath { get; set; } = "reports/supervisor-prompts.txt";

    public string? SupervisorInputReportPath { get; set; }

    public string? SupervisorInputDecisionPath { get; set; }
}
