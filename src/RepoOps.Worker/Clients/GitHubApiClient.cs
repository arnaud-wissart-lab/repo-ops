using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Options;
using RepoOps.Worker.Models;
using RepoOps.Worker.Options;

namespace RepoOps.Worker.Clients;

public sealed class GitHubApiClient(
    HttpClient httpClient,
    IOptions<GitHubOptions> options,
    ILogger<GitHubApiClient> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<GitHubPullRequestDto>> GetPullRequestsAsync(
        string owner,
        string repository,
        string state,
        CancellationToken cancellationToken)
    {
        var uri = $"repos/{owner}/{repository}/pulls?state={Uri.EscapeDataString(state)}&sort=updated&direction=desc&per_page=100";
        var pullRequests = await GetAsync<List<GitHubPullRequestDto>>(uri, cancellationToken);
        return pullRequests ?? [];
    }

    public async Task<string> GetCombinedStatusStateAsync(
        string owner,
        string repository,
        string sha,
        CancellationToken cancellationToken)
    {
        var uri = $"repos/{owner}/{repository}/commits/{Uri.EscapeDataString(sha)}/status";
        var payload = await GetAsync<GitHubCombinedStatusDto>(uri, cancellationToken);
        return payload?.State ?? string.Empty;
    }

    public async Task<IReadOnlyList<GitHubCheckRunDto>> GetCheckRunsAsync(
        string owner,
        string repository,
        string sha,
        CancellationToken cancellationToken)
    {
        var uri = $"repos/{owner}/{repository}/commits/{Uri.EscapeDataString(sha)}/check-runs?per_page=100";
        var payload = await GetAsync<GitHubCheckRunsResponseDto>(uri, cancellationToken);
        return payload?.CheckRuns ?? [];
    }

    public Task<GitHubPullRequestDto?> GetPullRequestDetailsAsync(
        string owner,
        string repository,
        int pullRequestNumber,
        CancellationToken cancellationToken)
    {
        var uri = $"repos/{owner}/{repository}/pulls/{pullRequestNumber}";
        return GetAsync<GitHubPullRequestDto>(uri, cancellationToken);
    }

    public Task<GitHubMergePullRequestResponseDto> MergePullRequestAsync(
        string owner,
        string repository,
        int pullRequestNumber,
        string mergeMethod,
        CancellationToken cancellationToken)
    {
        var uri = $"repos/{owner}/{repository}/pulls/{pullRequestNumber}/merge";
        var payload = new { merge_method = mergeMethod };
        return SendAsync<GitHubMergePullRequestResponseDto>(HttpMethod.Put, uri, payload, cancellationToken)!;
    }

    private async Task<T?> GetAsync<T>(string relativeUri, CancellationToken cancellationToken)
    {
        return await SendAsync<T>(HttpMethod.Get, relativeUri, payload: null, cancellationToken);
    }

    private async Task<T?> SendAsync<T>(
        HttpMethod method,
        string relativeUri,
        object? payload,
        CancellationToken cancellationToken)
    {
        ConfigureClient();

        using var request = new HttpRequestMessage(method, relativeUri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");

        if (payload is not null)
        {
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload, JsonOptions),
                Encoding.UTF8,
                "application/json");
        }

        HttpResponseMessage response;

        try
        {
            response = await httpClient.SendAsync(request, cancellationToken);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new GitHubApiException(
                "Le délai d'appel à l'API GitHub a été dépassé.",
                null,
                exception);
        }
        catch (HttpRequestException exception)
        {
            throw new GitHubApiException(
                "L'appel HTTP vers GitHub a échoué.",
                null,
                exception);
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await JsonSerializer.DeserializeAsync<GitHubApiErrorDto>(
                responseStream,
                JsonOptions,
                cancellationToken);

            var message = string.IsNullOrWhiteSpace(error?.Message)
                ? $"GitHub a répondu avec le statut HTTP {(int)response.StatusCode}."
                : $"GitHub a répondu avec le statut HTTP {(int)response.StatusCode} : {error.Message}";

            logger.LogWarning(
                "Réponse GitHub non valide sur {Uri} : {StatusCode}",
                relativeUri,
                response.StatusCode);

            throw new GitHubApiException(message, response.StatusCode);
        }

        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return default;
        }

        return await JsonSerializer.DeserializeAsync<T>(responseStream, JsonOptions, cancellationToken);
    }

    private void ConfigureClient()
    {
        var settings = options.Value;

        if (httpClient.BaseAddress is null
            || !string.Equals(httpClient.BaseAddress.ToString(), settings.ApiBaseUrl, StringComparison.OrdinalIgnoreCase))
        {
            httpClient.BaseAddress = new Uri(settings.ApiBaseUrl, UriKind.Absolute);
        }

        httpClient.DefaultRequestHeaders.UserAgent.Clear();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(settings.UserAgent);

        httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(settings.Token)
            ? null
            : new AuthenticationHeaderValue("Bearer", settings.Token);
    }
}
