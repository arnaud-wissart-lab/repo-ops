using System.Text;
using RepoOps.Worker.Models;

namespace RepoOps.Worker.Services;

public sealed class MaintenanceDigestRenderer
{
    public MaintenanceDigest Render(MaintenanceRunReport report)
    {
        return new MaintenanceDigest
        {
            Subject = $"[repo-ops] Synthèse maintenance du {report.Summary.RunDateUtc:yyyy-MM-dd}",
            PlainTextBody = BuildPlainText(report),
            HtmlBody = BuildHtml(report)
        };
    }

    private static string BuildPlainText(MaintenanceRunReport report)
    {
        var builder = new StringBuilder();

        builder.AppendLine("Synthèse repo-ops");
        builder.AppendLine($"Date d'exécution : {report.Summary.RunDateUtc:O}");
        builder.AppendLine($"Statut : {report.Summary.Status}");
        builder.AppendLine(
            $"Dépôts scannés : {(report.Summary.ScannedRepositories.Count > 0 ? string.Join(", ", report.Summary.ScannedRepositories) : "aucun dépôt scanné")}");
        builder.AppendLine();
        builder.AppendLine("Exécution explicite de Renovate :");
        builder.AppendLine($"- Statut : {report.RenovateExecution.Status}");
        builder.AppendLine($"- Déclenchée dans ce cycle : {(report.RenovateExecution.TriggerRequested ? "oui" : "non")}");
        builder.AppendLine($"- Dernier résultat connu réutilisé : {(report.RenovateExecution.IncludedFromLatestKnownExecution ? "oui" : "non")}");
        builder.AppendLine($"- Mode : {report.RenovateExecution.Mode}");
        builder.AppendLine($"- Commande : {(string.IsNullOrWhiteSpace(report.RenovateExecution.Command) ? "non renseignée" : report.RenovateExecution.Command)}");
        builder.AppendLine($"- Début : {FormatTimestamp(report.RenovateExecution.StartedAtUtc)}");
        builder.AppendLine($"- Fin : {FormatTimestamp(report.RenovateExecution.FinishedAtUtc)}");
        builder.AppendLine($"- Durée : {FormatDuration(report.RenovateExecution.DurationSeconds)}");
        builder.AppendLine($"- Code de sortie : {FormatExitCode(report.RenovateExecution.ExitCode)}");
        builder.AppendLine($"- Résumé : {report.RenovateExecution.Summary}");
        builder.AppendLine();
        builder.AppendLine("Compteurs :");
        builder.AppendLine($"- PR créées : {report.Summary.Counts.CreatedPullRequests}");
        builder.AppendLine($"- PR fusionnées : {report.Summary.Counts.MergedPullRequests}");
        builder.AppendLine($"- PR en échec : {report.Summary.Counts.FailedPullRequests}");
        builder.AppendLine($"- Vulnérabilités restantes : {report.Summary.Counts.RemainingVulnerabilities}");
        builder.AppendLine();
        AppendSection(
            builder,
            "PR prêtes à traiter",
            report.PullRequestStatuses.ReadyForReview,
            "Aucune PR prête à traiter.");
        builder.AppendLine();
        AppendSection(
            builder,
            "PR bloquées ou en attente",
            report.PullRequestStatuses.Blocked,
            "Aucune PR bloquée ou en attente.");
        builder.AppendLine();
        AppendSection(
            builder,
            "PR en échec détectées",
            report.PullRequestStatuses.FailedChecks,
            "Aucune PR en échec détectée.");
        builder.AppendLine();
        AppendSection(
            builder,
            "PR fusionnées récemment",
            report.PullRequestStatuses.MergedRecently,
            "Aucune PR fusionnée récemment.");
        builder.AppendLine();
        AppendSection(
            builder,
            "PR fermées sans fusion",
            report.PullRequestStatuses.ClosedWithoutMerge,
            "Aucune PR fermée sans fusion détectée récemment.");
        builder.AppendLine();
        builder.AppendLine("Actions manuelles recommandées :");

        AppendBullets(builder, report.Recommendations.ManualActions, "Aucune action manuelle supplémentaire.");

        builder.AppendLine();
        builder.AppendLine("Notes :");

        AppendBullets(builder, report.Messages.Notes, "Aucune note complémentaire.");

        builder.AppendLine();
        builder.AppendLine("Logs Renovate utiles :");

        AppendBullets(
            builder,
            SelectInterestingRenovateLines(report.RenovateExecution.Logs, report.RenovateExecution.Errors),
            "Aucun log Renovate capturé dans ce cycle.");

        return builder.ToString().TrimEnd();
    }

    private static string BuildHtml(MaintenanceRunReport report)
    {
        static string Escape(string value) => value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&#39;", StringComparison.Ordinal);

        static string RenderList(IEnumerable<string> items, string fallback)
        {
            var values = items.ToList();

            if (values.Count == 0)
            {
                return $"<p>{Escape(fallback)}</p>";
            }

            return $"<ul>{string.Join(string.Empty, values.Select(item => $"<li>{Escape(item)}</li>"))}</ul>";
        }

        return $"""
                <html lang="fr">
                  <body style="font-family: Arial, sans-serif; color: #172033; line-height: 1.5;">
                    <h1>Synthèse repo-ops</h1>
                    <p><strong>Date d'exécution :</strong> {Escape(report.Summary.RunDateUtc.ToString("O"))}</p>
                    <p><strong>Statut :</strong> {Escape(report.Summary.Status)}</p>
                    <p><strong>Dépôts scannés :</strong> {Escape(report.Summary.ScannedRepositories.Count > 0 ? string.Join(", ", report.Summary.ScannedRepositories) : "aucun dépôt scanné")}</p>
                    <h2>Exécution explicite de Renovate</h2>
                    <ul>
                      <li>Statut : {Escape(report.RenovateExecution.Status)}</li>
                      <li>Déclenchée dans ce cycle : {Escape(report.RenovateExecution.TriggerRequested ? "oui" : "non")}</li>
                      <li>Dernier résultat connu réutilisé : {Escape(report.RenovateExecution.IncludedFromLatestKnownExecution ? "oui" : "non")}</li>
                      <li>Mode : {Escape(report.RenovateExecution.Mode)}</li>
                      <li>Commande : {Escape(string.IsNullOrWhiteSpace(report.RenovateExecution.Command) ? "non renseignée" : report.RenovateExecution.Command)}</li>
                      <li>Début : {Escape(FormatTimestamp(report.RenovateExecution.StartedAtUtc))}</li>
                      <li>Fin : {Escape(FormatTimestamp(report.RenovateExecution.FinishedAtUtc))}</li>
                      <li>Durée : {Escape(FormatDuration(report.RenovateExecution.DurationSeconds))}</li>
                      <li>Code de sortie : {Escape(FormatExitCode(report.RenovateExecution.ExitCode))}</li>
                    </ul>
                    <p><strong>Résumé :</strong> {Escape(report.RenovateExecution.Summary)}</p>
                    <h2>Compteurs</h2>
                    <ul>
                      <li>PR créées : {report.Summary.Counts.CreatedPullRequests}</li>
                      <li>PR fusionnées : {report.Summary.Counts.MergedPullRequests}</li>
                      <li>PR en échec : {report.Summary.Counts.FailedPullRequests}</li>
                      <li>Vulnérabilités restantes : {report.Summary.Counts.RemainingVulnerabilities}</li>
                    </ul>
                    <h2>PR prêtes à traiter</h2>
                    {RenderList(report.PullRequestStatuses.ReadyForReview, "Aucune PR prête à traiter.")}
                    <h2>PR bloquées ou en attente</h2>
                    {RenderList(report.PullRequestStatuses.Blocked, "Aucune PR bloquée ou en attente.")}
                    <h2>PR en échec détectées</h2>
                    {RenderList(report.PullRequestStatuses.FailedChecks, "Aucune PR en échec détectée.")}
                    <h2>PR fusionnées récemment</h2>
                    {RenderList(report.PullRequestStatuses.MergedRecently, "Aucune PR fusionnée récemment.")}
                    <h2>PR fermées sans fusion</h2>
                    {RenderList(report.PullRequestStatuses.ClosedWithoutMerge, "Aucune PR fermée sans fusion détectée récemment.")}
                    <h2>Actions manuelles recommandées</h2>
                    {RenderList(report.Recommendations.ManualActions, "Aucune action manuelle supplémentaire.")}
                    <h2>Notes</h2>
                    {RenderList(report.Messages.Notes, "Aucune note complémentaire.")}
                    <h2>Logs Renovate utiles</h2>
                    {RenderList(SelectInterestingRenovateLines(report.RenovateExecution.Logs, report.RenovateExecution.Errors), "Aucun log Renovate capturé dans ce cycle.")}
                  </body>
                </html>
                """;
    }

    private static string FormatDuration(double? durationSeconds)
    {
        if (durationSeconds is null)
        {
            return "non disponible";
        }

        return $"{durationSeconds.Value:0.##} s";
    }

    private static string FormatExitCode(int? exitCode)
    {
        return exitCode?.ToString() ?? "non disponible";
    }

    private static string FormatTimestamp(DateTimeOffset? value)
    {
        return value?.ToString("O") ?? "non disponible";
    }

    private static IReadOnlyList<string> SelectInterestingRenovateLines(
        IReadOnlyList<string> logs,
        IReadOnlyList<string> errors)
    {
        return logs
            .Concat(errors.Select(error => $"stderr: {error}"))
            .Take(8)
            .ToArray();
    }

    private static void AppendSection(
        StringBuilder builder,
        string title,
        IReadOnlyList<string> items,
        string fallback)
    {
        builder.AppendLine($"{title} :");
        AppendBullets(builder, items, fallback);
    }

    private static void AppendBullets(
        StringBuilder builder,
        IReadOnlyList<string> items,
        string fallback)
    {
        if (items.Count == 0)
        {
            builder.AppendLine($"- {fallback}");
            return;
        }

        foreach (var item in items)
        {
            builder.AppendLine($"- {item}");
        }
    }
}
