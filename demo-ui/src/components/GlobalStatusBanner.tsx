import type { MaintenanceRunReport } from "../types";
import { AlertCircle, CheckCircle2, Clock3, ShieldAlert, TriangleAlert } from "lucide-react";
import { formatDateTime, formatDuration, formatRelativeTime, resolvePrimaryRepository } from "../utils";
import { StatusPill } from "./StatusPill";
import { Badge } from "./ui/badge";
import { Card, CardContent } from "./ui/card";

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
  const primaryRepository = resolvePrimaryRepository(report.summary.scannedRepositories);
  const normalizedStatus = report.summary.status.toLowerCase();
  const headline =
    normalizedStatus === "success"
      ? "✔ Analyse terminée avec succès"
      : normalizedStatus === "partial"
        ? "⚠ Analyse terminée avec un état partiel"
        : "✖ Analyse terminée avec erreur";
  const bannerTone =
    normalizedStatus === "success"
      ? "border-emerald-200 bg-emerald-50/80 status-glow-success dark:border-emerald-900/70 dark:bg-emerald-950/20"
      : normalizedStatus === "partial"
        ? "border-amber-200 bg-amber-50/80 status-glow-warning dark:border-amber-900/70 dark:bg-amber-950/20"
        : "border-rose-200 bg-rose-50/80 status-glow-danger dark:border-rose-900/70 dark:bg-rose-950/20";
  const Icon =
    normalizedStatus === "success"
      ? CheckCircle2
      : normalizedStatus === "partial"
        ? TriangleAlert
        : AlertCircle;

  return (
    <Card className={`section-enter overflow-hidden border ${bannerTone}`}>
      <CardContent className="px-5 py-5 lg:px-6">
        <div className="grid gap-5 xl:grid-cols-[minmax(0,1.2fr)_minmax(360px,0.8fr)]">
          <div className="flex items-start gap-4">
            <div className="rounded-lg border border-white/70 bg-white/85 p-3 shadow-sm dark:border-white/10 dark:bg-black/10">
              <Icon className="size-6" />
            </div>

            <div className="space-y-3">
              <div className="flex flex-wrap items-center gap-2">
                <Badge variant="neutral">Statut global</Badge>
                <StatusPill label={report.summary.status} tone={toneFromStatus(report.summary.status)} />
              </div>

              <div>
                <h2 className="text-2xl font-semibold text-foreground">{headline}</h2>
                <p className="mt-2 max-w-2xl text-sm leading-6 text-muted-foreground">
                  Le pipeline est maintenant consolidé pour <strong>{primaryRepository}</strong>. Vous pouvez relire le diagnostic, les décisions et la sortie technique sans changer d’écran.
                </p>
              </div>
            </div>
          </div>

          <div className="grid gap-3 sm:grid-cols-2">
            <div className="toolbar-metric">
              <div className="mb-2 flex items-center gap-2 text-muted-foreground">
                <Clock3 className="size-4" />
                <span className="text-xs font-semibold uppercase tracking-[0.16em]">Dernière exécution</span>
              </div>
              <strong className="block text-base font-semibold">{formatRelativeTime(report.summary.runDateUtc)}</strong>
              <span className="text-sm text-muted-foreground">{formatDateTime(report.summary.runDateUtc)}</span>
            </div>

            <div className="toolbar-metric">
              <div className="mb-2 flex items-center gap-2 text-muted-foreground">
                <Clock3 className="size-4" />
                <span className="text-xs font-semibold uppercase tracking-[0.16em]">Durée</span>
              </div>
              <strong className="block text-base font-semibold">{durationLabel}</strong>
              <span className="text-sm text-muted-foreground">Dry-run contrôlé</span>
            </div>

            <div className="toolbar-metric">
              <div className="mb-2 flex items-center gap-2 text-muted-foreground">
                <ShieldAlert className="size-4" />
                <span className="text-xs font-semibold uppercase tracking-[0.16em]">Source</span>
              </div>
              <strong className="block text-base font-semibold">{report.summary.inputSource}</strong>
              <span className="text-sm text-muted-foreground">Run ID {report.observability?.runId ?? "indisponible"}</span>
            </div>

            <div className="toolbar-metric">
              <div className="mb-2 flex items-center gap-2 text-muted-foreground">
                <ShieldAlert className="size-4" />
                <span className="text-xs font-semibold uppercase tracking-[0.16em]">Mode</span>
              </div>
              <strong className="block text-base font-semibold">Démonstration / dry-run</strong>
              <span className="text-sm text-muted-foreground">Exécution sensible désactivée</span>
            </div>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
