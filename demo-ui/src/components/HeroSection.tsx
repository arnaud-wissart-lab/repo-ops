import type { DemoMode, DeploymentExecutionResult, UiStatus } from "../types";
import { ArrowRight, PlayCircle, Rocket, ServerCog, ShieldCheck, Sparkles } from "lucide-react";
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
  onLoadMock: () => Promise<void>;
  onDeploy: () => Promise<void>;
}

function modeLabel(mode: DemoMode): string {
  if (mode === "mock") {
    return "Source mock";
  }

  if (mode === "auto") {
    return "Mode auto";
  }

  return "Source API";
}

export function HeroSection({
  mode,
  status,
  deploymentStatus,
  deploymentResult,
  deploymentError,
  onRun,
  onLoadMock,
  onDeploy,
}: HeroSectionProps) {
  const isLoading = status === "loading";
  const isDeploying = deploymentStatus === "loading";
  const isBusy = isLoading || isDeploying;

  return (
    <section className="grid gap-6 xl:grid-cols-[minmax(0,1.2fr)_minmax(360px,0.8fr)]">
      <Card className="section-enter overflow-hidden">
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
                Cette page sert à visualiser un run RepoOps comme un écran de pilotage :
                état du pipeline, arbitrages du superviseur, prompts techniques, synthèse
                métier et sortie brute pour relecture.
              </p>
            </div>

            <div className="grid gap-3 md:grid-cols-3">
              <div className="surface-subtle p-4">
                <div className="mb-3 flex items-center gap-3">
                  <span className="rounded-md bg-blue-50 p-2 text-blue-600 dark:bg-blue-950/50 dark:text-blue-300">
                    <ServerCog className="size-5" />
                  </span>
                  <h2 className="font-semibold">Comprendre le run</h2>
                </div>
                <p className="text-sm leading-6 text-muted-foreground">
                  L’écran montre ce qui a été détecté, les décisions prises et ce qui reste à valider.
                </p>
              </div>

              <div className="surface-subtle p-4">
                <div className="mb-3 flex items-center gap-3">
                  <span className="rounded-md bg-emerald-50 p-2 text-emerald-600 dark:bg-emerald-950/50 dark:text-emerald-300">
                    <Sparkles className="size-5" />
                  </span>
                  <h2 className="font-semibold">Par où commencer</h2>
                </div>
                <p className="text-sm leading-6 text-muted-foreground">
                  Chargez d’abord un exemple complet, puis testez l’analyse réelle si le worker répond.
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
                  Dry-run par défaut, pas d’opération Git depuis cette page et revue humaine conservée.
                </p>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card className="section-enter h-full">
        <CardHeader>
          <CardHeading>
            <p className="text-sm font-semibold uppercase tracking-[0.18em] text-muted-foreground">
              Actions rapides
            </p>
            <CardTitle>Piloter la démonstration</CardTitle>
            <CardDescription>
              Utilisez les actions principales, puis relisez le run dans les cartes du tableau de bord.
            </CardDescription>
          </CardHeading>
        </CardHeader>
        <CardContent className="space-y-5">
          <div className="surface-accent p-4">
            <div className="flex items-center gap-2">
              <Badge variant="info">Parcours recommandé</Badge>
              <p className="text-sm font-medium text-blue-700 dark:text-blue-300">
                1. Exemple · 2. Analyse · 3. Relecture
              </p>
            </div>
            <p className="mt-2 text-sm leading-6 text-blue-900 dark:text-blue-100">
              L’exemple remplit immédiatement le tableau de bord. L’analyse réelle vérifie ensuite la chaîne locale sans rompre le cadre dry-run.
            </p>
          </div>

          <div className="grid gap-3">
            <Button
              type="button"
              size="lg"
              className="h-auto min-w-0 items-start justify-between px-5 py-4 text-left"
              onClick={() => void onLoadMock()}
              disabled={isBusy}
            >
              <span className="min-w-0 space-y-1">
                <span className="block text-base font-semibold">Charger un exemple</span>
                <span className="block text-sm font-normal text-primary-foreground/85">
                  Remplit immédiatement la page avec un run réaliste et commenté.
                </span>
              </span>
              <PlayCircle className="mt-0.5 size-5 shrink-0" />
            </Button>

            <Button
              type="button"
              size="lg"
              variant="secondary"
              className="h-auto min-w-0 items-start justify-between px-5 py-4 text-left"
              onClick={() => void onRun()}
              disabled={isBusy}
            >
              <span className="min-w-0 space-y-1">
                <span className="block text-base font-semibold">
                  {isLoading ? "Analyse en cours..." : "Lancer une analyse réelle"}
                </span>
                <span className="block text-sm font-normal text-muted-foreground">
                  Utilise l’API locale si elle répond, sinon la page bascule automatiquement sur le mock.
                </span>
              </span>
              <ArrowRight className={`mt-0.5 size-5 shrink-0 ${isLoading ? "animate-pulse" : ""}`} />
            </Button>

            <Button
              type="button"
              variant="outline"
              size="lg"
              className="h-auto min-w-0 items-start justify-between px-5 py-4 text-left"
              onClick={() => void onDeploy()}
              disabled={isBusy}
            >
              <span className="min-w-0 space-y-1">
                <span className="block text-base font-semibold">
                  {isDeploying ? "Déploiement en cours..." : "Déployer en local"}
                </span>
                <span className="block text-sm font-normal text-muted-foreground">
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
