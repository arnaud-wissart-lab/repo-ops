import type { SupervisorAction } from "../types";
import { ArrowUpRight, GitPullRequest, ShieldAlert } from "lucide-react";
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
import { Button } from "./ui/button";

interface DecisionSectionProps {
  actions: SupervisorAction[];
}

function typeMeta(type: string): {
  label: string;
  badge: "success" | "warning" | "danger" | "neutral";
} {
  const normalized = type.toLowerCase();

  if (normalized === "automergeeligible") {
    return { label: "Auto-merge éligible", badge: "success" };
  }

  if (normalized === "fixrequired") {
    return { label: "Correctif requis", badge: "danger" };
  }

  if (normalized === "review") {
    return { label: "Revue", badge: "warning" };
  }

  return { label: "Ignoré", badge: "neutral" };
}

function toneFromPriority(priority: string): "done" | "warning" | "failed" | "neutral" {
  const normalized = priority.toLowerCase();

  if (normalized === "high") {
    return "failed";
  }

  if (normalized === "medium") {
    return "warning";
  }

  if (normalized === "low") {
    return "done";
  }

  return "neutral";
}

export function DecisionSection({ actions }: DecisionSectionProps) {
  return (
    <Card className="section-enter">
      <CardHeader>
        <CardHeading>
          <p className="text-sm font-semibold uppercase tracking-[0.18em] text-muted-foreground">Décisions</p>
          <CardTitle>Pourquoi ces décisions</CardTitle>
          <CardDescription>
            Le moteur reste explicable. Chaque carte montre la cible, la priorité et la raison métier.
          </CardDescription>
        </CardHeading>
      </CardHeader>
      <CardContent>
        {actions.length === 0 ? (
          <div className="rounded-lg border border-dashed border-border bg-secondary/50 p-6 text-sm text-muted-foreground">
            Aucune action structurée n’a été produite pour ce scénario.
          </div>
        ) : (
          <div className="grid gap-4">
            {actions.map((action) => {
              const meta = typeMeta(action.type);

              return (
                <article
                  key={`${action.repository}-${action.pullRequestNumber ?? "repo"}-${action.type}`}
                  className="decision-row p-5"
                >
                  <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
                    <div className="space-y-3">
                      <div className="flex flex-wrap items-center gap-2">
                        <Badge variant={meta.badge}>{meta.label}</Badge>
                        <StatusPill label={action.priority} tone={toneFromPriority(action.priority)} />
                        {action.isSecurityRelated ? (
                          <Badge variant="danger">
                            <ShieldAlert className="size-3.5" />
                            Sécurité {action.securitySeverity || "prioritaire"}
                          </Badge>
                        ) : null}
                      </div>

                      <div className="space-y-1">
                        <h3 className="text-lg font-semibold text-foreground">
                          {action.pullRequestNumber ? `PR #${action.pullRequestNumber}` : action.repository} — {meta.label}
                        </h3>
                        <p className="text-sm text-muted-foreground">
                          {action.pullRequestTitle ?? "Action structurée au niveau dépôt"}
                        </p>
                      </div>
                    </div>

                    <div className="grid gap-2 text-sm text-muted-foreground sm:grid-cols-2 lg:min-w-72">
                      <div className="surface-subtle px-3 py-2">
                        <span className="block text-[11px] font-semibold uppercase tracking-[0.16em]">Type</span>
                        <strong className="text-sm font-semibold text-foreground">{meta.label}</strong>
                      </div>
                      <div className="surface-subtle px-3 py-2">
                        <span className="block text-[11px] font-semibold uppercase tracking-[0.16em]">Priorité</span>
                        <strong className="text-sm font-semibold text-foreground">{action.priority}</strong>
                      </div>
                      <div className="surface-subtle px-3 py-2 sm:col-span-2">
                        <span className="block text-[11px] font-semibold uppercase tracking-[0.16em]">Cible</span>
                        <strong className="text-sm font-semibold text-foreground">{action.repository}</strong>
                      </div>
                    </div>
                  </div>

                  <div className="mt-4 grid gap-4 xl:grid-cols-[minmax(0,1fr)_minmax(0,0.9fr)]">
                    <div className="surface-subtle p-4">
                      <p className="mb-2 text-xs font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                        Raison
                      </p>
                      <p className="text-sm leading-6 text-foreground">{action.reason}</p>
                    </div>

                    <div className="surface-subtle p-4">
                      <p className="mb-2 text-xs font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                        Action suggérée
                      </p>
                      <p className="text-sm leading-6 text-foreground">
                        {action.recommendation || "Aucune recommandation supplémentaire."}
                      </p>
                    </div>
                  </div>

                  <div className="mt-4 flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
                    <span className="inline-flex items-center gap-1">
                      <GitPullRequest className="size-3.5" />
                      {action.pullRequestNumber ? `PR #${action.pullRequestNumber}` : "Niveau dépôt"}
                    </span>
                    {action.checksStatus ? <span>Checks : {action.checksStatus}</span> : null}
                  </div>

                  {action.pullRequestUrl ? (
                    <div className="mt-4">
                      <Button asChild variant="secondary" size="sm">
                        <a href={action.pullRequestUrl} target="_blank" rel="noreferrer">
                          Ouvrir la pull request source
                          <ArrowUpRight className="size-4" />
                        </a>
                      </Button>
                    </div>
                  ) : null}
                </article>
              );
            })}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
