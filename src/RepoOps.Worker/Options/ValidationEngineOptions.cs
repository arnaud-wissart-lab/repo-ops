namespace RepoOps.Worker.Options;

public sealed class ValidationEngineOptions
{
    public const string SectionName = "RepoOps:Validation";

    public string OutputPath { get; set; } = "reports/supervisor-validations.json";

    public string DigestOutputPath { get; set; } = "reports/supervisor-validations.txt";

    public string InputResponsePath { get; set; } = "reports/supervisor-codex-responses.json";

    public string? InputValidationPath { get; set; }

    public bool InteractiveMode { get; set; }
}
