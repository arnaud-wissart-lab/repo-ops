namespace RepoOps.Worker.Options;

public sealed class GitHubOptions
{
    public const string SectionName = "RepoOps:GitHub";

    public string ApiBaseUrl { get; set; } = "https://api.github.com/";

    public string Token { get; set; } = string.Empty;

    public int RecentMergedWindowDays { get; set; } = 7;

    public string UserAgent { get; set; } = "RepoOps.Worker";
}
