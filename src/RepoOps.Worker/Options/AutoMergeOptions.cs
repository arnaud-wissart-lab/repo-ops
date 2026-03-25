namespace RepoOps.Worker.Options;

public sealed class AutoMergeOptions
{
    public const string SectionName = "RepoOps:AutoMerge";

    public bool Enabled { get; set; }

    public bool DryRunEnabled { get; set; } = true;

    public string MergeMethod { get; set; } = "squash";

    public string[] AllowedUpdateTypes { get; set; } = ["patch"];
}
