import type { MaintenanceRunReport } from "../types";
import { formatDateTime } from "../utils";
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
  const duration = report.observability?.durationMilliseconds;
  const durationLabel =
    typeof duration === "number" ? `${(duration / 1000).toFixed(2)} s` : "non disponible";

  return (
    <section className="global-status-banner">
      <div className="global-status-main">
        <div>
          <p className="section-kicker">Statut global</p>
          <h2>Run consolidé prêt à être relu</h2>
        </div>
        <StatusPill label={report.summary.status} tone={toneFromStatus(report.summary.status)} />
      </div>

      <div className="global-status-meta">
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
      </div>
    </section>
  );
}
