import type { MaintenanceRunReport } from "../types";
import { formatDateTime, formatDuration, formatRelativeTime } from "../utils";
import { StatusPill } from "./StatusPill";

interface GlobalStatusBannerProps {
  report: MaintenanceRunReport;
}

function toneFromStatus(status: string): "done" | "warning" | "failed" {
  const normalized = status.toLowerCase();

  if (normalized === "success") {
    return "done";
  }

  if (normalized === "partial") {
    return "warning";
  }

  return "failed";
}

export function GlobalStatusBanner({ report }: GlobalStatusBannerProps) {
  const durationLabel = formatDuration(report.observability?.durationMilliseconds);
  const normalizedStatus = report.summary.status.toLowerCase();
  const headline =
    normalizedStatus === "success"
      ? "✔ Analyse terminée avec succès"
      : normalizedStatus === "partial"
        ? "⚠ Analyse terminée avec un état partiel"
        : "✖ Analyse terminée avec erreur";
  const bannerTone =
    normalizedStatus === "success"
      ? "global-status-banner-done"
      : normalizedStatus === "partial"
        ? "global-status-banner-warning"
        : "global-status-banner-failed";

  return (
    <section className={`global-status-banner ${bannerTone}`}>
      <div className="global-status-main">
        <div>
          <p className="section-kicker">Statut global</p>
          <h2>{headline}</h2>
          <p className="global-status-subtitle">
            Le run est consolidé, horodaté et prêt à être relu sans ouvrir les
            logs.
          </p>
        </div>
        <StatusPill label={report.summary.status} tone={toneFromStatus(report.summary.status)} />
      </div>

      <div className="global-status-meta">
        <div className="global-status-item global-status-item-highlight">
          <span>Dernière exécution</span>
          <strong>{formatRelativeTime(report.summary.runDateUtc)}</strong>
        </div>
        <div className="global-status-item">
          <span>Durée</span>
          <strong>{durationLabel}</strong>
        </div>
        <div className="global-status-item">
          <span>Mode</span>
          <strong>Dry-run</strong>
        </div>
        <div className="global-status-item">
          <span>Horodatage</span>
          <strong>{formatDateTime(report.summary.runDateUtc)}</strong>
        </div>
        <div className="global-status-item">
          <span>Source</span>
          <strong>{report.summary.inputSource}</strong>
        </div>
        <div className="global-status-item">
          <span>Run ID</span>
          <strong>{report.observability?.runId ?? "non disponible"}</strong>
        </div>
      </div>
    </section>
  );
}
