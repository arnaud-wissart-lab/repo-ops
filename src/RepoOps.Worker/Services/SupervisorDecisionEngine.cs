using RepoOps.Worker.Models;

namespace RepoOps.Worker.Services;

public sealed class SupervisorDecisionEngine(ILogger<SupervisorDecisionEngine> logger)
{
    public SupervisorDecisionResult Evaluate(MaintenanceRunReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var actions = new List<SupervisorAction>();

        foreach (var evaluation in report.AutoMerge.Evaluations)
        {
            actions.Add(BuildPullRequestAction(evaluation));
        }

        foreach (var repository in report.Vulnerabilities.Repositories.Where(item => item.CriticalCount > 0))
        {
            if (actions.Any(action =>
                    string.Equals(action.Repository, repository.Repository, StringComparison.OrdinalIgnoreCase)
                    && action.Priority == SupervisorActionPriority.High))
            {
                continue;
            }

            actions.Add(new SupervisorAction
            {
                Type = SupervisorActionType.FixRequired,
                Repository = repository.Repository,
                Priority = SupervisorActionPriority.High,
                Reason = $"Le dépôt présente {repository.CriticalCount} vulnérabilité(s) critique(s) ouverte(s) sans PR corrective prioritaire identifiée."
            });
        }

        var summary = new SupervisorDecisionSummary
        {
            TotalActions = actions.Count,
            ReviewActions = actions.Count(action => action.Type == SupervisorActionType.Review),
            AutoMergeEligibleActions = actions.Count(action => action.Type == SupervisorActionType.AutoMergeEligible),
            FixRequiredActions = actions.Count(action => action.Type == SupervisorActionType.FixRequired),
            IgnoreActions = actions.Count(action => action.Type == SupervisorActionType.Ignore),
            HighPriorityActions = actions.Count(action => action.Priority == SupervisorActionPriority.High)
        };

        var notes = BuildNotes(report, actions);

        logger.LogInformation(
            "Superviseur décisionnel exécuté : {TotalActions} action(s), {HighPriorityActions} priorité(s) haute(s)",
            summary.TotalActions,
            summary.HighPriorityActions);

        return new SupervisorDecisionResult
        {
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            SourceReportStatus = report.Summary.Status,
            Summary = summary,
            Actions = actions,
            Notes = notes
        };
    }

    private static SupervisorAction BuildPullRequestAction(PullRequestMergeEvaluation evaluation)
    {
        var reasons = new List<string>();
        var actionType = SupervisorActionType.Ignore;
        var priority = SupervisorActionPriority.Low;

        if (evaluation.ChecksStatus == PullRequestChecksStatus.Failed || evaluation.Decision == MergeDecision.Failed)
        {
            actionType = SupervisorActionType.FixRequired;
            priority = SupervisorActionPriority.High;
            reasons.Add("Les checks GitHub sont en échec ou la décision d'auto-merge a échoué.");
        }
        else if (evaluation.VersionType == PullRequestVersionType.Major)
        {
            actionType = SupervisorActionType.Review;
            priority = SupervisorActionPriority.High;
            reasons.Add("La mise à jour est majeure et requiert une revue prioritaire.");
        }
        else if (evaluation.VersionType == PullRequestVersionType.Minor)
        {
            actionType = SupervisorActionType.Review;
            priority = SupervisorActionPriority.Medium;
            reasons.Add("La mise à jour est mineure et doit être revue manuellement.");
        }
        else if (evaluation.VersionType == PullRequestVersionType.Patch
                 && evaluation.ChecksStatus == PullRequestChecksStatus.Success
                 && evaluation.Decision == MergeDecision.AutoMerge)
        {
            actionType = SupervisorActionType.AutoMergeEligible;
            priority = SupervisorActionPriority.Medium;
            reasons.Add("La mise à jour patch est prête, avec checks verts et décision d'auto-merge positive.");
        }
        else
        {
            reasons.Add(ResolveIgnoreReason(evaluation));
        }

        if (evaluation.IsSecurityUpdate)
        {
            if (string.Equals(evaluation.SecuritySeverity, "critical", StringComparison.OrdinalIgnoreCase))
            {
                priority = SupervisorActionPriority.High;
                reasons.Add("La PR est corrélée à une vulnérabilité critique et doit être priorisée.");
            }
            else if (!string.IsNullOrWhiteSpace(evaluation.SecuritySeverity))
            {
                reasons.Add($"La PR est liée à la sécurité avec une sévérité {evaluation.SecuritySeverity}.");
            }
        }

        return new SupervisorAction
        {
            Type = actionType,
            Repository = evaluation.Repository,
            PullRequestNumber = evaluation.Number,
            PullRequestTitle = evaluation.Title,
            PullRequestUrl = evaluation.HtmlUrl,
            Priority = priority,
            Reason = string.Join(" ", reasons.Where(reason => !string.IsNullOrWhiteSpace(reason)))
        };
    }

    private static string ResolveIgnoreReason(PullRequestMergeEvaluation evaluation)
    {
        if (evaluation.ChecksStatus == PullRequestChecksStatus.Pending)
        {
            return "Les checks sont encore en attente ; aucune action immédiate n'est retenue.";
        }

        if (evaluation.ChecksStatus == PullRequestChecksStatus.Unknown)
        {
            return "Les checks ne sont pas qualifiés de façon exploitable ; la PR reste en observation.";
        }

        if (evaluation.Decision == MergeDecision.Blocked)
        {
            return "La PR est bloquée et ne devient pas encore une action de supervision explicite.";
        }

        return "Aucune action immédiate n'est retenue pour cette PR dans cette première version.";
    }

    private static IReadOnlyList<string> BuildNotes(
        MaintenanceRunReport report,
        IReadOnlyList<SupervisorAction> actions)
    {
        var notes = new List<string>
        {
            "Le superviseur IA de première génération reste purement décisionnel : aucune action n'est exécutée automatiquement."
        };

        if (!string.Equals(report.Summary.Status, "Success", StringComparison.OrdinalIgnoreCase))
        {
            notes.Add($"Le rapport source est en statut {report.Summary.Status} ; les décisions doivent être relues avec ce contexte.");
        }

        if (actions.Count == 0)
        {
            notes.Add("Aucune action n'a été produite à partir du rapport source.");
        }

        return notes;
    }
}
