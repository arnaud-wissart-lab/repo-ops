import { useState } from "react";
import { Activity, Bot, LayoutDashboard } from "lucide-react";
import {
  buildSupervisorDecisions,
  buildSupervisorPrompts,
  executeCodexPrompts,
  getConfiguredDemoMode,
  getMockDemoRunState,
  runLocalDeployment,
  runMaintenanceReport,
} from "./api";
import { mockDeploymentExecutionResult } from "./mocks/demoData";
import { DecisionSection } from "./components/DecisionSection";
import { DemoModeBadge } from "./components/DemoModeBadge";
import { DeveloperPanel } from "./components/DeveloperPanel";
import { EmptyStatePanel } from "./components/EmptyStatePanel";
import { GlobalStatusBanner } from "./components/GlobalStatusBanner";
import { HeroSection } from "./components/HeroSection";
import { KpiGrid } from "./components/KpiGrid";
import { NarrativeSummary } from "./components/NarrativeSummary";
import { PipelineVisualizer } from "./components/PipelineVisualizer";
import { PromptSection } from "./components/PromptSection";
import { RunSummary } from "./components/RunSummary";
import { Badge } from "./components/ui/badge";
import { Alert, AlertDescription, AlertTitle } from "./components/ui/alert";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardHeading,
  CardTitle,
} from "./components/ui/card";
import type {
  DemoMode,
  DemoRunState,
  DeploymentExecutionResult,
  DeveloperLogEntry,
  MaintenanceRunReport,
  PipelineStep,
  PipelineStepKey,
  PipelineStepState,
  UiStatus,
} from "./types";
import {
  createDerivedLogEntries,
  createLogEntry,
  detectScenarioLabel,
  formatRelativeTime,
} from "./utils";

const configuredMode = getConfiguredDemoMode();

const pipelineDefinitions: Array<{
  key: PipelineStepKey;
  label: string;
  description: string;
}> = [
  { key: "github", label: "GitHub", description: "Collecte des PR et des vulnérabilités" },
  { key: "analysis", label: "Analyse", description: "Consolidation du rapport métier" },
  { key: "decision", label: "Décision", description: "Règles superviseur explicables" },
  { key: "prompts", label: "Prompts", description: "Prompts structurés prêts à l’emploi" },
  { key: "codex", label: "Codex", description: "Réponses structurées sans exécution réelle" },
  { key: "validation", label: "Validation", description: "Revue humaine encore requise" },
  { key: "result", label: "Résultat", description: "Synthèse premium et données brutes" },
];

function createInitialPipelineStates(): Record<PipelineStepKey, PipelineStepState> {
  return {
    github: "idle",
    analysis: "idle",
    decision: "idle",
    prompts: "idle",
    codex: "idle",
    validation: "idle",
    result: "idle",
  };
}

function toPipelineSteps(
  states: Record<PipelineStepKey, PipelineStepState>,
): PipelineStep[] {
  return pipelineDefinitions.map((definition) => ({
    ...definition,
    state: states[definition.key],
  }));
}

function wait(milliseconds: number): Promise<void> {
  return new Promise((resolve) => window.setTimeout(resolve, milliseconds));
}

function reportTone(report: MaintenanceRunReport): PipelineStepState {
  const normalized = report.summary.status.toLowerCase();

  if (normalized === "success") {
    return "done";
  }

  if (normalized === "partial") {
    return "warning";
  }

  return "failed";
}

function githubTone(report: MaintenanceRunReport): PipelineStepState {
  const githubProblem = report.messages.logs.some((entry) =>
    entry.toLowerCase().includes("[github]") &&
    (entry.toLowerCase().includes("impossible") ||
      entry.toLowerCase().includes("absent") ||
      entry.toLowerCase().includes("indisponible")),
  );

  if (githubProblem) {
    return "failed";
  }

  if (report.vulnerabilities.status.toLowerCase() === "partial") {
    return "warning";
  }

  return "done";
}

export default function App() {
  const [status, setStatus] = useState<UiStatus>("idle");
  const [run, setRun] = useState<DemoRunState | null>(null);
  const [error, setError] = useState("");
  const [logs, setLogs] = useState<DeveloperLogEntry[]>([]);
  const [pipelineStates, setPipelineStates] = useState(createInitialPipelineStates);
  const [activeMode, setActiveMode] = useState<DemoMode>(configuredMode);
  const [deploymentStatus, setDeploymentStatus] = useState<UiStatus>("idle");
  const [deploymentResult, setDeploymentResult] = useState<DeploymentExecutionResult | null>(null);
  const [deploymentError, setDeploymentError] = useState("");

  function appendLog(entry: DeveloperLogEntry) {
    setLogs((current) => [...current, entry]);
  }

  function setStepState(step: PipelineStepKey, state: PipelineStepState) {
    setPipelineStates((current) => ({
      ...current,
      [step]: state,
    }));
  }

  async function animateStep(
    step: PipelineStepKey,
    finalState: PipelineStepState,
    delay = 180,
  ) {
    setStepState(step, "running");
    await wait(delay);
    setStepState(step, finalState);
  }

  async function executeMockScenario() {
    appendLog(createLogEntry("INFO", "ui", "Chargement d’un scénario mock réaliste."));
    const mockRun = getMockDemoRunState();

    await animateStep("github", "warning", 240);
    await animateStep("analysis", reportTone(mockRun.report), 200);
    await animateStep("decision", "done", 180);
    await animateStep("prompts", "done", 180);
    await animateStep("codex", "done", 220);
    await animateStep("validation", "warning", 120);
    await animateStep("result", "done", 120);

    setRun(mockRun);
    setLogs((current) => [
      ...current,
      ...createDerivedLogEntries("worker", mockRun.report.messages.logs),
      ...createDerivedLogEntries("worker", mockRun.report.messages.notes),
      createLogEntry("WARN", "supervisor", "Validation humaine encore requise avant toute exécution."),
      createLogEntry("INFO", "ui", "Le scénario mock a été chargé avec succès."),
    ]);
  }

  async function executeApiScenario() {
    appendLog(createLogEntry("INFO", "ui", "Déclenchement HTTP du worker RepoOps."));

    setStepState("github", "running");
    await wait(140);
    setStepState("analysis", "running");

    const report = await runMaintenanceReport();
    setStepState("github", githubTone(report));
    setStepState("analysis", reportTone(report));
    setLogs((current) => [
      ...current,
      ...createDerivedLogEntries("worker", report.messages.logs),
      ...createDerivedLogEntries("worker", report.messages.notes),
    ]);
    appendLog(
      createLogEntry(
        "INFO",
        "maintenance",
        `Rapport reçu avec le statut ${report.summary.status}.`,
      ),
    );

    setStepState("decision", "running");
    await wait(100);
    const decisions = await buildSupervisorDecisions(report);
    setStepState("decision", decisions.actions.length > 0 ? "done" : "warning");
    appendLog(
      createLogEntry(
        "INFO",
        "supervisor",
        `${decisions.summary.totalActions} action(s) structurée(s) produite(s).`,
      ),
    );

    setStepState("prompts", "running");
    await wait(100);
    const prompts = await buildSupervisorPrompts(decisions);
    setStepState("prompts", prompts.prompts.length > 0 ? "done" : "warning");
    appendLog(
      createLogEntry(
        "INFO",
        "prompts",
        `${prompts.summary.totalPrompts} prompt(s) généré(s).`,
      ),
    );

    setStepState("codex", "running");
    await wait(100);
    const codex = await executeCodexPrompts(prompts);
    setStepState("codex", codex.responses.length > 0 ? "done" : "warning");
    appendLog(
      createLogEntry(
        "INFO",
        "codex",
        `${codex.summary.totalResponses} réponse(s) structurée(s) reçue(s).`,
      ),
    );

    await animateStep("validation", "warning", 110);
    await animateStep("result", reportTone(report), 110);
    appendLog(
      createLogEntry(
        "WARN",
        "validation",
        "Le run est prêt pour une validation humaine, aucune exécution réelle n’a été lancée.",
      ),
    );

    setRun({
      source: "api",
      report,
      decisions,
      prompts,
      codex,
    });
  }

  async function executeScenario(mode: DemoMode) {
    setStatus("loading");
    setError("");
    setRun(null);
    setLogs([]);
    setPipelineStates(createInitialPipelineStates());
    setActiveMode(mode);

    try {
      if (mode === "mock") {
        await executeMockScenario();
      } else if (mode === "auto") {
        try {
          await executeApiScenario();
        } catch (apiError) {
          appendLog(
            createLogEntry(
              "WARN",
              "ui",
              "API indisponible, bascule automatique vers le scénario mock.",
            ),
          );
          await executeMockScenario();

          if (apiError instanceof Error) {
            setError(
              `L’API locale n’a pas répondu. La démo a basculé sur un exemple mock. Détail : ${apiError.message}`,
            );
          }
        }
      } else {
        await executeApiScenario();
      }

      setStatus("success");
    } catch (caughtError) {
      setStatus("error");
      setStepState("result", "failed");
      setError(
        caughtError instanceof Error
          ? caughtError.message
          : "Une erreur inconnue a empêché la démonstration de s’exécuter.",
      );
      appendLog(
        createLogEntry(
          "ERROR",
          "ui",
          caughtError instanceof Error
            ? caughtError.message
            : "Erreur inconnue lors du scénario de démonstration.",
        ),
      );
    }
  }

  async function executeDeployment(mode: DemoMode) {
    setDeploymentStatus("loading");
    setDeploymentError("");
    setDeploymentResult(null);

    try {
      let result: DeploymentExecutionResult;

      if (mode === "mock") {
        result = structuredClone(mockDeploymentExecutionResult);
      } else if (mode === "auto") {
        try {
          result = await runLocalDeployment();
        } catch (apiError) {
          appendLog(
            createLogEntry(
              "WARN",
              "deployment",
              "API de déploiement indisponible, bascule automatique vers le scénario mock.",
            ),
          );
          result = structuredClone(mockDeploymentExecutionResult);

          if (apiError instanceof Error) {
            setDeploymentError(
              `L’API de déploiement locale n’a pas répondu. La démonstration a basculé sur un exemple mock. Détail : ${apiError.message}`,
            );
          }
        }
      } else {
        result = await runLocalDeployment();
      }

      setDeploymentResult(result);
      setDeploymentStatus(result.status.toLowerCase() === "failed" ? "error" : "success");
      setLogs((current) => [
        ...current,
        ...createDerivedLogEntries("deployment", result.logs),
        ...createDerivedLogEntries("deployment", result.errors),
        createLogEntry(
          result.status.toLowerCase() === "failed" ? "ERROR" : "INFO",
          "deployment",
          result.summary,
        ),
      ]);
    } catch (caughtError) {
      setDeploymentStatus("error");
      setDeploymentError(
        caughtError instanceof Error
          ? caughtError.message
          : "Le déploiement local n’a pas pu être déclenché.",
      );
      appendLog(
        createLogEntry(
          "ERROR",
          "deployment",
          caughtError instanceof Error
            ? caughtError.message
            : "Erreur inconnue lors du déploiement local.",
        ),
      );
    }
  }

  const report = run?.report;
  const decisions = run?.decisions;
  const prompts = run?.prompts;
  const codex = run?.codex;

  const readyPullRequests =
    (report?.pullRequestStatuses.readyForReview.length ?? 0) +
    (report?.autoMerge.readyForMerge.length ?? 0);

  const blockedPullRequests =
    (report?.pullRequestStatuses.blocked.length ?? 0) +
    (report?.autoMerge.blockedPullRequests.length ?? 0);

  const analyzedPullRequests =
    report?.observability?.metrics.analyzedPullRequests ??
    report?.summary.counts.createdPullRequests ??
    0;

  const proposedActions = decisions?.summary.totalActions ?? 0;
  const scenarioLabel = report
    ? detectScenarioLabel(
        report.pullRequestStatuses.failedChecks.length,
        report.vulnerabilities.criticalCount,
        decisions?.actions.some((action) => action.isSecurityRelated) ?? false,
        report.autoMerge.readyForMerge.length,
        run?.source === "mock",
      )
    : "Scénario de démonstration prêt à être lancé";

  return (
    <div className="app-shell">
      <header className="app-header">
        <div className="app-header-container">
          <div className="app-brand">
            <div className="app-brand-mark">
              <LayoutDashboard className="size-5" />
            </div>
            <div className="app-brand-copy">
              <p className="app-brand-title">RepoOps Live Demo</p>
              <p className="app-brand-subtitle">Supervision et maintenance logicielle</p>
            </div>
          </div>

          <DemoModeBadge />
        </div>
      </header>

      <main className="app-page">
        <section className="page-toolbar section-enter">
          <div className="page-toolbar-heading">
            <p className="page-toolbar-kicker">Dashboard</p>
            <h1 className="page-toolbar-title">Centre de supervision RepoOps</h1>
            <p className="page-toolbar-description">
              Visualisez comment RepoOps agrège les signaux GitHub, prépare des décisions explicables et organise la relecture d’un run sans passer par les logs bruts.
            </p>
          </div>

          <div className="page-toolbar-meta">
            <div className="toolbar-metric">
              <span className="toolbar-metric-label">Scénario</span>
              <strong className="toolbar-metric-value">{scenarioLabel}</strong>
            </div>
            <div className="toolbar-metric">
              <span className="toolbar-metric-label">Dernière exécution</span>
              <strong className="toolbar-metric-value">
                {report ? formatRelativeTime(report.summary.runDateUtc) : "Aucun run"}
              </strong>
            </div>
            <div className="toolbar-metric">
              <span className="toolbar-metric-label">Mode</span>
              <strong className="toolbar-metric-value">Démonstration / dry-run</strong>
            </div>
          </div>
        </section>

        <HeroSection
          mode={activeMode}
          status={status}
          deploymentStatus={deploymentStatus}
          deploymentResult={deploymentResult}
          deploymentError={deploymentError}
          onRun={() => executeScenario(configuredMode)}
          onLoadMock={() => executeScenario("mock")}
          onDeploy={() => executeDeployment(configuredMode)}
        />

        {error ? (
          <Alert variant="danger" className="section-enter">
            <Activity className="size-4" />
            <div>
              <AlertTitle>Exécution interrompue</AlertTitle>
              <AlertDescription>{error}</AlertDescription>
            </div>
          </Alert>
        ) : null}

        {report ? <GlobalStatusBanner report={report} /> : null}

        {report && decisions && codex ? (
          <NarrativeSummary report={report} decisions={decisions} codex={codex} />
        ) : (
          <EmptyStatePanel />
        )}

        <KpiGrid
          analyzedPullRequests={analyzedPullRequests}
          readyPullRequests={readyPullRequests}
          blockedPullRequests={blockedPullRequests}
          vulnerabilities={report?.vulnerabilities.openAlerts ?? 0}
          proposedActions={proposedActions}
        />

        <div className="dashboard-grid">
          <div className="dashboard-main">
            <PipelineVisualizer steps={toPipelineSteps(pipelineStates)} />
            <DecisionSection actions={decisions?.actions ?? []} />
            <PromptSection prompts={prompts?.prompts ?? []} />
          </div>

          <div className="dashboard-side">
            {report && decisions && codex ? (
              <RunSummary report={report} decisions={decisions} codex={codex} />
            ) : (
              <Card className="section-enter">
                <CardHeader>
                  <CardHeading>
                    <div className="mb-2 flex items-center gap-2">
                      <Badge variant="neutral">Synthèse</Badge>
                      <Bot className="size-4 text-primary" />
                    </div>
                    <CardTitle>Vue d’ensemble du run</CardTitle>
                    <CardDescription>
                      La synthèse opérationnelle apparaîtra ici après le premier scénario.
                    </CardDescription>
                  </CardHeading>
                </CardHeader>
                <CardContent className="text-sm leading-6 text-muted-foreground">
                  Chargez un exemple pour voir immédiatement un run complet, puis utilisez l’analyse réelle pour tester la chaîne locale.
                </CardContent>
              </Card>
            )}

            <DeveloperPanel logs={logs} run={run} />
          </div>
        </div>
      </main>
    </div>
  );
}
