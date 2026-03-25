using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using RepoOps.Worker.Models;
using RepoOps.Worker.Options;

namespace RepoOps.Worker.Services;

public sealed class PullRequestDecisionService(IOptions<AutoMergeOptions> options)
{
    private static readonly Regex SemanticVersionRegex = new(
        @"(?<!\d)v?(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public PullRequestMergeEvaluation Evaluate(
        string repository,
        GitHubPullRequestDto pullRequest,
        PullRequestChecksStatus checksStatus,
        bool? mergeable,
        string mergeableState)
    {
        var versionType = DetectVersionType(pullRequest);
        var decision = ResolveDecision(versionType, checksStatus, mergeable, mergeableState, pullRequest.Draft);

        return new PullRequestMergeEvaluation
        {
            Repository = repository,
            Number = pullRequest.Number,
            Title = pullRequest.Title,
            HtmlUrl = pullRequest.HtmlUrl,
            VersionType = versionType,
            ChecksStatus = checksStatus,
            Mergeable = mergeable,
            MergeableState = mergeableState,
            Decision = decision,
            Summary = BuildSummary(decision, versionType, checksStatus, mergeable, mergeableState, pullRequest.Draft)
        };
    }

    private MergeDecision ResolveDecision(
        PullRequestVersionType versionType,
        PullRequestChecksStatus checksStatus,
        bool? mergeable,
        string mergeableState,
        bool draft)
    {
        if (draft)
        {
            return MergeDecision.Blocked;
        }

        if (checksStatus == PullRequestChecksStatus.Failed
            || checksStatus == PullRequestChecksStatus.Pending
            || checksStatus == PullRequestChecksStatus.Unknown)
        {
            return MergeDecision.Blocked;
        }

        if (mergeable is not true)
        {
            return MergeDecision.Blocked;
        }

        if (!string.Equals(mergeableState, "clean", StringComparison.OrdinalIgnoreCase))
        {
            return MergeDecision.Blocked;
        }

        if (versionType is PullRequestVersionType.Major or PullRequestVersionType.Unknown)
        {
            return MergeDecision.ManualReview;
        }

        return Allows(versionType)
            ? MergeDecision.AutoMerge
            : MergeDecision.ManualReview;
    }

    private bool Allows(PullRequestVersionType versionType)
    {
        return options.Value.AllowedUpdateTypes
            .Any(allowedType => string.Equals(
                allowedType,
                versionType.ToString(),
                StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildSummary(
        MergeDecision decision,
        PullRequestVersionType versionType,
        PullRequestChecksStatus checksStatus,
        bool? mergeable,
        string mergeableState,
        bool draft)
    {
        if (draft)
        {
            return "PR en brouillon, exclue de l'auto-merge.";
        }

        if (checksStatus == PullRequestChecksStatus.Failed)
        {
            return "Checks GitHub en échec, auto-merge bloqué.";
        }

        if (checksStatus == PullRequestChecksStatus.Pending)
        {
            return "Checks GitHub encore en attente, auto-merge bloqué.";
        }

        if (checksStatus == PullRequestChecksStatus.Unknown)
        {
            return "Les checks GitHub ne sont pas assez qualifiés pour décider un merge automatique.";
        }

        if (mergeable is not true)
        {
            return "GitHub n'indique pas la PR comme mergeable, auto-merge bloqué.";
        }

        if (!string.Equals(mergeableState, "clean", StringComparison.OrdinalIgnoreCase))
        {
            var state = string.IsNullOrWhiteSpace(mergeableState) ? "inconnu" : mergeableState;
            return $"L'état mergeable GitHub est {state}, auto-merge bloqué.";
        }

        return decision switch
        {
            MergeDecision.AutoMerge => $"Mise à jour {versionType.ToString().ToLowerInvariant()} compatible avec la politique d'auto-merge.",
            MergeDecision.ManualReview when versionType == PullRequestVersionType.Major => "Mise à jour majeure détectée, revue manuelle requise.",
            MergeDecision.ManualReview when versionType == PullRequestVersionType.Unknown => "Type de version non détectable, revue manuelle requise.",
            MergeDecision.ManualReview => $"Mise à jour {versionType.ToString().ToLowerInvariant()} hors politique d'auto-merge, revue manuelle requise.",
            _ => "Décision d'auto-merge indisponible."
        };
    }

    private static PullRequestVersionType DetectVersionType(GitHubPullRequestDto pullRequest)
    {
        foreach (var label in pullRequest.Labels)
        {
            if (string.Equals(label.Name, "major", StringComparison.OrdinalIgnoreCase))
            {
                return PullRequestVersionType.Major;
            }

            if (string.Equals(label.Name, "minor", StringComparison.OrdinalIgnoreCase))
            {
                return PullRequestVersionType.Minor;
            }

            if (string.Equals(label.Name, "patch", StringComparison.OrdinalIgnoreCase))
            {
                return PullRequestVersionType.Patch;
            }
        }

        var matches = SemanticVersionRegex.Matches(pullRequest.Title);
        if (matches.Count < 2)
        {
            return PullRequestVersionType.Unknown;
        }

        if (!TryExtractVersion(matches[0], out var previousVersion)
            || !TryExtractVersion(matches[^1], out var nextVersion))
        {
            return PullRequestVersionType.Unknown;
        }

        if (nextVersion.major > previousVersion.major)
        {
            return PullRequestVersionType.Major;
        }

        if (nextVersion.minor > previousVersion.minor)
        {
            return PullRequestVersionType.Minor;
        }

        if (nextVersion.patch > previousVersion.patch)
        {
            return PullRequestVersionType.Patch;
        }

        return PullRequestVersionType.Unknown;
    }

    private static bool TryExtractVersion(Match match, out (int major, int minor, int patch) version)
    {
        version = default;

        return int.TryParse(match.Groups["major"].Value, out version.major)
            && int.TryParse(match.Groups["minor"].Value, out version.minor)
            && int.TryParse(match.Groups["patch"].Value, out version.patch);
    }
}
