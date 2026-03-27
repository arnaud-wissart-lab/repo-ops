import type {
  CodexExecutionResult,
  MaintenanceRunReport,
  SupervisorDecisionResult,
} from "../types";
import { ClipboardList, Clock3, ShieldCheck, Sparkles } from "lucide-react";
import {
  detectScenarioLabel,
  formatDateTime,
  formatDuration,
  formatRelativeTime,
  resolvePrimaryRepository,
} from "../utils";
import { StatusPill } from "./StatusPill";
import { Badge } from "./ui/badge";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardHeading,
  CardTitle,
} from "./ui/card";

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
  const primaryRepository = resolvePrimaryRepository(report.summary.scannedRepositories);

  return (
    <Card className="section-enter">
      <CardHeader>
        <CardHeading>
          <div className="mb-2 flex flex-wrap items-center gap-2">
            <Badge variant="neutral">Synthèse</Badge>
            <StatusPill label={report.summary.status} tone={statusTone(report.summary.status)} />
          </div>
          <CardTitle>Résumé exécutif du run</CardTitle>
          <CardDescription>
            Vue d’ensemble compacte du scénario, des messages utiles et des réponses proposées.
          </CardDescription>
        </CardHeading>
      </CardHeader>
      <CardContent className="space-y-5">
        <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
          <div className="surface-subtle p-4">
            <div className="mb-2 flex items-center gap-2 text-muted-foreground">
              <ClipboardList className="size-4" />
              <span className="text-xs font-semibold uppercase tracking-[0.16em]">Scénario</span>
            </div>
            <strong className="block text-sm font-semibold text-foreground">{scenario}</strong>
            <span className="text-xs text-muted-foreground">{primaryRepository}</span>
          </div>
          <div className="surface-subtle p-4">
            <div className="mb-2 flex items-center gap-2 text-muted-foreground">
              <Clock3 className="size-4" />
              <span className="text-xs font-semibold uppercase tracking-[0.16em]">Dernière exécution</span>
            </div>
            <strong className="block text-sm font-semibold text-foreground">{formatRelativeTime(report.summary.runDateUtc)}</strong>
            <span className="text-xs text-muted-foreground">{formatDateTime(report.summary.runDateUtc)}</span>
          </div>
          <div className="surface-subtle p-4">
            <div className="mb-2 flex items-center gap-2 text-muted-foreground">
              <Clock3 className="size-4" />
              <span className="text-xs font-semibold uppercase tracking-[0.16em]">Durée</span>
            </div>
            <strong className="block text-sm font-semibold text-foreground">{formatDuration(report.observability?.durationMilliseconds)}</strong>
            <span className="text-xs text-muted-foreground">Mode démonstration / dry-run</span>
          </div>
          <div className="surface-subtle p-4">
            <div className="mb-2 flex items-center gap-2 text-muted-foreground">
              <ShieldCheck className="size-4" />
              <span className="text-xs font-semibold uppercase tracking-[0.16em]">Source</span>
            </div>
            <strong className="block text-sm font-semibold text-foreground">{report.summary.inputSource}</strong>
            <span className="text-xs text-muted-foreground">Dépôt analysé : {primaryRepository}</span>
          </div>
        </div>

        <div className="grid gap-4 xl:grid-cols-2">
          <article className="surface-subtle p-5">
            <div className="mb-4 flex items-center gap-2">
              <Sparkles className="size-4 text-primary" />
              <h3 className="text-base font-semibold">Résultat métier immédiat</h3>
            </div>
            <p className="text-base font-semibold text-foreground">{report.digest.subject}</p>
            <p className="mt-1 text-sm text-muted-foreground">
              Run du {formatDateTime(report.summary.runDateUtc)} via <strong>{report.summary.inputSource}</strong> sur <strong>{primaryRepository}</strong>
            </p>
            <pre className="mt-4 whitespace-pre-wrap rounded-lg border border-border bg-white/60 p-4 text-sm leading-6 text-foreground dark:bg-black/10">
              {report.digest.plainTextBody}
            </pre>
          </article>

          <article className="surface-subtle p-5">
            <div className="mb-4 flex items-center gap-2">
              <ClipboardList className="size-4 text-primary" />
              <h3 className="text-base font-semibold">Messages importants</h3>
            </div>
            <ul className="space-y-3 text-sm leading-6 text-foreground">
              {report.messages.notes.map((note) => (
                <li key={note} className="rounded-md border border-border bg-white/60 px-4 py-3 dark:bg-black/10">
                  {note}
                </li>
              ))}
              {report.recommendations.manualActions.map((action) => (
                <li key={action} className="rounded-md border border-border bg-white/60 px-4 py-3 dark:bg-black/10">
                  {action}
                </li>
              ))}
            </ul>
          </article>
        </div>

        <div className="grid gap-4 xl:grid-cols-2">
          <article className="surface-subtle p-5">
            <h3 className="text-base font-semibold">Lecture rapide</h3>
            <div className="mt-4 grid gap-3 sm:grid-cols-2">
              <div className="rounded-md border border-border bg-white/60 px-4 py-3 dark:bg-black/10">
                <span className="block text-xs font-semibold uppercase tracking-[0.16em] text-muted-foreground">Sécurité</span>
                <strong className="text-base font-semibold text-foreground">{report.vulnerabilities.status}</strong>
              </div>
              <div className="rounded-md border border-border bg-white/60 px-4 py-3 dark:bg-black/10">
                <span className="block text-xs font-semibold uppercase tracking-[0.16em] text-muted-foreground">Renovate</span>
                <strong className="text-base font-semibold text-foreground">{report.renovateExecution?.status ?? "non disponible"}</strong>
              </div>
              <div className="rounded-md border border-border bg-white/60 px-4 py-3 dark:bg-black/10">
                <span className="block text-xs font-semibold uppercase tracking-[0.16em] text-muted-foreground">Décisions</span>
                <strong className="text-base font-semibold text-foreground">{decisions.summary.totalActions}</strong>
              </div>
              <div className="rounded-md border border-border bg-white/60 px-4 py-3 dark:bg-black/10">
                <span className="block text-xs font-semibold uppercase tracking-[0.16em] text-muted-foreground">Réponses Codex</span>
                <strong className="text-base font-semibold text-foreground">{codex.summary.totalResponses}</strong>
              </div>
            </div>
          </article>

          <article className="surface-subtle p-5">
            <h3 className="text-base font-semibold">Réponses proposées par le système</h3>
            <ul className="mt-4 space-y-3 text-sm leading-6 text-foreground">
              {codex.responses.length === 0 ? (
                <li className="rounded-md border border-dashed border-border px-4 py-3 text-muted-foreground">
                  Aucune réponse structurée disponible.
                </li>
              ) : (
                codex.responses.map((response) => (
                  <li key={response.actionId} className="rounded-md border border-border bg-white/60 px-4 py-3 dark:bg-black/10">
                    <strong>{response.repository}</strong>
                    {response.pullRequestNumber ? ` #${response.pullRequestNumber}` : ""} : {response.summary}
                  </li>
                ))
              )}
            </ul>
          </article>
        </div>
      </CardContent>
    </Card>
  );
}
