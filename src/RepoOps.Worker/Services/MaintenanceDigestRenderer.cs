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
        builder.AppendLine("Compteurs :");
        builder.AppendLine($"- PR créées : {report.Summary.Counts.CreatedPullRequests}");
        builder.AppendLine($"- PR fusionnées : {report.Summary.Counts.MergedPullRequests}");
        builder.AppendLine($"- PR en échec : {report.Summary.Counts.FailedPullRequests}");
        builder.AppendLine($"- Vulnérabilités restantes : {report.Summary.Counts.RemainingVulnerabilities}");
        builder.AppendLine();
        AppendSection(
            builder,
            "PR ouvertes Renovate détectées",
            report.Summary.CreatedPullRequests,
            "Aucune PR ouverte Renovate détectée.");
        builder.AppendLine();
        AppendSection(
            builder,
            "PR fusionnées récemment",
            report.Summary.MergedPullRequests,
            "Aucune PR fusionnée récemment.");
        builder.AppendLine();
        AppendSection(
            builder,
            "PR en échec détectées",
            report.Summary.FailedPullRequests,
            "Aucune PR en échec détectée.");
        builder.AppendLine();
        builder.AppendLine("Actions manuelles recommandées :");

        AppendBullets(builder, report.Recommendations.ManualActions, "Aucune action manuelle supplémentaire.");

        builder.AppendLine();
        builder.AppendLine("Notes :");

        AppendBullets(builder, report.Messages.Notes, "Aucune note complémentaire.");

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
                    <h2>Compteurs</h2>
                    <ul>
                      <li>PR créées : {report.Summary.Counts.CreatedPullRequests}</li>
                      <li>PR fusionnées : {report.Summary.Counts.MergedPullRequests}</li>
                      <li>PR en échec : {report.Summary.Counts.FailedPullRequests}</li>
                      <li>Vulnérabilités restantes : {report.Summary.Counts.RemainingVulnerabilities}</li>
                    </ul>
                    <h2>PR ouvertes Renovate détectées</h2>
                    {RenderList(report.Summary.CreatedPullRequests, "Aucune PR ouverte Renovate détectée.")}
                    <h2>PR fusionnées récemment</h2>
                    {RenderList(report.Summary.MergedPullRequests, "Aucune PR fusionnée récemment.")}
                    <h2>PR en échec détectées</h2>
                    {RenderList(report.Summary.FailedPullRequests, "Aucune PR en échec détectée.")}
                    <h2>Actions manuelles recommandées</h2>
                    {RenderList(report.Recommendations.ManualActions, "Aucune action manuelle supplémentaire.")}
                    <h2>Notes</h2>
                    {RenderList(report.Messages.Notes, "Aucune note complémentaire.")}
                  </body>
                </html>
                """;
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
