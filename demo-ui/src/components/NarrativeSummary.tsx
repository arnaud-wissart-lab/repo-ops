import type {
  CodexExecutionResult,
  MaintenanceRunReport,
  SupervisorDecisionResult,
} from "../types";
import { detectScenarioLabel } from "../utils";
import { StatusPill } from "./StatusPill";

interface NarrativeSummaryProps {
  report: MaintenanceRunReport;
  decisions: SupervisorDecisionResult;
  codex: CodexExecutionResult;
}

function statusTone(status: string): "done" | "warning" | "failed" {
  const normalized = status.toLowerCase();

  if (normalized === "success") {
    return "done";
  }

  if (normalized === "partial") {
    return "warning";
  }

  return "failed";
}

export function NarrativeSummary({
  report,
  decisions,
  codex,
}: NarrativeSummaryProps) {
  const analyzed = report.observability?.metrics.analyzedPullRequests ?? 0;
  const anomalies =
    report.observability?.metrics.errorCount ?? report.summary.counts.failedPullRequests;
  const fixes =
    codex.summary.proposedFixResponses > 0
      ? codex.summary.proposedFixResponses
      : decisions.summary.fixRequiredActions;
  const vulnerabilities = report.vulnerabilities.openAlerts;
  const action = report.recommendations.manualActions[0] ?? "Relire les décisions prioritaires avant tout passage en mode réel.";
  const securityReview = decisions.actions.some((decision) => decision.isSecurityRelated);
  const scenario = detectScenarioLabel(
    report.pullRequestStatuses.failedChecks.length,
    report.vulnerabilities.criticalCount,
    securityReview,
    report.autoMerge.readyForMerge.length,
  );

  return (
    <section className="panel narrative-panel panel-reveal">
      <div className="panel-header">
        <div>
          <p className="section-kicker">Ce que le système a fait</p>
          <h2>Lecture immédiate du run</h2>
        </div>
        <StatusPill label={report.summary.status} tone={statusTone(report.summary.status)} />
      </div>

      <div className="narrative-grid">
        <article className="narrative-card narrative-card-primary">
          <p className="narrative-eyebrow">Analyse terminée</p>
          <h3>Ce que le système a fait</h3>
          <ul className="narrative-list">
            <li>{analyzed} Pull Request{analyzed > 1 ? "s" : ""} analysée{analyzed > 1 ? "s" : ""}</li>
            <li>{anomalies} anomalie{anomalies > 1 ? "s" : ""} détectée{anomalies > 1 ? "s" : ""}</li>
            <li>{fixes} correction{fixes > 1 ? "s" : ""} proposée{fixes > 1 ? "s" : ""} par IA</li>
            <li>{vulnerabilities} vulnérabilité{vulnerabilities > 1 ? "s" : ""} détectée{vulnerabilities > 1 ? "s" : ""}</li>
          </ul>
          <div className="narrative-action-callout">
            <span>Action recommandée</span>
            <strong>{action}</strong>
          </div>
        </article>

        <article className="narrative-card">
          <p className="narrative-eyebrow">Scénario</p>
          <h3>{scenario}</h3>
          <ul className="detail-list compact-list narrative-detail-list">
            <li>{report.summary.counts.failedPullRequests} PR en échec à traiter</li>
            <li>{report.autoMerge.readyForMerge.length} PR prête{report.autoMerge.readyForMerge.length > 1 ? "s" : ""} pour validation finale</li>
            <li>{decisions.summary.highPriorityActions} action{decisions.summary.highPriorityActions > 1 ? "s" : ""} prioritaire{decisions.summary.highPriorityActions > 1 ? "s" : ""}</li>
          </ul>
        </article>

        <article className="narrative-card">
          <p className="narrative-eyebrow">Lecture rapide</p>
          <h3>{report.digest.subject}</h3>
          <ul className="detail-list compact-list narrative-detail-list">
            <li>{decisions.summary.totalActions} décision{decisions.summary.totalActions > 1 ? "s" : ""} structurée{decisions.summary.totalActions > 1 ? "s" : ""}</li>
            <li>{codex.summary.totalResponses} réponse{codex.summary.totalResponses > 1 ? "s" : ""} générée{codex.summary.totalResponses > 1 ? "s" : ""}</li>
            <li>{codex.summary.requiresHumanValidationResponses} validation{codex.summary.requiresHumanValidationResponses > 1 ? "s" : ""} humaine{codex.summary.requiresHumanValidationResponses > 1 ? "s" : ""} requise{codex.summary.requiresHumanValidationResponses > 1 ? "s" : ""}</li>
          </ul>
        </article>
      </div>
    </section>
  );
}
