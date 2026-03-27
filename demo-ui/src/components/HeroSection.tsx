import type { DemoMode, UiStatus } from "../types";
import { ArrowRight, Bot, PlayCircle, Rocket, ShieldCheck } from "lucide-react";
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
  onRun,
  onLoadMock,
  onDeploy,
}: HeroSectionProps) {
  const isLoading = status === "loading";
  const isDeploying = deploymentStatus === "loading";
  const isBusy = isLoading || isDeploying;

  return (
    <header className="grid gap-6 xl:grid-cols-[minmax(0,1.45fr)_minmax(360px,0.95fr)]">
      <Card className="overflow-hidden">
        <CardContent className="px-7 py-7 lg:px-9 lg:py-8">
          <div className="mb-5 flex flex-wrap items-center gap-2">
            <Badge variant="info">RepoOps Live Demo</Badge>
            <Badge variant="neutral">{modeLabel(mode)}</Badge>
            <Badge variant="success">Validation humaine conservée</Badge>
          </div>

          <div className="max-w-4xl space-y-5">
            <div className="space-y-3">
              <p className="text-sm font-semibold uppercase tracking-[0.2em] text-primary">
                Supervision de maintenance logicielle
              </p>
              <h1 className="max-w-3xl text-4xl font-semibold tracking-tight text-foreground md:text-5xl">
                Comprendre en quelques secondes ce que le système a détecté, décidé et recommandé.
              </h1>
              <p className="max-w-3xl text-base leading-7 text-muted-foreground md:text-lg">
                RepoOps agrège les signaux GitHub, qualifie les risques, génère des décisions
                explicables, prépare des prompts techniques et présente un run relisible sans
                déclencher d’action dangereuse depuis cette page.
              </p>
            </div>

            <div className="grid gap-3 md:grid-cols-3">
              <div className="rounded-xl border border-border/70 bg-card/70 p-4">
                <div className="mb-3 flex items-center gap-3">
                  <span className="rounded-xl bg-blue-50 p-2 text-blue-600 dark:bg-blue-950/50 dark:text-blue-300">
                    <Bot className="size-5" />
                  </span>
                  <h2 className="font-semibold">À quoi sert cette page</h2>
                </div>
                <p className="text-sm leading-6 text-muted-foreground">
                  Montrer, sans jargon inutile, un run complet de maintenance logicielle pilotée.
                </p>
              </div>
              <div className="rounded-xl border border-border/70 bg-card/70 p-4">
                <div className="mb-3 flex items-center gap-3">
                  <span className="rounded-xl bg-emerald-50 p-2 text-emerald-600 dark:bg-emerald-950/50 dark:text-emerald-300">
                    <ShieldCheck className="size-5" />
                  </span>
                  <h2 className="font-semibold">Ce que vous pouvez faire</h2>
                </div>
                <p className="text-sm leading-6 text-muted-foreground">
                  Charger un scénario guidé, exécuter une analyse réelle si le worker répond,
                  puis relire le pipeline, les décisions et la sortie technique.
                </p>
              </div>
              <div className="rounded-xl border border-border/70 bg-card/70 p-4">
                <div className="mb-3 flex items-center gap-3">
                  <span className="rounded-xl bg-violet-50 p-2 text-violet-600 dark:bg-violet-950/50 dark:text-violet-300">
                    <Rocket className="size-5" />
                  </span>
                  <h2 className="font-semibold">Ce qui reste sous contrôle</h2>
                </div>
                <p className="text-sm leading-6 text-muted-foreground">
                  Dry-run par défaut, aucune opération Git depuis l’interface et validation humaine
                  requise avant exécution réelle.
                </p>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card className="h-full">
        <CardHeader>
          <CardHeading>
            <p className="text-sm font-semibold uppercase tracking-[0.18em] text-primary">
              Démarrage guidé
            </p>
            <CardTitle>Commencer sans se perdre</CardTitle>
            <CardDescription>
              La lecture la plus simple consiste à charger d’abord un exemple complet, puis à tester
              l’analyse réelle si le backend local est disponible.
            </CardDescription>
          </CardHeading>
        </CardHeader>
        <CardContent className="space-y-5">
          <div className="space-y-3 rounded-2xl border border-blue-100 bg-blue-50/70 p-4 dark:border-blue-900/70 dark:bg-blue-950/20">
            <div className="flex items-center gap-2">
              <Badge variant="info">Parcours recommandé</Badge>
              <p className="text-sm font-medium text-blue-700 dark:text-blue-300">1. Exemple · 2. Analyse · 3. Déploiement</p>
            </div>
            <p className="text-sm leading-6 text-blue-900 dark:text-blue-100">
              Le scénario d’exemple montre immédiatement le rôle de RepoOps. L’analyse réelle permet
              ensuite de vérifier la chaîne locale. Le déploiement reste un bouton manuel distinct.
            </p>
          </div>

          <div className="grid gap-3">
            <Button
              type="button"
              size="lg"
              className="h-auto items-start justify-between rounded-2xl px-5 py-4 text-left"
              onClick={() => void onLoadMock()}
              disabled={isBusy}
            >
              <span className="space-y-1">
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
              className="h-auto items-start justify-between rounded-2xl border-primary/20 px-5 py-4 text-left hover:border-primary/40"
              onClick={() => void onRun()}
              disabled={isBusy}
            >
              <span className="space-y-1">
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
              className="h-auto items-start justify-between rounded-2xl px-5 py-4 text-left"
              onClick={() => void onDeploy()}
              disabled={isBusy}
            >
              <span className="space-y-1">
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

          <div className="grid gap-3 rounded-2xl border border-dashed border-border bg-secondary/50 p-4 text-sm text-muted-foreground">
            <div className="flex items-center justify-between gap-3">
              <span>Analyse</span>
              <StatusPill label={isLoading ? "En cours" : "Prête"} tone={isLoading ? "running" : "neutral"} />
            </div>
            <div className="flex items-center justify-between gap-3">
              <span>Déploiement</span>
              <StatusPill
                label={isDeploying ? "En cours" : "Manuel"}
                tone={isDeploying ? "running" : "warning"}
              />
            </div>
          </div>
        </CardContent>
      </Card>
    </header>
  );
}
