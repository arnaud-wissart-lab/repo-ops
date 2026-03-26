import type {
  CodexExecutionResult,
  MaintenanceRunReport,
  SupervisorDecisionResult,
} from "../types";
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

function detectScenario(report: MaintenanceRunReport, decisions: SupervisorDecisionResult): string {
  const failedChecks = report.pullRequestStatuses.failedChecks.length;
  const criticalVulnerabilities = report.vulnerabilities.criticalCount;
  const securityReview = decisions.actions.find((action) => action.isSecurityRelated);
  const readyForMerge = report.autoMerge.readyForMerge.length;

  if (criticalVulnerabilities > 0 && securityReview) {
    return "Mise à jour de dépendance avec vulnérabilité critique à traiter en priorité";
  }

  if (failedChecks > 0) {
    return "Mise à jour de dépendance avec build cassé et correction ciblée attendue";
  }

  if (readyForMerge > 0) {
    return "Patch de dépendance prêt pour validation finale avant auto-merge contrôlé";
  }

  return "Cycle de maintenance standard avec tri des PR et préparation des actions";
}

export function NarrativeSummary({
  report,
  decisions,
  codex,
}: NarrativeSummaryProps) {
  const analyzed = report.observability?.metrics.analyzedPullRequests ?? 0;
  const failed = report.summary.counts.failedPullRequests;
  const fixes = decisions.summary.fixRequiredActions;
  const validations = codex.summary.requiresHumanValidationResponses;
  const vulnerabilities = report.vulnerabilities.openAlerts;
  const action = report.recommendations.manualActions[0] ?? "Relire les décisions prioritaires avant tout passage en mode réel.";

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
          <h3>{report.digest.subject}</h3>
          <ul className="narrative-list">
            <li>{analyzed} Pull Request{analyzed > 1 ? "s" : ""} analysée{analyzed > 1 ? "s" : ""}</li>
            <li>{failed} échec{failed > 1 ? "s" : ""} détecté{failed > 1 ? "s" : ""}</li>
            <li>{fixes} correction{fixes > 1 ? "s" : ""} proposée{fixes > 1 ? "s" : ""} par IA</li>
            <li>{validations} validation{validations > 1 ? "s" : ""} humaine{validations > 1 ? "s" : ""} encore requise{validations > 1 ? "s" : ""}</li>
            <li>{vulnerabilities} vulnérabilité{vulnerabilities > 1 ? "s" : ""} détectée{vulnerabilities > 1 ? "s" : ""}</li>
          </ul>
        </article>

        <article className="narrative-card">
          <p className="narrative-eyebrow">Contexte du run</p>
          <h3>{detectScenario(report, decisions)}</h3>
          <p className="subtle-text">
            Le scénario affiché ci-dessus aide à comprendre rapidement pourquoi
            les décisions ont été prises, sans devoir lire tous les détails.
          </p>
        </article>

        <article className="narrative-card">
          <p className="narrative-eyebrow">Action recommandée</p>
          <h3>{action}</h3>
          <p className="subtle-text">
            Cette recommandation correspond au prochain geste utile pour un
            responsable technique ou un recruteur en lecture guidée.
          </p>
        </article>
      </div>
    </section>
  );
}
