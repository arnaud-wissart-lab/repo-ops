namespace RepoOps.Worker.Options;

public sealed class AutoMergeOptions
{
    public const string SectionName = "RepoOps:AutoMerge";

    public bool Enabled { get; set; }

    public bool DryRunEnabled { get; set; } = true;

    public string MergeMethod { get; set; } = "squash";

    public string[] AllowedUpdateTypes { get; set; } = ["patch"];

    public string[] AllowedMergeableStates { get; set; } = ["clean"];

    public string PolicyFilePath { get; set; } = string.Empty;

    public List<RepositoryAutoMergePolicy> RepositoryPolicies { get; set; } = [];
}

public sealed class RepositoryAutoMergePolicy
{
    public string Repository { get; set; } = string.Empty;

    public bool? AllowAutoMerge { get; set; }

    public bool ReviewRequired { get; set; }

    public bool ReadOnly { get; set; }

    public string MergeMethod { get; set; } = string.Empty;

    public string[] AllowedUpdateTypes { get; set; } = [];
}
