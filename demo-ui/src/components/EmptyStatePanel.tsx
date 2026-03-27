import { Bot, PlayCircle, Radar, Rocket } from "lucide-react";
import { Badge } from "./ui/badge";
import { Card, CardContent, CardHeader, CardHeading, CardTitle } from "./ui/card";

export function EmptyStatePanel() {
  return (
    <Card className="section-enter">
      <CardHeader>
        <CardHeading>
          <div className="mb-2 flex items-center gap-2">
            <Badge variant="info">Guide rapide</Badge>
            <Bot className="size-4 text-primary" />
          </div>
          <CardTitle>Comment utiliser cette page</CardTitle>
        </CardHeading>
      </CardHeader>
      <CardContent className="grid gap-4 md:grid-cols-3">
        <article className="rounded-2xl border border-border/70 bg-card/80 p-5">
          <div className="mb-4 flex items-center gap-3">
            <span className="rounded-xl bg-blue-50 p-2 text-blue-600 dark:bg-blue-950/50 dark:text-blue-300">
              <Radar className="size-5" />
            </span>
            <h3 className="font-semibold">1. Comprendre le rôle de RepoOps</h3>
          </div>
          <p className="text-sm leading-6 text-muted-foreground">
            RepoOps suit un run de maintenance logicielle : collecte GitHub, qualification, décisions, prompts et synthèse.
          </p>
        </article>

        <article className="rounded-2xl border border-border/70 bg-card/80 p-5">
          <div className="mb-4 flex items-center gap-3">
            <span className="rounded-xl bg-emerald-50 p-2 text-emerald-600 dark:bg-emerald-950/50 dark:text-emerald-300">
              <PlayCircle className="size-5" />
            </span>
            <h3 className="font-semibold">2. Démarrer sans risque</h3>
          </div>
          <p className="text-sm leading-6 text-muted-foreground">
            Utilisez <strong>Charger un exemple</strong> pour voir un run complet immédiatement, sans dépendre du backend.
          </p>
        </article>

        <article className="rounded-2xl border border-border/70 bg-card/80 p-5">
          <div className="mb-4 flex items-center gap-3">
            <span className="rounded-xl bg-violet-50 p-2 text-violet-600 dark:bg-violet-950/50 dark:text-violet-300">
              <Rocket className="size-5" />
            </span>
            <h3 className="font-semibold">3. Passer au réel</h3>
          </div>
          <p className="text-sm leading-6 text-muted-foreground">
            Lancez ensuite une analyse réelle si le worker local est disponible. Le mode dry-run reste actif pour les actions sensibles.
          </p>
        </article>
      </CardContent>
    </Card>
  );
}
