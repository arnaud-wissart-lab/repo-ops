import type { DemoMode, DeploymentExecutionResult, UiStatus } from "../types";
import {
  ArrowRight,
  ExternalLink,
  Rocket,
  ServerCog,
  ShieldCheck,
  Sparkles,
} from "lucide-react";
import { demoRepositorySlug, demoRepositoryUrl } from "../utils";
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

interface HeroSectionProps {
  mode: DemoMode;
  status: UiStatus;
  deploymentStatus: UiStatus;
  deploymentResult: DeploymentExecutionResult | null;
  deploymentError: string;
  onRun: () => Promise<void>;
  onDeploy: () => Promise<void>;
}

function modeLabel(mode: DemoMode): string {
  return mode === "api" ? "Source GitHub réelle" : "Source inconnue";
}

export function HeroSection({
  mode,
  status,
  deploymentStatus,
  deploymentResult,
  deploymentError,
  onRun,
  onDeploy,
}: HeroSectionProps) {
  const isLoading = status === "loading";
  const isDeploying = deploymentStatus === "loading";
  const isBusy = isLoading || isDeploying;

  return (
    <section className="grid gap-6 xl:grid-cols-[minmax(0,1.2fr)_minmax(360px,0.8fr)] xl:items-start">
      <Card className="section-enter self-start overflow-hidden">
        <CardContent className="px-7 py-7 lg:px-8 lg:py-8">
          <div className="mb-4 flex flex-wrap items-center gap-2">
            <Badge variant="neutral">Tableau de bord</Badge>
            <Badge variant="neutral">{modeLabel(mode)}</Badge>
            <Badge variant="success">Validation humaine conservée</Badge>
          </div>

          <div className="max-w-4xl space-y-6">
            <div className="space-y-3">
              <p className="text-sm font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                RepoOps Live Demo
              </p>
              <h2 className="max-w-3xl text-3xl font-semibold tracking-tight text-foreground md:text-4xl">
                Centre de supervision de maintenance logicielle
              </h2>
              <p className="max-w-3xl text-sm leading-7 text-muted-foreground md:text-base">
                RepoOps analyse le dépôt public <strong>{demoRepositorySlug}</strong>, recoupe
                les pull requests techniques, explique les arbitrages du superviseur et prépare
                la prochaine action sans vous obliger à reconstituer le contexte à la main dans GitHub.
              </p>
            </div>

            <div className="grid gap-3 md:grid-cols-3">
              <div className="surface-subtle p-4">
                <div className="mb-3 flex items-center gap-3">
                  <span className="rounded-md bg-blue-50 p-2 text-blue-600 dark:bg-blue-950/50 dark:text-blue-300">
                    <ServerCog className="size-5" />
                  </span>
                  <h2 className="font-semibold">Ce que RepoOps apporte</h2>
                </div>
                <p className="text-sm leading-6 text-muted-foreground">
                  L’écran regroupe les signaux épars de GitHub, hiérarchise les PR ouvertes et
                  explique pourquoi une action mérite une revue.
                </p>
              </div>

              <div className="surface-subtle p-4">
                <div className="mb-3 flex items-center gap-3">
                  <span className="rounded-md bg-emerald-50 p-2 text-emerald-600 dark:bg-emerald-950/50 dark:text-emerald-300">
                    <Sparkles className="size-5" />
                  </span>
                  <h2 className="font-semibold">Comment le lire</h2>
                </div>
                <p className="text-sm leading-6 text-muted-foreground">
                  Ouvrez d’abord le dépôt GitHub, puis lancez l’analyse pour comparer la vue brute
                  GitHub avec la lecture priorisée de RepoOps.
                </p>
              </div>

              <div className="surface-subtle p-4">
                <div className="mb-3 flex items-center gap-3">
                  <span className="rounded-md bg-violet-50 p-2 text-violet-600 dark:bg-violet-950/50 dark:text-violet-300">
                    <ShieldCheck className="size-5" />
                  </span>
                  <h2 className="font-semibold">Ce qui reste sous contrôle</h2>
                </div>
                <p className="text-sm leading-6 text-muted-foreground">
                  Dry-run par défaut, aucune opération Git depuis la page et validation humaine conservée
                  avant toute action sensible.
                </p>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card className="section-enter self-start">
        <CardHeader>
          <CardHeading>
            <p className="text-sm font-semibold uppercase tracking-[0.18em] text-muted-foreground">
              Actions rapides
            </p>
            <CardTitle>Piloter la démonstration</CardTitle>
            <CardDescription>
              Le scénario s’appuie sur un vrai dépôt GitHub public pour rendre les PR, les checks
              et les recommandations immédiatement vérifiables.
            </CardDescription>
          </CardHeading>
        </CardHeader>
        <CardContent className="space-y-5">
          <div className="surface-accent p-4">
            <div className="flex items-center gap-2">
              <Badge variant="info">Parcours recommandé</Badge>
              <p className="text-sm font-medium text-blue-700 dark:text-blue-300">
                1. Dépôt GitHub · 2. Analyse RepoOps · 3. Relecture
              </p>
            </div>
            <p className="mt-2 text-sm leading-6 text-blue-900 dark:text-blue-100">
              Commencez par observer les PR ouvertes dans GitHub, puis lancez l’analyse pour voir
              comment RepoOps les trie, les résume et prépare la suite.
            </p>
          </div>

          <div className="grid gap-3">
            <a href={demoRepositoryUrl} target="_blank" rel="noreferrer" className="inline-flex">
              <Button
                type="button"
                size="lg"
                className="h-auto min-w-0 flex-1 items-start justify-between whitespace-normal px-5 py-4 text-left"
              >
                <span className="min-w-0 space-y-1 pr-3">
                  <span className="block text-base font-semibold">Ouvrir le dépôt GitHub</span>
                  <span className="block break-words text-sm font-normal text-primary-foreground/85">
                    Consultez les vraies branches et les vraies pull requests du scénario public.
                  </span>
                </span>
                <ExternalLink className="mt-0.5 size-5 shrink-0" />
              </Button>
            </a>

            <Button
              type="button"
              size="lg"
              variant="secondary"
              className="h-auto min-w-0 items-start justify-between whitespace-normal px-5 py-4 text-left"
              onClick={() => void onRun()}
              disabled={isBusy}
            >
              <span className="min-w-0 space-y-1 pr-3">
                <span className="block text-base font-semibold">
                  {isLoading ? "Analyse en cours..." : "Analyser le dépôt de démonstration"}
                </span>
                <span className="block break-words text-sm font-normal text-muted-foreground">
                  Interroge le worker local pour reconstruire une lecture RepoOps claire à partir
                  des PR réellement ouvertes.
                </span>
              </span>
              <ArrowRight className={`mt-0.5 size-5 shrink-0 ${isLoading ? "animate-pulse" : ""}`} />
            </Button>

            <Button
              type="button"
              variant="outline"
              size="lg"
              className="h-auto min-w-0 items-start justify-between whitespace-normal px-5 py-4 text-left"
              onClick={() => void onDeploy()}
              disabled={isBusy}
            >
              <span className="min-w-0 space-y-1 pr-3">
                <span className="block text-base font-semibold">
                  {isDeploying ? "Déploiement en cours..." : "Déployer en local"}
                </span>
                <span className="block break-words text-sm font-normal text-muted-foreground">
                  Déclenche le workflow manuel prévu pour votre machine personnelle.
                </span>
              </span>
              <Rocket className={`mt-0.5 size-5 shrink-0 ${isDeploying ? "animate-bounce" : ""}`} />
            </Button>
          </div>

          <div className="surface-subtle p-4 text-sm text-muted-foreground">
            <div className="flex items-center justify-between gap-3">
              <span>Analyse</span>
              <StatusPill label={isLoading ? "En cours" : "Prête"} tone={isLoading ? "running" : "neutral"} />
            </div>
            <div className="mt-3 flex items-center justify-between gap-3">
              <span>Déploiement</span>
              <StatusPill
                label={isDeploying ? "En cours" : "Manuel"}
                tone={isDeploying ? "running" : "warning"}
              />
            </div>

            {deploymentResult ? (
              <div className="mt-3 border-t border-border pt-3 text-sm leading-6 text-foreground">
                <p className="font-semibold">Dernier déploiement</p>
                <p>{deploymentResult.summary}</p>
              </div>
            ) : null}

            {deploymentError ? (
              <div className="mt-3 border-t border-border pt-3 text-sm leading-6 text-rose-600 dark:text-rose-300">
                {deploymentError}
              </div>
            ) : null}
          </div>
        </CardContent>
      </Card>
    </section>
  );
}
