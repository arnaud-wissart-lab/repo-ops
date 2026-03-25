using System.Net;

namespace RepoOps.Worker.Clients;

public sealed class GitHubApiException(
    string message,
    HttpStatusCode? statusCode = null,
    Exception? innerException = null) : Exception(message, innerException)
{
    public HttpStatusCode? StatusCode { get; } = statusCode;
}
