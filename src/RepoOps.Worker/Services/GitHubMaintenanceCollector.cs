using System.Net;
using Microsoft.Extensions.Options;
using RepoOps.Worker.Clients;
using RepoOps.Worker.Models;
using RepoOps.Worker.Options;

namespace RepoOps.Worker.Services;

public sealed class GitHubMaintenanceCollector(
    GitHubApiClient gitHubApiClient,
    IOptions<GitHubOptions> options,
    IOptions<AutoMergeOptions> autoMergeOptions,
    PullRequestDecisionService pullRequestDecisionService,
    PullRequestAutoMergeService pullRequestAutoMergeService,
    ILogger<GitHubMaintenanceCollector> logger)
{
    private static readonly HashSet<string> RenovateLogins = new(StringComparer.OrdinalIgnoreCase)
    {
        "renovate[bot]",
        "app/renovate",
        "renovate-bot"
    };

    private static readonly HashSet<string> SuccessfulCheckConclusions = new(StringComparer.OrdinalIgnoreCase)
    {
        "success",
        "neutral",
        "skipped"
    };

    private static readonly HashSet<string> FailedCheckConclusions = new(StringComparer.OrdinalIgnoreCase)
    {
        "action_required",
        "cancelled",
        "failure",
        "startup_failure",
        "stale",
        "timed_out"
    };

    private const string ReadyForReviewReason = "checks verts";
    private const string PendingChecksReason = "checks en attente";
    private const string FailedChecksReason = "checks en échec";
    private const string DraftReason = "brouillon";
    private const string UnknownChecksReason = "qualification incomplète";

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
        var readyForReview = new List<string>();
        var blocked = new List<string>();
        var failedChecks = new List<string>();
        var mergedRecently = new List<string>();
        var closedWithoutMerge = new List<string>();
        var readyForMerge = new List<string>();
        var manualReviewPullRequests = new List<string>();
        var blockedPullRequests = new List<string>();
        var failedAutoMergePullRequests = new List<string>();
        var autoMergedPullRequests = new List<string>();
        var mergeEvaluations = new List<PullRequestMergeEvaluation>();
        var repositoryFailures = 0;
        var autoMergeFailures = 0;
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
                new PullRequestStatuses(),
                new AutoMergeSummary
                {
                    Enabled = autoMergeOptions.Value.Enabled,
                    DryRunEnabled = autoMergeOptions.Value.DryRunEnabled,
                    MergeMethod = autoMergeOptions.Value.MergeMethod
                },
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
                new PullRequestStatuses(),
                new AutoMergeSummary
                {
                    Enabled = autoMergeOptions.Value.Enabled,
                    DryRunEnabled = autoMergeOptions.Value.DryRunEnabled,
                    MergeMethod = autoMergeOptions.Value.MergeMethod
                },
                logs,
                notes,
                manualActions);
        }

        var recentThreshold = DateTimeOffset.UtcNow.AddDays(-Math.Abs(options.Value.RecentMergedWindowDays));
        logs.Add($"[github] Fenêtre de corrélation récente configurée sur {options.Value.RecentMergedWindowDays} jour(s).");

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
                    .Where(pullRequest => pullRequest.MergedAt is not null && pullRequest.MergedAt >= recentThreshold)
                    .ToList();

                var closedRenovatePullRequests = closedPullRequests
                    .Where(IsRenovatePullRequest)
                    .Where(pullRequest => pullRequest.MergedAt is null && pullRequest.ClosedAt is not null && pullRequest.ClosedAt >= recentThreshold)
                    .ToList();

                foreach (var pullRequest in openRenovatePullRequests)
                {
                    var display = FormatPullRequest(repository, pullRequest);
                    createdPullRequests.Add(display);

                    var qualification = await QualifyOpenPullRequestAsync(
                        repository,
                        owner,
                        repositoryName,
                        pullRequest,
                        cancellationToken);

                    partialDataDetected |= qualification.IsPartial;
                    logs.AddRange(qualification.Logs);

                    bool? mergeable = null;
                    var mergeableState = string.Empty;

                    try
                    {
                        var details = await gitHubApiClient.GetPullRequestDetailsAsync(
                            owner,
                            repositoryName,
                            pullRequest.Number,
                            cancellationToken);

                        mergeable = details?.Mergeable;
                        mergeableState = details?.MergeableState ?? string.Empty;
                    }
                    catch (GitHubApiException exception)
                    {
                        partialDataDetected = true;
                        logs.Add($"[github] Impossible de lire les détails de mergeabilité pour {repository}#{pullRequest.Number} : {exception.Message}");
                    }

                    var evaluation = pullRequestDecisionService.Evaluate(
                        repository,
                        pullRequest,
                        qualification.ChecksStatus,
                        mergeable,
                        mergeableState);

                    evaluation = await pullRequestAutoMergeService.ExecuteAsync(
                        owner,
                        repositoryName,
                        evaluation,
                        cancellationToken);

                    mergeEvaluations.Add(evaluation);
                    logs.Add($"[automerge] {repository}#{pullRequest.Number} : décision {evaluation.Decision}, action {evaluation.ActionStatus}.");

                    switch (qualification.Bucket)
                    {
                        case PullRequestBucket.ReadyForReview:
                            if (evaluation.ActionStatus != PullRequestMergeActionStatus.Merged)
                            {
                                readyForReview.Add(qualification.Display);
                            }
                            break;
                        case PullRequestBucket.FailedChecks:
                            failedChecks.Add(qualification.Display);
                            failedPullRequests.Add(qualification.Display);
                            break;
                        default:
                            blocked.Add(qualification.Display);
                            break;
                    }

                    switch (evaluation.Decision)
                    {
                        case MergeDecision.AutoMerge when evaluation.ActionStatus == PullRequestMergeActionStatus.Merged:
                            autoMergedPullRequests.Add(BuildDecisionDisplay(evaluation));
                            mergedPullRequests.Add($"{display} (auto-merge exécuté par le worker)");
                            break;
                        case MergeDecision.AutoMerge:
                            readyForMerge.Add(BuildDecisionDisplay(evaluation));
                            break;
                        case MergeDecision.ManualReview:
                            manualReviewPullRequests.Add(BuildDecisionDisplay(evaluation));
                            break;
                        case MergeDecision.Blocked:
                            blockedPullRequests.Add(BuildDecisionDisplay(evaluation));
                            break;
                        case MergeDecision.Failed:
                            autoMergeFailures++;
                            failedAutoMergePullRequests.Add(BuildDecisionDisplay(evaluation));
                            failedPullRequests.Add($"{display} (échec du merge automatique)");
                            break;
                    }
                }

                foreach (var pullRequest in mergedRenovatePullRequests)
                {
                    var entry = $"{FormatPullRequest(repository, pullRequest)} (fusionnée récemment)";
                    mergedPullRequests.Add(entry);
                    mergedRecently.Add(entry);
                }

                foreach (var pullRequest in closedRenovatePullRequests)
                {
                    closedWithoutMerge.Add($"{FormatPullRequest(repository, pullRequest)} (fermée sans fusion)");
                }

                scannedRepositories.Add(repository);
                logs.Add(
                    $"[github] {repository} scanné : {readyForReview.Count(status => status.StartsWith($"{repository}#", StringComparison.Ordinal))} prête(s), {blocked.Count(status => status.StartsWith($"{repository}#", StringComparison.Ordinal))} bloquée(s), {failedChecks.Count(status => status.StartsWith($"{repository}#", StringComparison.Ordinal))} en échec, {mergedRenovatePullRequests.Count} fusionnée(s) récemment, {closedRenovatePullRequests.Count} fermée(s) sans fusion.");
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

        var pullRequestStatuses = new PullRequestStatuses
        {
            ReadyForReview = readyForReview,
            Blocked = blocked,
            FailedChecks = failedChecks,
            MergedRecently = mergedRecently,
            ClosedWithoutMerge = closedWithoutMerge
        };

        var autoMerge = new AutoMergeSummary
        {
            Enabled = autoMergeOptions.Value.Enabled,
            DryRunEnabled = autoMergeOptions.Value.DryRunEnabled,
            MergeMethod = autoMergeOptions.Value.MergeMethod,
            ReadyForMerge = readyForMerge,
            ManualReviewPullRequests = manualReviewPullRequests,
            BlockedPullRequests = blockedPullRequests,
            FailedPullRequests = failedAutoMergePullRequests,
            AutoMergedPullRequests = autoMergedPullRequests,
            Evaluations = mergeEvaluations
        };

        notes.Add("La collecte des vulnérabilités reste non branchée dans cette étape et n'alimente pas encore les compteurs.");
        notes.Add("Le worker qualifie désormais les PR Renovate ouvertes selon les checks GitHub et les statuts combinés disponibles.");
        notes.Add("Les PR fermées sans fusion sont corrélées uniquement sur la fenêtre récente configurée.");
        notes.Add(
            $"La politique d'auto-merge analyse les PR Renovate selon les checks GitHub, la mergeabilité et le type de version. Types autorisés : {string.Join(", ", autoMergeOptions.Value.AllowedUpdateTypes)}. Dry-run : {(autoMergeOptions.Value.DryRunEnabled ? "actif" : "désactivé")}.");

        if (readyForReview.Count > 0)
        {
            manualActions.Add("Examiner et traiter les PR Renovate prêtes avec checks verts.");
        }

        if (blocked.Count > 0)
        {
            manualActions.Add("Surveiller les PR bloquées ou en attente avant décision.");
        }

        if (failedChecks.Count > 0)
        {
            manualActions.Add("Analyser les checks en échec avant toute fusion.");
        }

        if (closedWithoutMerge.Count > 0)
        {
            manualActions.Add("Vérifier si les PR fermées sans fusion l'ont été volontairement.");
        }

        if (readyForMerge.Count > 0)
        {
            manualActions.Add("Confirmer les PR candidates à l'auto-merge ou activer l'exécution réelle si la politique vous convient.");
        }

        if (manualReviewPullRequests.Count > 0)
        {
            manualActions.Add("Revoir manuellement les mises à jour majeures, mineures non autorisées ou non qualifiées.");
        }

        if (failedAutoMergePullRequests.Count > 0)
        {
            manualActions.Add("Analyser les échecs d'auto-merge GitHub avant une nouvelle tentative.");
        }

        manualActions.Add("Compléter plus tard la collecte des vulnérabilités avec une source GitHub dédiée.");

        if (repositoryFailures > 0)
        {
            manualActions.Add("Vérifier les permissions du jeton GitHub et l'accessibilité des dépôts en échec.");
        }

        var status = ResolveStatus(scannedRepositories.Count, repositoryFailures, autoMergeFailures, partialDataDetected);

        if (status == "Success")
        {
            notes.Add("Toutes les interrogations GitHub prévues dans ce lot se sont terminées correctement.");
        }
        else if (status == "Partial")
        {
            notes.Add("La collecte GitHub a produit un résultat partiel : au moins un dépôt ou une qualification de checks n'a pas pu être obtenue complètement.");
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
            pullRequestStatuses,
            autoMerge,
            logs,
            notes,
            manualActions);
    }

    private async Task<PullRequestQualification> QualifyOpenPullRequestAsync(
        string repository,
        string owner,
        string repositoryName,
        GitHubPullRequestDto pullRequest,
        CancellationToken cancellationToken)
    {
        var logs = new List<string>();
        var combinedState = string.Empty;
        var checkRuns = Array.Empty<GitHubCheckRunDto>();
        var partialDataDetected = false;

        if (pullRequest.Draft)
        {
            return new PullRequestQualification(
                PullRequestBucket.Blocked,
                $"{FormatPullRequest(repository, pullRequest)} ({DraftReason})",
                PullRequestChecksStatus.Pending,
                false,
                logs);
        }

        if (string.IsNullOrWhiteSpace(pullRequest.Head.Sha))
        {
            partialDataDetected = true;
            logs.Add($"[github] SHA absent pour la PR {repository}#{pullRequest.Number}, qualification des checks impossible.");
            return new PullRequestQualification(
                PullRequestBucket.Blocked,
                $"{FormatPullRequest(repository, pullRequest)} ({UnknownChecksReason})",
                PullRequestChecksStatus.Unknown,
                partialDataDetected,
                logs);
        }

        try
        {
            checkRuns = (await gitHubApiClient.GetCheckRunsAsync(
                owner,
                repositoryName,
                pullRequest.Head.Sha,
                cancellationToken)).ToArray();
        }
        catch (GitHubApiException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            partialDataDetected = true;
            logs.Add($"[github] Check-runs indisponibles pour {repository}#{pullRequest.Number} : {exception.Message}");
        }
        catch (GitHubApiException exception)
        {
            partialDataDetected = true;
            logs.Add($"[github] Impossible de lire les check-runs pour {repository}#{pullRequest.Number} : {exception.Message}");
        }

        try
        {
            combinedState = await gitHubApiClient.GetCombinedStatusStateAsync(
                owner,
                repositoryName,
                pullRequest.Head.Sha,
                cancellationToken);
        }
        catch (GitHubApiException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            partialDataDetected = true;
            logs.Add($"[github] Statut combiné indisponible pour {repository}#{pullRequest.Number} : {exception.Message}");
        }
        catch (GitHubApiException exception)
        {
            partialDataDetected = true;
            logs.Add($"[github] Impossible de lire le statut combiné pour {repository}#{pullRequest.Number} : {exception.Message}");
        }

        var hasFailedChecks = checkRuns.Any(CheckRunHasFailed)
            || string.Equals(combinedState, "failure", StringComparison.OrdinalIgnoreCase)
            || string.Equals(combinedState, "error", StringComparison.OrdinalIgnoreCase);

        if (hasFailedChecks)
        {
            return new PullRequestQualification(
                PullRequestBucket.FailedChecks,
                $"{FormatPullRequest(repository, pullRequest)} ({FailedChecksReason})",
                PullRequestChecksStatus.Failed,
                partialDataDetected,
                logs);
        }

        var hasPendingChecks = checkRuns.Any(CheckRunIsPending)
            || string.Equals(combinedState, "pending", StringComparison.OrdinalIgnoreCase);

        if (hasPendingChecks)
        {
            return new PullRequestQualification(
                PullRequestBucket.Blocked,
                $"{FormatPullRequest(repository, pullRequest)} ({PendingChecksReason})",
                PullRequestChecksStatus.Pending,
                partialDataDetected,
                logs);
        }

        var hasSuccessfulChecks = checkRuns.Any(CheckRunHasSucceeded)
            || string.Equals(combinedState, "success", StringComparison.OrdinalIgnoreCase);

        if (hasSuccessfulChecks)
        {
            return new PullRequestQualification(
                PullRequestBucket.ReadyForReview,
                $"{FormatPullRequest(repository, pullRequest)} ({ReadyForReviewReason})",
                PullRequestChecksStatus.Success,
                partialDataDetected,
                logs);
        }

        partialDataDetected = true;
        logs.Add($"[github] Qualification incomplète pour {repository}#{pullRequest.Number} : aucun statut décisif n'a été trouvé.");

        return new PullRequestQualification(
            PullRequestBucket.Blocked,
            $"{FormatPullRequest(repository, pullRequest)} ({UnknownChecksReason})",
            PullRequestChecksStatus.Unknown,
            partialDataDetected,
            logs);
    }

    private static GitHubCollectionResult BuildResult(
        string status,
        IReadOnlyList<string> scannedRepositories,
        IReadOnlyList<string> createdPullRequests,
        IReadOnlyList<string> mergedPullRequests,
        IReadOnlyList<string> failedPullRequests,
        IReadOnlyList<string> remainingVulnerabilities,
        PullRequestStatuses pullRequestStatuses,
        AutoMergeSummary autoMerge,
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
            PullRequestStatuses = pullRequestStatuses,
            AutoMerge = autoMerge,
            Logs = logs,
            Notes = notes,
            ManualActions = manualActions.Distinct(StringComparer.Ordinal).ToArray()
        };
    }

    private static string ResolveStatus(
        int scannedRepositoryCount,
        int repositoryFailures,
        int autoMergeFailures,
        bool partialDataDetected)
    {
        if (scannedRepositoryCount == 0)
        {
            return "Failed";
        }

        if (repositoryFailures > 0 || autoMergeFailures > 0 || partialDataDetected)
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

    private static bool CheckRunHasFailed(GitHubCheckRunDto checkRun)
    {
        return FailedCheckConclusions.Contains(checkRun.Conclusion);
    }

    private static bool CheckRunIsPending(GitHubCheckRunDto checkRun)
    {
        return string.Equals(checkRun.Status, "queued", StringComparison.OrdinalIgnoreCase)
            || string.Equals(checkRun.Status, "in_progress", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(checkRun.Conclusion);
    }

    private static bool CheckRunHasSucceeded(GitHubCheckRunDto checkRun)
    {
        return string.Equals(checkRun.Status, "completed", StringComparison.OrdinalIgnoreCase)
            && SuccessfulCheckConclusions.Contains(checkRun.Conclusion);
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

    private static string BuildDecisionDisplay(PullRequestMergeEvaluation evaluation)
    {
        var location = string.IsNullOrWhiteSpace(evaluation.HtmlUrl)
            ? $"{evaluation.Repository}#{evaluation.Number}"
            : $"{evaluation.Repository}#{evaluation.Number} - {evaluation.HtmlUrl}";

        return $"{location} ({FormatVersionType(evaluation.VersionType)}, checks {FormatChecksStatus(evaluation.ChecksStatus)}) - {evaluation.Summary}";
    }

    private static string FormatVersionType(PullRequestVersionType versionType)
    {
        return versionType switch
        {
            PullRequestVersionType.Patch => "patch",
            PullRequestVersionType.Minor => "minor",
            PullRequestVersionType.Major => "major",
            _ => "inconnu"
        };
    }

    private static string FormatChecksStatus(PullRequestChecksStatus checksStatus)
    {
        return checksStatus switch
        {
            PullRequestChecksStatus.Success => "verts",
            PullRequestChecksStatus.Pending => "en attente",
            PullRequestChecksStatus.Failed => "en échec",
            _ => "inconnus"
        };
    }

    private sealed record PullRequestQualification(
        PullRequestBucket Bucket,
        string Display,
        PullRequestChecksStatus ChecksStatus,
        bool IsPartial,
        IReadOnlyList<string> Logs);

    private enum PullRequestBucket
    {
        ReadyForReview,
        Blocked,
        FailedChecks
    }
}
