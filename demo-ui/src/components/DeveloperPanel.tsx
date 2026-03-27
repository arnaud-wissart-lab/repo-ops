import { useEffect, useRef, useState } from "react";
import { Braces, ClipboardCopy, Logs } from "lucide-react";
import type { DeveloperLogEntry, DemoRunState } from "../types";
import { formatDateTime, toPrettyJson } from "../utils";
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
import { Tabs, TabsContent, TabsList, TabsTrigger } from "./ui/tabs";

interface DeveloperPanelProps {
  logs: DeveloperLogEntry[];
  run: DemoRunState | null;
}

type TabKey = "logs" | "json";

export function DeveloperPanel({ logs, run }: DeveloperPanelProps) {
  const [activeTab, setActiveTab] = useState<TabKey>("logs");
  const [copied, setCopied] = useState(false);
  const [autoScroll, setAutoScroll] = useState(true);
  const consoleRef = useRef<HTMLDivElement | null>(null);
  const jsonContent = run ? toPrettyJson(run) : "Aucun JSON à afficher.";
  const jsonLines = jsonContent.split("\n");

  useEffect(() => {
    if (!autoScroll || activeTab !== "logs" || !consoleRef.current) {
      return;
    }

    consoleRef.current.scrollTop = consoleRef.current.scrollHeight;
  }, [activeTab, autoScroll, logs]);

  async function copyJson() {
    if (!run) {
      return;
    }

    await navigator.clipboard.writeText(toPrettyJson(run));
    setCopied(true);
    window.setTimeout(() => setCopied(false), 1500);
  }

  return (
    <Card className="section-enter">
      <CardHeader>
        <CardHeading>
          <div className="mb-2 flex flex-wrap items-center gap-2">
            <Badge variant="neutral">Mode développeur</Badge>
            <Badge variant="info">Inspection technique</Badge>
          </div>
          <CardTitle>Sortie technique (mode développeur)</CardTitle>
          <CardDescription>
            Logs structurés et JSON brut prêts pour inspection, copie et démonstration technique.
          </CardDescription>
        </CardHeading>
      </CardHeader>
      <CardContent>
        <Tabs value={activeTab} onValueChange={(value) => setActiveTab(value as TabKey)}>
          <div className="developer-toolbar">
            <TabsList>
              <TabsTrigger value="logs">
                <Logs className="size-4" />
                Logs
              </TabsTrigger>
              <TabsTrigger value="json">
                <Braces className="size-4" />
                JSON brut
              </TabsTrigger>
            </TabsList>

            {activeTab === "logs" ? (
              <label className="inline-flex items-center gap-2 text-sm text-muted-foreground">
                <input
                  type="checkbox"
                  checked={autoScroll}
                  onChange={(event) => setAutoScroll(event.target.checked)}
                />
                Auto-scroll
              </label>
            ) : (
              <Button
                type="button"
                variant="secondary"
                size="sm"
                onClick={() => void copyJson()}
                disabled={!run}
              >
                <ClipboardCopy className="size-4" />
                {copied ? "JSON copié" : "Copier le JSON"}
              </Button>
            )}
          </div>

          <TabsContent value="logs">
            <div className="mb-3 flex items-center justify-between gap-3 text-sm">
              <span className="font-medium text-foreground">Trace d’exécution</span>
              <span className="text-muted-foreground">{logs.length} entrée(s)</span>
            </div>
            <div ref={consoleRef} className="code-surface max-h-[34rem] overflow-auto p-4">
              {logs.length === 0 ? (
                <p className="text-sm text-slate-400">Aucun log disponible pour l’instant.</p>
              ) : (
                <div className="space-y-2 font-mono text-[13px]">
                  {logs.map((log, index) => (
                    <div
                      key={`${log.timestamp}-${index}`}
                      className="grid gap-2 rounded-lg border border-slate-800/70 bg-slate-950/70 px-3 py-2 md:grid-cols-[168px_72px_96px_minmax(0,1fr)]"
                    >
                      <span className="text-slate-400">{formatDateTime(log.timestamp)}</span>
                      <span
                        className={
                          log.level === "ERROR"
                            ? "font-semibold text-rose-300"
                            : log.level === "WARN"
                              ? "font-semibold text-amber-300"
                              : "font-semibold text-emerald-300"
                        }
                      >
                        {log.level}
                      </span>
                      <span className="text-sky-300">{log.source}</span>
                      <span className="break-words text-slate-100">{log.message}</span>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </TabsContent>

          <TabsContent value="json">
            <div className="mb-3 flex items-center justify-between gap-3 text-sm">
              <span className="font-medium text-foreground">Sortie technique brute</span>
              <span className="text-muted-foreground">JSON formaté et copiable</span>
            </div>
            <div className="code-surface grid max-h-[34rem] grid-cols-[56px_minmax(0,1fr)] overflow-hidden">
              <div className="overflow-hidden border-r border-slate-800/80 bg-slate-900/80 px-3 py-4 text-right font-mono text-xs text-slate-500">
                {jsonLines.map((_, index) => (
                  <div key={`line-${index + 1}`} className="leading-6">
                    {index + 1}
                  </div>
                ))}
              </div>
              <pre className="overflow-auto p-4 font-mono text-[13px] leading-6 text-slate-100">{jsonContent}</pre>
            </div>
          </TabsContent>
        </Tabs>
      </CardContent>
    </Card>
  );
}
