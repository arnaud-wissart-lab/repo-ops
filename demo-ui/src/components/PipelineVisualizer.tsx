import type { PipelineStep } from "../types";
import { CheckCircle2, CircleDashed, LoaderCircle, TriangleAlert, XCircle } from "lucide-react";
import { Badge } from "./ui/badge";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardHeading,
  CardTitle,
} from "./ui/card";

interface PipelineVisualizerProps {
  steps: PipelineStep[];
}

export function PipelineVisualizer({ steps }: PipelineVisualizerProps) {
  const stateLabels: Record<PipelineStep["state"], string> = {
    idle: "En attente",
    running: "En cours",
    done: "Terminé",
    warning: "Vigilance",
    failed: "Erreur",
  };

  const allDone = steps.every((step) => step.state === "done");
  const hasFailure = steps.some((step) => step.state === "failed");
  const hasWarning = steps.some((step) => step.state === "warning");

  const completionLabel = hasFailure
    ? "Analyse terminée avec erreur"
    : hasWarning
      ? "Analyse terminée avec vigilance"
      : allDone
        ? "Analyse terminée avec succès"
        : "Pipeline prêt à s’exécuter";
  const completionNote = hasFailure
    ? "Un point bloquant a été détecté et documenté dans le run."
    : hasWarning
      ? "Le run est terminé, avec des décisions à relire avant toute suite."
      : allDone
        ? "Le pipeline s’est déroulé complètement et la synthèse est prête."
        : "Le système attend un déclenchement pour dérouler le run complet.";

  return (
    <Card className="section-enter h-full">
      <CardHeader>
        <CardHeading>
          <div className="mb-2 flex flex-wrap items-center gap-2">
            <Badge variant="neutral">Pipeline</Badge>
            <Badge variant={hasFailure ? "danger" : hasWarning ? "warning" : allDone ? "success" : "info"}>
              {completionLabel}
            </Badge>
          </div>
          <CardTitle>Timeline du run</CardTitle>
          <CardDescription>
            Chaque étape reflète l’état courant du run, avec une lecture séquentielle claire et sans automatisation dangereuse.
          </CardDescription>
        </CardHeading>
      </CardHeader>
      <CardContent className="space-y-5">
        <div className="space-y-4">
          {steps.map((step, index) => {
            const Icon =
              step.state === "done"
                ? CheckCircle2
                : step.state === "running"
                  ? LoaderCircle
                  : step.state === "warning"
                    ? TriangleAlert
                    : step.state === "failed"
                      ? XCircle
                      : CircleDashed;
            const iconClassName =
              step.state === "done"
                ? "text-emerald-600"
                : step.state === "running"
                  ? "animate-spin text-primary"
                  : step.state === "warning"
                    ? "text-amber-500"
                    : step.state === "failed"
                      ? "text-rose-500"
                      : "text-slate-400";

            return (
              <article key={step.key} className="grid gap-4 sm:grid-cols-[auto_minmax(0,1fr)_auto]">
                <div className="relative flex flex-col items-center">
                  <span className="z-10 flex size-10 items-center justify-center rounded-2xl border border-border bg-card shadow-sm">
                    <Icon className={`size-5 ${iconClassName}`} />
                  </span>
                  {index < steps.length - 1 ? (
                    <span className="absolute left-1/2 top-10 h-[calc(100%+0.75rem)] w-px -translate-x-1/2 bg-border" />
                  ) : null}
                </div>
                <div className="rounded-2xl border border-border/70 bg-card/70 p-4">
                  <div className="flex flex-wrap items-center gap-2">
                    <p className="text-base font-semibold text-foreground">{step.label}</p>
                    <Badge variant="neutral">Étape {index + 1}</Badge>
                  </div>
                  <p className="mt-2 text-sm leading-6 text-muted-foreground">{step.description}</p>
                </div>
                <div className="self-start">
                  <Badge
                    variant={
                      step.state === "done"
                        ? "success"
                        : step.state === "warning" || step.state === "running"
                          ? "warning"
                          : step.state === "failed"
                            ? "danger"
                            : "neutral"
                    }
                  >
                    {stateLabels[step.state]}
                  </Badge>
                </div>
              </article>
            );
          })}
        </div>

        <div
          className={`rounded-2xl border p-4 ${
            hasFailure
              ? "border-rose-200 bg-rose-50 dark:border-rose-900 dark:bg-rose-950/30"
              : hasWarning
                ? "border-amber-200 bg-amber-50 dark:border-amber-900 dark:bg-amber-950/30"
                : allDone
                  ? "border-emerald-200 bg-emerald-50 shadow-[0_0_0_1px_rgba(23,198,83,0.06),0_0_40px_rgba(23,198,83,0.10)] dark:border-emerald-900 dark:bg-emerald-950/30"
                  : "border-border bg-secondary/50"
          }`}
        >
          <p className="text-sm font-semibold uppercase tracking-[0.18em] text-muted-foreground">Clôture du pipeline</p>
          <strong className="mt-2 block text-lg font-semibold tracking-tight text-foreground">{completionLabel}</strong>
          <p className="mt-2 text-sm leading-6 text-muted-foreground">{completionNote}</p>
        </div>
      </CardContent>
    </Card>
  );
}
