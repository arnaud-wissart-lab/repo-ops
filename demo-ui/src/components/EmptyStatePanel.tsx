import { Bot, EyeOff, GitPullRequest, Radar, Rocket } from "lucide-react";
import { Badge } from "./ui/badge";
import { Button } from "./ui/button";
import { Card, CardContent, CardHeader, CardHeading, CardTitle } from "./ui/card";
import { demoRepositorySlug } from "../utils";

interface EmptyStatePanelProps {
  onDismiss?: () => void;
}

export function EmptyStatePanel({ onDismiss }: EmptyStatePanelProps) {
  return (
    <Card className="section-enter">
      <CardHeader className="flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <CardHeading>
          <div className="mb-2 flex items-center gap-2">
            <Badge variant="info">Guide rapide</Badge>
            <Bot className="size-4 text-primary" />
          </div>
          <CardTitle>Commencer sans se perdre</CardTitle>
        </CardHeading>

        {onDismiss ? (
          <Button type="button" variant="ghost" size="sm" onClick={onDismiss}>
            <EyeOff className="size-4" />
            Masquer l’aide
          </Button>
        ) : null}
      </CardHeader>

      <CardContent className="grid gap-4 md:grid-cols-3">
        <article className="surface-subtle h-full p-5">
          <div className="mb-4 flex items-center gap-3">
            <span className="rounded-md bg-blue-50 p-2 text-blue-600 dark:bg-blue-950/50 dark:text-blue-300">
              <Radar className="size-5" />
            </span>
            <h3 className="font-semibold">1. Comprendre RepoOps</h3>
          </div>
          <p className="break-words text-sm leading-6 text-muted-foreground">
            RepoOps analyse un dépôt GitHub réel, regroupe les signaux utiles et transforme les PR techniques en priorités lisibles.
          </p>
        </article>

        <article className="surface-subtle h-full p-5">
          <div className="mb-4 flex items-center gap-3">
            <span className="rounded-md bg-emerald-50 p-2 text-emerald-600 dark:bg-emerald-950/50 dark:text-emerald-300">
              <GitPullRequest className="size-5" />
            </span>
            <h3 className="font-semibold">2. Observer le dépôt analysé</h3>
          </div>
          <p className="break-words text-sm leading-6 text-muted-foreground">
            Le scénario de démonstration s’appuie sur <strong>{demoRepositorySlug}</strong> avec de vraies branches et de vraies pull requests ouvertes.
          </p>
        </article>

        <article className="surface-subtle h-full p-5">
          <div className="mb-4 flex items-center gap-3">
            <span className="rounded-md bg-violet-50 p-2 text-violet-600 dark:bg-violet-950/50 dark:text-violet-300">
              <Rocket className="size-5" />
            </span>
            <h3 className="font-semibold">3. Lancer l’analyse réelle</h3>
          </div>
          <p className="break-words text-sm leading-6 text-muted-foreground">
            Lancez l’analyse du worker pour voir ce que RepoOps priorise, pourquoi une PR est bloquée et quelle action il recommande ensuite.
          </p>
        </article>
      </CardContent>
    </Card>
  );
}
