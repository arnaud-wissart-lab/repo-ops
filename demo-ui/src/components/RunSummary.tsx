import type {
  CodexExecutionResult,
  MaintenanceRunReport,
  SupervisorDecisionResult,
} from "../types";
import { detectScenarioLabel, formatDateTime, formatDuration, formatRelativeTime } from "../utils";
import { StatusPill } from "./StatusPill";

interface RunSummaryProps {
  report: MaintenanceRunReport;
  decisions: SupervisorDecisionResult;
  codex: CodexExecutionResult;
}

function statusTone(status: string): "done" | "warning" | "failed" {
  const normalized = status.toLowerCase();

  if (normalized === "success" || normalized === "succeeded" || normalized === "available") {
    return "done";
  }

  if (normalized === "partial") {
    return "warning";
  }

  return "failed";
}

export function RunSummary({ report, decisions, codex }: RunSummaryProps) {
  const scenario = detectScenarioLabel(
    report.pullRequestStatuses.failedChecks.length,
    report.vulnerabilities.criticalCount,
    decisions.actions.some((action) => action.isSecurityRelated),
    report.autoMerge.readyForMerge.length,
  );

  return (
    <section className="panel panel-reveal">
      <div className="panel-header">
        <div>
          <p className="section-kicker">Synthèse</p>
          <h2>Résumé exécutif du run</h2>
        </div>
        <StatusPill label={report.summary.status} tone={statusTone(report.summary.status)} />
      </div>

      <div className="run-context-banner">
        <div>
          <span>Scénario</span>
          <strong>{scenario}</strong>
        </div>
        <div>
          <span>Dernière exécution</span>
          <strong>{formatRelativeTime(report.summary.runDateUtc)}</strong>
        </div>
        <div>
          <span>Durée</span>
          <strong>{formatDuration(report.observability?.durationMilliseconds)}</strong>
        </div>
        <div>
          <span>Mode</span>
          <strong>Démonstration / dry-run</strong>
        </div>
      </div>

      <div className="summary-grid">
        <article className="summary-card">
          <h3>Résultat métier immédiat</h3>
          <p className="summary-lead">{report.digest.subject}</p>
          <p className="subtle-text">
            Run du {formatDateTime(report.summary.runDateUtc)} via{" "}
            <strong>{report.summary.inputSource}</strong>
          </p>
          <pre>{report.digest.plainTextBody}</pre>
        </article>

        <article className="summary-card">
          <h3>Messages importants</h3>
          <ul className="detail-list">
            {report.messages.notes.map((note) => (
              <li key={note}>{note}</li>
            ))}
            {report.recommendations.manualActions.map((action) => (
              <li key={action}>{action}</li>
            ))}
          </ul>
        </article>

        <article className="summary-card">
          <h3>Lecture rapide</h3>
          <div className="summary-stats">
            <div>
              <span>Sécurité</span>
              <strong>{report.vulnerabilities.status}</strong>
            </div>
            <div>
              <span>Renovate</span>
              <strong>{report.renovateExecution?.status ?? "non disponible"}</strong>
            </div>
            <div>
              <span>Décisions</span>
              <strong>{decisions.summary.totalActions}</strong>
            </div>
            <div>
              <span>Réponses Codex</span>
              <strong>{codex.summary.totalResponses}</strong>
            </div>
          </div>
        </article>

        <article className="summary-card">
          <h3>Réponses proposées par le système</h3>
          <ul className="detail-list">
            {codex.responses.length === 0 ? (
              <li>Aucune réponse structurée disponible.</li>
            ) : (
              codex.responses.map((response) => (
                <li key={response.actionId}>
                  <strong>{response.repository}</strong>
                  {response.pullRequestNumber ? ` #${response.pullRequestNumber}` : ""} :
                  {" "}{response.summary}
                </li>
              ))
            )}
          </ul>
        </article>
      </div>
    </section>
  );
}
