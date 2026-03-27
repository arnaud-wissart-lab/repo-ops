import { useState } from "react";
import { ArrowUpRight, ClipboardCopy, Sparkles } from "lucide-react";
import type { GeneratedPrompt } from "../types";
import { truncate } from "../utils";
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

interface PromptSectionProps {
  prompts: GeneratedPrompt[];
}

function PromptRow({ prompt }: { prompt: GeneratedPrompt }) {
  const [copied, setCopied] = useState(false);

  async function copyPrompt() {
    await navigator.clipboard.writeText(prompt.promptText);
    setCopied(true);
    window.setTimeout(() => setCopied(false), 1500);
  }

  return (
    <details className="group rounded-2xl border border-border/80 bg-card/80 transition-colors hover:border-primary/25">
      <summary className="flex cursor-pointer list-none items-start justify-between gap-4 px-5 py-4">
        <div className="space-y-2">
          <div className="flex flex-wrap items-center gap-2">
            <Badge variant="neutral">{prompt.promptType}</Badge>
            <Badge variant={prompt.priority.toLowerCase() === "high" ? "danger" : prompt.priority.toLowerCase() === "medium" ? "warning" : "success"}>
              {prompt.priority}
            </Badge>
          </div>
          <div>
            <h3 className="text-base font-semibold tracking-tight text-foreground">
              {prompt.pullRequestTitle || prompt.repository}
            </h3>
            <p className="text-sm text-muted-foreground">{prompt.repository}</p>
          </div>
        </div>
        <span className="rounded-lg bg-secondary px-3 py-1.5 text-xs font-semibold uppercase tracking-[0.14em] text-muted-foreground transition-colors group-open:bg-primary group-open:text-primary-foreground">
          Déplier
        </span>
      </summary>

      <div className="space-y-4 border-t border-border/70 px-5 py-4">
        <div className="flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
          <span>{prompt.repository}</span>
          {prompt.pullRequestNumber ? <span>PR #{prompt.pullRequestNumber}</span> : null}
          <span>Checks : {prompt.context.checksStatus || "non précisé"}</span>
        </div>

        <div className="grid gap-4 xl:grid-cols-[minmax(0,0.9fr)_minmax(0,1.1fr)]">
          <div className="space-y-4">
            <div className="rounded-2xl border border-border/70 bg-secondary/40 p-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-muted-foreground">Problème ciblé</p>
              <p className="mt-2 text-sm leading-6 text-foreground">{prompt.context.problemSummary}</p>
            </div>
            <div className="rounded-2xl border border-border/70 bg-secondary/40 p-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-muted-foreground">Recommandation</p>
              <p className="mt-2 text-sm leading-6 text-foreground">{prompt.context.recommendation}</p>
            </div>
            <div className="rounded-2xl border border-dashed border-border bg-card/70 p-4 text-sm text-muted-foreground">
              <strong className="block text-xs font-semibold uppercase tracking-[0.18em] text-muted-foreground">Aperçu</strong>
              <p className="mt-2 leading-6">{truncate(prompt.promptText, 240)}</p>
            </div>
          </div>

          <pre className="code-surface overflow-x-auto p-4 text-sm leading-6">{prompt.promptText}</pre>
        </div>

        <div className="flex flex-wrap items-center gap-3">
          {prompt.pullRequestUrl ? (
            <Button asChild variant="secondary" size="sm">
              <a href={prompt.pullRequestUrl} target="_blank" rel="noreferrer">
                Ouvrir la pull request
                <ArrowUpRight className="size-4" />
              </a>
            </Button>
          ) : null}
          <Button type="button" variant="outline" size="sm" onClick={copyPrompt}>
            <ClipboardCopy className="size-4" />
            {copied ? "Prompt copié" : "Copier le prompt"}
          </Button>
        </div>
      </div>
    </details>
  );
}

export function PromptSection({ prompts }: PromptSectionProps) {
  return (
    <Card className="section-enter">
      <CardHeader>
        <CardHeading>
          <div className="mb-2 flex items-center gap-2">
            <Badge variant="neutral">Prompts générés</Badge>
            <Sparkles className="size-4 text-primary" />
          </div>
          <CardTitle>Prompts prêts pour Codex</CardTitle>
          <CardDescription>
            Les templates restent lisibles, techniques et directement copiables.
          </CardDescription>
        </CardHeading>
      </CardHeader>
      <CardContent>
      {prompts.length === 0 ? (
        <div className="rounded-2xl border border-dashed border-border bg-secondary/50 p-6 text-sm text-muted-foreground">
          Aucun prompt n’a été généré pour ce run.
        </div>
      ) : (
        <div className="grid gap-4">
          {prompts.map((prompt) => (
            <PromptRow
              key={`${prompt.repository}-${prompt.pullRequestNumber ?? "repo"}-${prompt.promptType}`}
              prompt={prompt}
            />
          ))}
        </div>
      )}
      </CardContent>
    </Card>
  );
}
