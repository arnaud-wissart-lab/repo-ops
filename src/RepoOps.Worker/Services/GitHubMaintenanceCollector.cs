using System.Net;
using Microsoft.Extensions.Options;
using RepoOps.Worker.Clients;
using RepoOps.Worker.Models;
using RepoOps.Worker.Options;

namespace RepoOps.Worker.Services;

public sealed class GitHubMaintenanceCollector(
    GitHubApiClient gitHubApiClient,
    IOptions<GitHubOptions> options,
    ILogger<GitHubMaintenanceCollector> logger)
{
    private static readonly HashSet<string> RenovateLogins = new(StringComparer.OrdinalIgnoreCase)
    {
        "renovate[bot]",
        "app/renovate",
        "renovate-bot"
    };

    public async Task<GitHubCollectionResult> CollectAsync(
        IReadOnlyList<string> configuredRepositories,
        CancellationToken cancellationToken)
    {
        var logs = new List<string>();
        var notes = new List<string>();
        var manualActions = new List<string>();
        var scannedRepositories = new List<string>();
        var createdPullRequests = new List<string>();
        var mergedPullRequests = new List<string>();
        var failedPullRequests = new List<string>();
        var remainingVulnerabilities = new List<string>();
        var repositoryFailures = 0;
        var partialDataDetected = false;

        if (configuredRepositories.Count == 0)
        {
            logs.Add("[github] Aucun dépôt n'est configuré dans RENOVATE_REPOSITORIES.");
            notes.Add("Aucun dépôt n'a été ciblé, la collecte GitHub ne peut pas démarrer.");
            manualActions.Add("Renseigner au moins un dépôt dans RENOVATE_REPOSITORIES.");

            return BuildResult(
                "Failed",
                scannedRepositories,
                createdPullRequests,
                mergedPullRequests,
                failedPullRequests,
                remainingVulnerabilities,
                logs,
                notes,
                manualActions);
        }

        if (string.IsNullOrWhiteSpace(options.Value.Token))
        {
            logs.Add("[github] GITHUB_TOKEN est absent ; la collecte GitHub est impossible.");
            notes.Add("Le worker ne peut pas interroger GitHub tant que GITHUB_TOKEN n'est pas fourni.");
            manualActions.Add("Renseigner GITHUB_TOKEN avec un jeton autorisé à lire les dépôts ciblés.");

            return BuildResult(
                "Failed",
                scannedRepositories,
                createdPullRequests,
                mergedPullRequests,
                failedPullRequests,
                remainingVulnerabilities,
                logs,
                notes,
                manualActions);
        }

        var mergedThreshold = DateTimeOffset.UtcNow.AddDays(-Math.Abs(options.Value.RecentMergedWindowDays));
        logs.Add($"[github] Fenêtre de fusion récente configurée sur {options.Value.RecentMergedWindowDays} jour(s).");

        foreach (var repository in configuredRepositories)
        {
            if (!TryParseRepository(repository, out var owner, out var repositoryName))
            {
                repositoryFailures++;
                partialDataDetected = true;
                logs.Add($"[github] Dépôt ignoré car invalide : {repository}");
                notes.Add($"Le dépôt {repository} n'a pas pu être traité car son format attendu est owner/repo.");
                continue;
            }

            try
            {
                var openPullRequests = await gitHubApiClient.GetPullRequestsAsync(
                    owner,
                    repositoryName,
                    "open",
                    cancellationToken);

                var closedPullRequests = await gitHubApiClient.GetPullRequestsAsync(
                    owner,
                    repositoryName,
                    "closed",
                    cancellationToken);

                var openRenovatePullRequests = openPullRequests
                    .Where(IsRenovatePullRequest)
                    .ToList();

                var mergedRenovatePullRequests = closedPullRequests
                    .Where(IsRenovatePullRequest)
                    .Where(pullRequest => pullRequest.MergedAt is not null && pullRequest.MergedAt >= mergedThreshold)
                    .ToList();

                foreach (var pullRequest in openRenovatePullRequests)
                {
                    createdPullRequests.Add(FormatPullRequest(repository, pullRequest));

                    if (string.IsNullOrWhiteSpace(pullRequest.Head.Sha))
                    {
                        partialDataDetected = true;
                        logs.Add($"[github] SHA absent pour la PR {repository}#{pullRequest.Number}, état des checks indisponible.");
                        continue;
                    }

                    try
                    {
                        var state = await gitHubApiClient.GetCombinedStatusStateAsync(
                            owner,
                            repositoryName,
                            pullRequest.Head.Sha,
                            cancellationToken);

                        if (string.Equals(state, "failure", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(state, "error", StringComparison.OrdinalIgnoreCase))
                        {
                            failedPullRequests.Add($"{FormatPullRequest(repository, pullRequest)} (checks: {state})");
                        }
                    }
                    catch (GitHubApiException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
                    {
                        partialDataDetected = true;
                        logs.Add($"[github] Checks indisponibles pour {repository}#{pullRequest.Number} : {exception.Message}");
                    }
                    catch (GitHubApiException exception)
                    {
                        partialDataDetected = true;
                        logs.Add($"[github] Impossible de lire l'état des checks pour {repository}#{pullRequest.Number} : {exception.Message}");
                    }
                }

                foreach (var pullRequest in mergedRenovatePullRequests)
                {
                    mergedPullRequests.Add(FormatPullRequest(repository, pullRequest));
                }

                scannedRepositories.Add(repository);
                logs.Add(
                    $"[github] {repository} scanné : {openRenovatePullRequests.Count} PR Renovate ouverte(s), {mergedRenovatePullRequests.Count} PR Renovate fusionnée(s) récemment.");
            }
            catch (GitHubApiException exception)
            {
                repositoryFailures++;
                partialDataDetected = true;
                logger.LogWarning(
                    exception,
                    "Échec de collecte GitHub sur {Repository}",
                    repository);
                logs.Add($"[github] Échec de collecte sur {repository} : {exception.Message}");
                notes.Add($"Le dépôt {repository} n'a pas pu être interrogé correctement.");
            }
        }

        notes.Add("La collecte des vulnérabilités reste non branchée dans cette étape et n'alimente pas encore les compteurs.");
        notes.Add("Le worker lit désormais GitHub pour les PR Renovate et les états de checks les plus simples.");

        manualActions.Add("Compléter plus tard la collecte des vulnérabilités avec une source GitHub dédiée.");

        if (repositoryFailures > 0)
        {
            manualActions.Add("Vérifier les permissions du jeton GitHub et l'accessibilité des dépôts en échec.");
        }

        var status = ResolveStatus(scannedRepositories.Count, repositoryFailures, partialDataDetected);

        if (status == "Success")
        {
            notes.Add("Toutes les interrogations GitHub prévues dans ce lot se sont terminées correctement.");
        }
        else if (status == "Partial")
        {
            notes.Add("La collecte GitHub a produit un résultat partiel : au moins un dépôt ou une donnée complémentaire n'a pas pu être traité.");
        }
        else
        {
            notes.Add("La collecte GitHub a échoué ou n'a produit aucun résultat exploitable.");
        }

        return BuildResult(
            status,
            scannedRepositories,
            createdPullRequests,
            mergedPullRequests,
            failedPullRequests,
            remainingVulnerabilities,
            logs,
            notes,
            manualActions);
    }

    private static GitHubCollectionResult BuildResult(
        string status,
        IReadOnlyList<string> scannedRepositories,
        IReadOnlyList<string> createdPullRequests,
        IReadOnlyList<string> mergedPullRequests,
        IReadOnlyList<string> failedPullRequests,
        IReadOnlyList<string> remainingVulnerabilities,
        IReadOnlyList<string> logs,
        IReadOnlyList<string> notes,
        IReadOnlyList<string> manualActions)
    {
        return new GitHubCollectionResult
        {
            Status = status,
            ScannedRepositories = scannedRepositories,
            CreatedPullRequests = createdPullRequests,
            MergedPullRequests = mergedPullRequests,
            FailedPullRequests = failedPullRequests,
            RemainingVulnerabilities = remainingVulnerabilities,
            Logs = logs,
            Notes = notes,
            ManualActions = manualActions
        };
    }

    private static string ResolveStatus(int scannedRepositoryCount, int repositoryFailures, bool partialDataDetected)
    {
        if (scannedRepositoryCount == 0)
        {
            return "Failed";
        }

        if (repositoryFailures > 0 || partialDataDetected)
        {
            return "Partial";
        }

        return "Success";
    }

    private static bool TryParseRepository(string repository, out string owner, out string name)
    {
        owner = string.Empty;
        name = string.Empty;

        var parts = repository.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        owner = parts[0];
        name = parts[1];
        return true;
    }

    private static bool IsRenovatePullRequest(GitHubPullRequestDto pullRequest)
    {
        return RenovateLogins.Contains(pullRequest.User.Login)
            || pullRequest.Head.Ref.StartsWith("renovate/", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatPullRequest(string repository, GitHubPullRequestDto pullRequest)
    {
        var title = string.IsNullOrWhiteSpace(pullRequest.Title)
            ? "Sans titre"
            : pullRequest.Title.Trim();

        return string.IsNullOrWhiteSpace(pullRequest.HtmlUrl)
            ? $"{repository}#{pullRequest.Number} - {title}"
            : $"{repository}#{pullRequest.Number} - {title} - {pullRequest.HtmlUrl}";
    }
}
