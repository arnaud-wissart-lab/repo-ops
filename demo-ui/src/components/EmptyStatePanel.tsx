import { Bot, EyeOff, PlayCircle, Radar, Rocket } from "lucide-react";
import { Badge } from "./ui/badge";
import { Button } from "./ui/button";
import { Card, CardContent, CardHeader, CardHeading, CardTitle } from "./ui/card";

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
        <article className="surface-subtle p-5">
          <div className="mb-4 flex items-center gap-3">
            <span className="rounded-md bg-blue-50 p-2 text-blue-600 dark:bg-blue-950/50 dark:text-blue-300">
              <Radar className="size-5" />
            </span>
            <h3 className="font-semibold">1. Comprendre RepoOps</h3>
          </div>
          <p className="text-sm leading-6 text-muted-foreground">
            RepoOps suit un run de maintenance logicielle : collecte GitHub, qualification, décisions, prompts et synthèse.
          </p>
        </article>

        <article className="surface-subtle p-5">
          <div className="mb-4 flex items-center gap-3">
            <span className="rounded-md bg-emerald-50 p-2 text-emerald-600 dark:bg-emerald-950/50 dark:text-emerald-300">
              <PlayCircle className="size-5" />
            </span>
            <h3 className="font-semibold">2. Charger un exemple</h3>
          </div>
          <p className="text-sm leading-6 text-muted-foreground">
            Utilisez <strong>Charger un exemple</strong> pour remplir immédiatement le tableau de bord, même sans backend.
          </p>
        </article>

        <article className="surface-subtle p-5">
          <div className="mb-4 flex items-center gap-3">
            <span className="rounded-md bg-violet-50 p-2 text-violet-600 dark:bg-violet-950/50 dark:text-violet-300">
              <Rocket className="size-5" />
            </span>
            <h3 className="font-semibold">3. Tester le réel</h3>
          </div>
          <p className="text-sm leading-6 text-muted-foreground">
            Lancez ensuite une analyse réelle si le worker local répond. Les actions sensibles restent protégées en dry-run.
          </p>
        </article>
      </CardContent>
    </Card>
  );
}
