namespace RepoOps.Worker.Options;

public sealed class CodexExecutorOptions
{
    public const string SectionName = "RepoOps:Codex";

    public string Mode { get; set; } = "Stub";

    public string OutputPath { get; set; } = "reports/supervisor-codex-responses.json";

    public string DigestOutputPath { get; set; } = "reports/supervisor-codex-responses.txt";

    public string InputPromptPath { get; set; } = "reports/supervisor-prompts.json";
}
