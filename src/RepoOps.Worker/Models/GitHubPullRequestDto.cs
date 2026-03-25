using System.Text.Json.Serialization;

namespace RepoOps.Worker.Models;

public sealed class GitHubPullRequestDto
{
    public int Number { get; init; }

    public string Title { get; init; } = string.Empty;

    public string HtmlUrl { get; init; } = string.Empty;

    public bool Draft { get; init; }

    public DateTimeOffset? MergedAt { get; init; }

    public DateTimeOffset? ClosedAt { get; init; }

    public GitHubUserDto User { get; init; } = new();

    public GitHubPullRequestHeadDto Head { get; init; } = new();
}

public sealed class GitHubUserDto
{
    public string Login { get; init; } = string.Empty;
}

public sealed class GitHubPullRequestHeadDto
{
    public string Sha { get; init; } = string.Empty;

    public string Ref { get; init; } = string.Empty;
}

public sealed class GitHubCombinedStatusDto
{
    public string State { get; init; } = string.Empty;
}

public sealed class GitHubCheckRunsResponseDto
{
    public int TotalCount { get; init; }

    public IReadOnlyList<GitHubCheckRunDto> CheckRuns { get; init; } = Array.Empty<GitHubCheckRunDto>();
}

public sealed class GitHubCheckRunDto
{
    public string Name { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string Conclusion { get; init; } = string.Empty;

    public string HtmlUrl { get; init; } = string.Empty;
}

public sealed class GitHubApiErrorDto
{
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("documentation_url")]
    public string DocumentationUrl { get; init; } = string.Empty;
}
