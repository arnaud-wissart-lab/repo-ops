using System.Text.Json.Serialization;

namespace RepoOps.Worker.Models;

public sealed class GitHubDependabotAlertDto
{
    public int Number { get; init; }

    public string State { get; init; } = string.Empty;

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; init; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonPropertyName("fixed_at")]
    public DateTimeOffset? FixedAt { get; init; }

    public GitHubDependabotDependencyDto Dependency { get; init; } = new();

    [JsonPropertyName("security_advisory")]
    public GitHubSecurityAdvisoryDto SecurityAdvisory { get; init; } = new();

    [JsonPropertyName("security_vulnerability")]
    public GitHubSecurityVulnerabilityDto SecurityVulnerability { get; init; } = new();
}

public sealed class GitHubDependabotDependencyDto
{
    public GitHubPackageDto Package { get; init; } = new();

    [JsonPropertyName("manifest_path")]
    public string ManifestPath { get; init; } = string.Empty;

    public string Scope { get; init; } = string.Empty;
}

public sealed class GitHubPackageDto
{
    public string Ecosystem { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;
}

public sealed class GitHubSecurityAdvisoryDto
{
    [JsonPropertyName("ghsa_id")]
    public string GhsaId { get; init; } = string.Empty;

    [JsonPropertyName("cve_id")]
    public string CveId { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string Severity { get; init; } = string.Empty;
}

public sealed class GitHubSecurityVulnerabilityDto
{
    public GitHubPackageDto Package { get; init; } = new();

    public string Severity { get; init; } = string.Empty;

    [JsonPropertyName("vulnerable_version_range")]
    public string VulnerableVersionRange { get; init; } = string.Empty;

    [JsonPropertyName("first_patched_version")]
    public GitHubFirstPatchedVersionDto? FirstPatchedVersion { get; init; }
}

public sealed class GitHubFirstPatchedVersionDto
{
    public string Identifier { get; init; } = string.Empty;
}
