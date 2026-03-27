import { Bot, GitPullRequest, ShieldAlert, Sparkles, TriangleAlert } from "lucide-react";
import { Card, CardContent } from "./ui/card";

interface KpiGridProps {
  analyzedPullRequests: number;
  readyPullRequests: number;
  blockedPullRequests: number;
  vulnerabilities: number;
  proposedActions: number;
}

const items = [
  {
    key: "analyzed",
    label: "PR analysées",
    icon: GitPullRequest,
    accent: "Vue d’ensemble",
    iconClassName: "text-blue-600 bg-blue-50 dark:bg-blue-950/50 dark:text-blue-300",
  },
  {
    key: "ready",
    label: "PR prêtes",
    icon: Sparkles,
    accent: "Prêtes à traiter",
    iconClassName: "text-emerald-600 bg-emerald-50 dark:bg-emerald-950/50 dark:text-emerald-300",
  },
  {
    key: "blocked",
    label: "PR bloquées",
    icon: TriangleAlert,
    accent: "Points d’attention",
    iconClassName: "text-amber-600 bg-amber-50 dark:bg-amber-950/50 dark:text-amber-300",
  },
  {
    key: "vulnerabilities",
    label: "Vulnérabilités",
    icon: ShieldAlert,
    accent: "Risque sécurité",
    iconClassName: "text-rose-600 bg-rose-50 dark:bg-rose-950/50 dark:text-rose-300",
  },
  {
    key: "actions",
    label: "Actions proposées",
    icon: Bot,
    accent: "Sortie superviseur",
    iconClassName: "text-violet-600 bg-violet-50 dark:bg-violet-950/50 dark:text-violet-300",
  },
] as const;

export function KpiGrid({
  analyzedPullRequests,
  readyPullRequests,
  blockedPullRequests,
  vulnerabilities,
  proposedActions,
}: KpiGridProps) {
  const values = {
    analyzed: analyzedPullRequests,
    ready: readyPullRequests,
    blocked: blockedPullRequests,
    vulnerabilities,
    actions: proposedActions,
  };

  return (
    <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
      {items.map((item) => (
        <Card key={item.key} className="section-enter transition-transform duration-200 hover:-translate-y-0.5">
          <CardContent className="space-y-4 px-5 py-5">
            <div className="flex items-start justify-between gap-3">
              <div className="space-y-1">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                  {item.accent}
                </p>
                <h3 className="text-sm font-medium text-foreground">{item.label}</h3>
              </div>
              <span className={`inline-flex rounded-xl p-2 ${item.iconClassName}`}>
                <item.icon className="size-4" />
              </span>
            </div>
            <div className="space-y-1">
              <strong className="block text-4xl font-semibold tracking-tight text-foreground">
                {values[item.key]}
              </strong>
              <span className="text-sm text-muted-foreground">Lecture instantanée du run</span>
            </div>
          </CardContent>
        </Card>
      ))}
    </section>
  );
}
