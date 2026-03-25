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
        var policy = ResolvePolicy(repository);
        var versionType = DetectVersionType(pullRequest);
        var reasons = BuildReasons(policy, versionType, checksStatus, mergeable, mergeableState, pullRequest.Draft);
        var decision = ResolveDecision(policy, versionType, checksStatus, mergeable, mergeableState, pullRequest.Draft);

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
            MergeMethod = policy.MergeMethod,
            PolicySource = policy.Source,
            Decision = decision,
            Reasons = reasons,
            Summary = string.Join(" ", reasons)
        };
    }

    private EffectiveAutoMergePolicy ResolvePolicy(string repository)
    {
        var settings = options.Value;
        var matchedPolicy = settings.RepositoryPolicies
            .FirstOrDefault(policy => string.Equals(
                policy.Repository,
                repository,
                StringComparison.OrdinalIgnoreCase));

        var allowedUpdateTypes = matchedPolicy?.AllowedUpdateTypes is { Length: > 0 }
            ? matchedPolicy.AllowedUpdateTypes
            : settings.AllowedUpdateTypes;

        var mergeMethod = string.IsNullOrWhiteSpace(matchedPolicy?.MergeMethod)
            ? settings.MergeMethod
            : matchedPolicy!.MergeMethod;

        return new EffectiveAutoMergePolicy(
            Source: matchedPolicy is null ? "global" : $"repository:{matchedPolicy.Repository}",
            AllowAutoMerge: matchedPolicy?.AllowAutoMerge ?? true,
            ReviewRequired: matchedPolicy?.ReviewRequired ?? false,
            ReadOnly: matchedPolicy?.ReadOnly ?? false,
            MergeMethod: mergeMethod,
            AllowedUpdateTypes: allowedUpdateTypes,
            AllowedMergeableStates: settings.AllowedMergeableStates);
    }

    private static MergeDecision ResolveDecision(
        EffectiveAutoMergePolicy policy,
        PullRequestVersionType versionType,
        PullRequestChecksStatus checksStatus,
        bool? mergeable,
        string mergeableState,
        bool draft)
    {
        if (draft
            || checksStatus is PullRequestChecksStatus.Failed or PullRequestChecksStatus.Pending or PullRequestChecksStatus.Unknown
            || mergeable is not true
            || !IsAllowedMergeableState(policy, mergeableState))
        {
            return MergeDecision.Blocked;
        }

        if (policy.ReadOnly || policy.ReviewRequired || !policy.AllowAutoMerge)
        {
            return MergeDecision.ManualReview;
        }

        if (versionType is PullRequestVersionType.Major or PullRequestVersionType.Unknown)
        {
            return MergeDecision.ManualReview;
        }

        return Allows(policy, versionType)
            ? MergeDecision.AutoMerge
            : MergeDecision.ManualReview;
    }

    private static IReadOnlyList<string> BuildReasons(
        EffectiveAutoMergePolicy policy,
        PullRequestVersionType versionType,
        PullRequestChecksStatus checksStatus,
        bool? mergeable,
        string mergeableState,
        bool draft)
    {
        var reasons = new List<string>
        {
            $"Politique appliquée : {policy.Source}.",
            $"Type de mise à jour détecté : {FormatVersionType(versionType)}.",
            $"État des checks : {FormatChecksStatus(checksStatus)}."
        };

        if (draft)
        {
            reasons.Add("La PR est en brouillon, donc exclue du merge automatique.");
            return reasons;
        }

        if (checksStatus == PullRequestChecksStatus.Failed)
        {
            reasons.Add("Les checks GitHub sont en échec.");
            return reasons;
        }

        if (checksStatus == PullRequestChecksStatus.Pending)
        {
            reasons.Add("Les checks GitHub sont encore en attente.");
            return reasons;
        }

        if (checksStatus == PullRequestChecksStatus.Unknown)
        {
            reasons.Add("Les checks GitHub ne sont pas suffisamment qualifiés pour prendre une décision sûre.");
            return reasons;
        }

        if (mergeable is not true)
        {
            reasons.Add("GitHub n'indique pas la PR comme mergeable.");
            return reasons;
        }

        if (!IsAllowedMergeableState(policy, mergeableState))
        {
            reasons.Add(
                $"L'état mergeable GitHub '{NormalizeMergeableState(mergeableState)}' n'est pas accepté par la politique.");
            return reasons;
        }

        if (policy.ReadOnly)
        {
            reasons.Add("Le dépôt est déclaré en lecture seule pour l'auto-merge.");
            return reasons;
        }

        if (policy.ReviewRequired)
        {
            reasons.Add("Une revue manuelle est exigée par la politique du dépôt.");
            return reasons;
        }

        if (!policy.AllowAutoMerge)
        {
            reasons.Add("L'auto-merge est désactivé pour ce dépôt.");
            return reasons;
        }

        if (versionType == PullRequestVersionType.Major)
        {
            reasons.Add("Les mises à jour majeures exigent une revue manuelle.");
            return reasons;
        }

        if (versionType == PullRequestVersionType.Unknown)
        {
            reasons.Add("Le type de version n'est pas détectable, une revue manuelle est requise.");
            return reasons;
        }

        if (!Allows(policy, versionType))
        {
            reasons.Add(
                $"Les mises à jour {FormatVersionType(versionType)} ne sont pas autorisées par la politique active.");
            return reasons;
        }

        reasons.Add(
            $"La PR est éligible à l'auto-merge avec la méthode {policy.MergeMethod}.");
        return reasons;
    }

    private static bool Allows(EffectiveAutoMergePolicy policy, PullRequestVersionType versionType)
    {
        return (policy.AllowedUpdateTypes ?? [])
            .Any(allowedType => string.Equals(
                allowedType,
                versionType.ToString(),
                StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsAllowedMergeableState(
        EffectiveAutoMergePolicy policy,
        string mergeableState)
    {
        var normalizedState = NormalizeMergeableState(mergeableState);

        return (policy.AllowedMergeableStates ?? [])
            .Any(allowedState => string.Equals(
                allowedState,
                normalizedState,
                StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeMergeableState(string mergeableState)
    {
        return string.IsNullOrWhiteSpace(mergeableState)
            ? "inconnu"
            : mergeableState.Trim();
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
            PullRequestChecksStatus.Success => "succès",
            PullRequestChecksStatus.Pending => "en attente",
            PullRequestChecksStatus.Failed => "en échec",
            _ => "inconnu"
        };
    }

    private sealed record EffectiveAutoMergePolicy(
        string Source,
        bool AllowAutoMerge,
        bool ReviewRequired,
        bool ReadOnly,
        string MergeMethod,
        IReadOnlyList<string> AllowedUpdateTypes,
        IReadOnlyList<string> AllowedMergeableStates);
}
