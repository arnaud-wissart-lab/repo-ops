import type {
  CodexExecutionResult,
  MaintenanceRunReport,
  SupervisorDecisionResult,
} from "../types";
import { ArrowRightCircle, Bot, GitPullRequest, ShieldAlert, Wrench } from "lucide-react";
import { detectScenarioLabel } from "../utils";
import { StatusPill } from "./StatusPill";
import { Badge } from "./ui/badge";
import {
  Card,
  CardContent,
  CardHeader,
  CardHeading,
  CardTitle,
} from "./ui/card";

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
    <Card className="section-enter">
      <CardHeader>
        <CardHeading>
          <div className="mb-2 flex flex-wrap items-center gap-2">
            <Badge variant="info">Ce que le système a fait</Badge>
            <StatusPill label={report.summary.status} tone={statusTone(report.summary.status)} />
          </div>
          <CardTitle>Lecture immédiate du run</CardTitle>
          <p className="text-sm leading-6 text-muted-foreground">
            Ce bloc raconte le run sans vous obliger à lire le pipeline complet ou la sortie JSON.
          </p>
        </CardHeading>
      </CardHeader>
      <CardContent className="grid gap-4 xl:grid-cols-[minmax(0,1.15fr)_minmax(320px,0.85fr)]">
        <div className="rounded-2xl border border-border/70 bg-card/80 p-5">
          <p className="text-sm font-semibold uppercase tracking-[0.18em] text-primary">Analyse terminée</p>
          <ul className="mt-4 space-y-3 text-sm text-foreground">
            <li className="flex items-center gap-3">
              <GitPullRequest className="size-4 text-primary" />
              <span>
                <strong>{analyzed}</strong> Pull Request{analyzed > 1 ? "s" : ""} analysée{analyzed > 1 ? "s" : ""}
              </span>
            </li>
            <li className="flex items-center gap-3">
              <Wrench className="size-4 text-amber-500" />
              <span>
                <strong>{anomalies}</strong> anomalie{anomalies > 1 ? "s" : ""} détectée{anomalies > 1 ? "s" : ""}
              </span>
            </li>
            <li className="flex items-center gap-3">
              <Bot className="size-4 text-violet-500" />
              <span>
                <strong>{fixes}</strong> correction{fixes > 1 ? "s" : ""} proposée{fixes > 1 ? "s" : ""} par IA
              </span>
            </li>
            <li className="flex items-center gap-3">
              <ShieldAlert className="size-4 text-rose-500" />
              <span>
                <strong>{vulnerabilities}</strong> vulnérabilité{vulnerabilities > 1 ? "s" : ""} identifiée{vulnerabilities > 1 ? "s" : ""}
              </span>
            </li>
          </ul>

          <div className="mt-5 rounded-2xl border border-primary/15 bg-accent/70 p-4">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-primary">Action recommandée</p>
            <div className="mt-2 flex items-start gap-3">
              <ArrowRightCircle className="mt-0.5 size-4 shrink-0 text-primary" />
              <strong className="text-base leading-6 text-foreground">{action}</strong>
            </div>
          </div>
        </div>

        <div className="grid gap-4">
          <div className="rounded-2xl border border-border/70 bg-card/80 p-5">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-muted-foreground">Scénario</p>
            <h3 className="mt-2 text-lg font-semibold tracking-tight">{scenario}</h3>
            <ul className="mt-4 space-y-2 text-sm leading-6 text-muted-foreground">
              <li>{report.summary.counts.failedPullRequests} PR en échec à traiter</li>
              <li>
                {report.autoMerge.readyForMerge.length} PR prête{report.autoMerge.readyForMerge.length > 1 ? "s" : ""} pour validation finale
              </li>
              <li>
                {decisions.summary.highPriorityActions} action{decisions.summary.highPriorityActions > 1 ? "s" : ""} prioritaire{decisions.summary.highPriorityActions > 1 ? "s" : ""}
              </li>
            </ul>
          </div>

          <div className="rounded-2xl border border-border/70 bg-card/80 p-5">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-muted-foreground">Trace métier</p>
            <h3 className="mt-2 text-lg font-semibold tracking-tight">{report.digest.subject}</h3>
            <ul className="mt-4 space-y-2 text-sm leading-6 text-muted-foreground">
              <li>{decisions.summary.totalActions} décision{decisions.summary.totalActions > 1 ? "s" : ""} structurée{decisions.summary.totalActions > 1 ? "s" : ""}</li>
              <li>{codex.summary.totalResponses} réponse{codex.summary.totalResponses > 1 ? "s" : ""} générée{codex.summary.totalResponses > 1 ? "s" : ""}</li>
              <li>{codex.summary.requiresHumanValidationResponses} validation{codex.summary.requiresHumanValidationResponses > 1 ? "s" : ""} humaine{codex.summary.requiresHumanValidationResponses > 1 ? "s" : ""} requise{codex.summary.requiresHumanValidationResponses > 1 ? "s" : ""}</li>
            </ul>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
