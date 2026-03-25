namespace RepoOps.Worker.Models;

public sealed class MaintenanceDigest
{
    public string Subject { get; init; } = string.Empty;

    public string PlainTextBody { get; init; } = string.Empty;

    public string HtmlBody { get; init; } = string.Empty;
}
