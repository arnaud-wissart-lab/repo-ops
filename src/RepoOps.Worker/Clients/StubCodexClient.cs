using System.Text;
using RepoOps.Worker.Models;

namespace RepoOps.Worker.Clients;

public sealed class StubCodexClient(ILogger<StubCodexClient> logger) : ICodexClient
{
    public string Mode => "Stub";

    public Task<CodexClientResponse> ExecuteAsync(
        GeneratedPrompt prompt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        cancellationToken.ThrowIfCancellationRequested();

        var responseType = ResolveResponseType(prompt);
        var response = new CodexClientResponse
        {
            ResponseType = responseType,
            ConfidenceLevel = ResolveConfidenceLevel(responseType),
            Summary = BuildSummary(prompt, responseType),
            ResponseText = BuildResponseText(prompt, responseType),
            ProposedUnifiedDiff = string.Empty,
            RequiresHumanValidation = true,
            ReadyForExecution = false
        };

        logger.LogInformation(
            "Client simulé utilisé pour {Repository}#{PullRequestNumber} ({PromptType})",
            prompt.Repository,
            prompt.PullRequestNumber,
            prompt.PromptType);

        return Task.FromResult(response);
    }

    private static CodexResponseType ResolveResponseType(GeneratedPrompt prompt)
    {
        if (string.Equals(prompt.PromptType, "fix-required", StringComparison.OrdinalIgnoreCase)
            || string.Equals(prompt.PromptType, "vulnerability-priority", StringComparison.OrdinalIgnoreCase))
        {
            return CodexResponseType.ProposedFix;
        }

        if (prompt.PromptText.Contains("refactor", StringComparison.OrdinalIgnoreCase))
        {
            return CodexResponseType.Refactor;
        }

        return CodexResponseType.Analysis;
    }

    private static CodexConfidenceLevel ResolveConfidenceLevel(CodexResponseType responseType)
    {
        return responseType switch
        {
            CodexResponseType.ProposedFix => CodexConfidenceLevel.Medium,
            CodexResponseType.Refactor => CodexConfidenceLevel.Low,
            _ => CodexConfidenceLevel.Medium
        };
    }

    private static string BuildSummary(GeneratedPrompt prompt, CodexResponseType responseType)
    {
        return responseType switch
        {
            CodexResponseType.ProposedFix => "Réponse simulée orientée correction, à relire avant toute modification manuelle.",
            CodexResponseType.Refactor => "Réponse simulée orientée refactorisation, à valider avant toute action.",
            _ => prompt.ActionType switch
            {
                SupervisorActionType.AutoMergeEligible => "Réponse simulée orientée validation finale avant une décision humaine.",
                SupervisorActionType.Review => "Réponse simulée orientée analyse et revue manuelle.",
                _ => "Réponse simulée orientée analyse manuelle."
            }
        };
    }

    private static string BuildResponseText(GeneratedPrompt prompt, CodexResponseType responseType)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Réponse simulée");
        builder.AppendLine("- Aucun appel réel à Codex n'a été effectué dans ce mode.");
        builder.AppendLine($"- Type de réponse proposé : {FormatResponseType(responseType)}");
        builder.AppendLine($"- Confiance estimée : {FormatConfidenceLevel(ResolveConfidenceLevel(responseType))}");
        builder.AppendLine();
        builder.AppendLine("Résumé");
        builder.AppendLine($"- {BuildSummary(prompt, responseType)}");
        builder.AppendLine($"- Recommandation initiale : {prompt.Context.Recommendation}");
        builder.AppendLine($"- État des checks : {prompt.Context.ChecksStatus}");

        if (prompt.Context.IsSecurityRelated)
        {
            builder.AppendLine($"- Priorité sécurité : {prompt.Context.SecuritySeverity}");
        }

        builder.AppendLine();
        builder.AppendLine("Action proposée");
        builder.AppendLine(ResolveActionProposal(prompt, responseType));
        builder.AppendLine();
        builder.AppendLine("Validation humaine requise");
        builder.AppendLine("- Vérifier manuellement la PR, le dépôt cible et l'impact réel avant toute exécution.");

        return builder.ToString().TrimEnd();
    }

    private static string ResolveActionProposal(GeneratedPrompt prompt, CodexResponseType responseType)
    {
        return responseType switch
        {
            CodexResponseType.ProposedFix =>
                "- Reprendre le prompt de correction, confirmer le diagnostic, puis préparer un correctif minimal sans l'appliquer automatiquement.",
            CodexResponseType.Refactor =>
                "- Reprendre le prompt de refactorisation, comparer les options et sélectionner une approche explicitement validée par un humain.",
            _ => prompt.ActionType switch
            {
                SupervisorActionType.AutoMergeEligible =>
                    "- Effectuer une dernière revue manuelle des checks, du diff et des protections de branche avant de décider d'une fusion.",
                SupervisorActionType.Review =>
                    "- Produire une synthèse de revue, identifier les risques et décider manuellement de la suite à donner.",
                _ =>
                    "- Examiner le contexte, confirmer qu'aucune action automatique n'est déclenchée et préparer la prochaine étape manuelle."
            }
        };
    }

    private static string FormatResponseType(CodexResponseType responseType) => responseType switch
    {
        CodexResponseType.ProposedFix => "correction proposée",
        CodexResponseType.Refactor => "refactorisation",
        _ => "analyse"
    };

    private static string FormatConfidenceLevel(CodexConfidenceLevel confidenceLevel) => confidenceLevel switch
    {
        CodexConfidenceLevel.High => "élevée",
        CodexConfidenceLevel.Medium => "moyenne",
        _ => "faible"
    };
}
