using System.Text;
using RepoOps.Worker.Models;

namespace RepoOps.Worker.Services;

public sealed class PromptGeneratorService(ILogger<PromptGeneratorService> logger)
{
    public GeneratedPromptResult Generate(SupervisorDecisionResult decisions)
    {
        ArgumentNullException.ThrowIfNull(decisions);

        var prompts = decisions.Actions
            .Select(BuildPrompt)
            .ToArray();

        var summary = new GeneratedPromptSummary
        {
            TotalPrompts = prompts.Length,
            HighPriorityPrompts = prompts.Count(prompt => prompt.Priority == SupervisorActionPriority.High),
            ReviewPrompts = prompts.Count(prompt => prompt.ActionType == SupervisorActionType.Review),
            FixPrompts = prompts.Count(prompt => prompt.ActionType == SupervisorActionType.FixRequired),
            ValidationPrompts = prompts.Count(prompt => prompt.ActionType == SupervisorActionType.AutoMergeEligible)
        };

        logger.LogInformation(
            "Générateur de prompts exécuté : {TotalPrompts} prompt(s), {HighPriorityPrompts} priorité(s) haute(s)",
            summary.TotalPrompts,
            summary.HighPriorityPrompts);

        return new GeneratedPromptResult
        {
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            SourceDecisionGeneratedAtUtc = decisions.GeneratedAtUtc,
            SourceReportStatus = decisions.SourceReportStatus,
            Summary = summary,
            Prompts = prompts,
            Notes = BuildNotes(decisions, prompts)
        };
    }

    private static GeneratedPrompt BuildPrompt(SupervisorAction action)
    {
        var promptType = ResolvePromptType(action);
        var promptText = BuildPromptText(action, promptType);

        return new GeneratedPrompt
        {
            ActionType = action.Type,
            Repository = action.Repository,
            PullRequestNumber = action.PullRequestNumber,
            PullRequestTitle = action.PullRequestTitle,
            PullRequestUrl = action.PullRequestUrl,
            Priority = action.Priority,
            PromptType = promptType,
            PromptText = promptText,
            Context = new PromptContext
            {
                ProblemSummary = action.Reason,
                ChecksStatus = FormatChecksStatus(action.ChecksStatus),
                Recommendation = action.Recommendation,
                IsSecurityRelated = action.IsSecurityRelated,
                SecuritySeverity = action.SecuritySeverity
            }
        };
    }

    private static string ResolvePromptType(SupervisorAction action)
    {
        if (action.Type == SupervisorActionType.FixRequired && action.IsSecurityRelated)
        {
            return "vulnerability-priority";
        }

        return action.Type switch
        {
            SupervisorActionType.FixRequired => "fix-required",
            SupervisorActionType.Review => "review",
            SupervisorActionType.AutoMergeEligible => "auto-merge-eligible",
            _ => "ignore"
        };
    }

    private static string BuildPromptText(SupervisorAction action, string promptType)
    {
        var builder = new StringBuilder();
        var target = action.PullRequestNumber is null
            ? action.Repository
            : $"{action.Repository}#{action.PullRequestNumber}";
        var securityLine = action.IsSecurityRelated
            ? $"- Contexte sécurité : oui ({(string.IsNullOrWhiteSpace(action.SecuritySeverity) ? "sévérité non précisée" : action.SecuritySeverity)})"
            : "- Contexte sécurité : non";

        builder.AppendLine("Contexte");
        builder.AppendLine($"- Dépôt cible : {action.Repository}");
        builder.AppendLine($"- Cible précise : {target}");

        if (!string.IsNullOrWhiteSpace(action.PullRequestTitle))
        {
            builder.AppendLine($"- Titre de PR : {action.PullRequestTitle}");
        }

        if (!string.IsNullOrWhiteSpace(action.PullRequestUrl))
        {
            builder.AppendLine($"- URL de PR : {action.PullRequestUrl}");
        }

        builder.AppendLine($"- Résumé du problème : {action.Reason}");
        builder.AppendLine($"- État des checks : {FormatChecksStatus(action.ChecksStatus)}");
        builder.AppendLine($"- Priorité : {action.Priority}");
        builder.AppendLine(securityLine);
        builder.AppendLine();
        builder.AppendLine("Objectif");
        builder.AppendLine(ResolveObjective(action, promptType));
        builder.AppendLine();
        builder.AppendLine("Contraintes");
        builder.AppendLine("- Rester strictement dans le périmètre de la PR ou du dépôt ciblé.");
        builder.AppendLine("- Ne pas exécuter de déploiement ni d'action destructive.");
        builder.AppendLine("- Produire une réponse exploitable, argumentée et concise.");
        builder.AppendLine($"- Recommandation repo-ops : {action.Recommendation}");
        builder.AppendLine();
        builder.AppendLine("Sortie attendue");
        builder.AppendLine(ResolveExpectedOutput(promptType));

        return builder.ToString().TrimEnd();
    }

    private static string ResolveObjective(SupervisorAction action, string promptType)
    {
        return promptType switch
        {
            "fix-required" =>
                "Analyser la cause du blocage ou de l'échec, proposer un correctif minimal et préciser les validations à relancer.",
            "vulnerability-priority" =>
                "Traiter en priorité le sujet de sécurité, confirmer l'impact réel et proposer la correction ou la mitigation la plus sûre.",
            "review" =>
                "Analyser la PR et donner une recommandation claire sur l'impact, le risque et la suite à donner.",
            "auto-merge-eligible" =>
                "Faire une validation finale de la PR et confirmer si elle peut être fusionnée en sécurité ou si un point de vigilance impose une revue supplémentaire.",
            _ =>
                $"Passer en revue la situation pour {action.Repository} et confirmer qu'aucune action immédiate n'est nécessaire."
        };
    }

    private static string ResolveExpectedOutput(string promptType)
    {
        return promptType switch
        {
            "fix-required" or "vulnerability-priority" =>
                "- diagnostic court\n- correctif proposé\n- validations à exécuter\n- risques résiduels",
            "review" =>
                "- synthèse de la PR\n- risques identifiés\n- recommandation finale\n- validations utiles",
            "auto-merge-eligible" =>
                "- verdict final\n- points de contrôle restants\n- recommandation de fusion ou de revue",
            _ =>
                "- constat\n- justification\n- prochaine action éventuelle"
        };
    }

    private static string FormatChecksStatus(PullRequestChecksStatus checksStatus)
    {
        return checksStatus switch
        {
            PullRequestChecksStatus.Success => "succès",
            PullRequestChecksStatus.Pending => "en attente",
            PullRequestChecksStatus.Failed => "en échec",
            _ => "non qualifié"
        };
    }

    private static IReadOnlyList<string> BuildNotes(
        SupervisorDecisionResult decisions,
        IReadOnlyList<GeneratedPrompt> prompts)
    {
        var notes = new List<string>
        {
            "Les prompts générés sont destinés à être relus puis utilisés manuellement ; aucune exécution n'est automatisée."
        };

        if (prompts.Count == 0)
        {
            notes.Add("Aucun prompt n'a été produit car aucune action superviseur exploitable n'était disponible.");
        }

        if (!string.Equals(decisions.SourceReportStatus, "Success", StringComparison.OrdinalIgnoreCase))
        {
            notes.Add($"Le rapport source est en statut {decisions.SourceReportStatus} ; les prompts doivent être interprétés avec prudence.");
        }

        return notes;
    }
}
