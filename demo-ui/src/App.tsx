import { useState } from "react";
import {
  buildSupervisorDecisions,
  buildSupervisorPrompts,
  executeCodexPrompts,
  getConfiguredDemoMode,
  getMockDemoRunState,
  runMaintenanceReport,
} from "./api";
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
import { StatusPill } from "./components/StatusPill";
import type {
  DemoMode,
  DemoRunState,
  DeveloperLogEntry,
  MaintenanceRunReport,
  PipelineStep,
  PipelineStepKey,
  PipelineStepState,
  UiStatus,
} from "./types";
import { createDerivedLogEntries, createLogEntry, formatDateTime } from "./utils";

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

function statusTone(status: UiStatus): "neutral" | "done" | "warning" | "failed" {
  if (status === "success") {
    return "done";
  }

  if (status === "loading") {
    return "warning";
  }

  if (status === "error") {
    return "failed";
  }

  return "neutral";
}

export default function App() {
  const [status, setStatus] = useState<UiStatus>("idle");
  const [run, setRun] = useState<DemoRunState | null>(null);
  const [error, setError] = useState("");
  const [logs, setLogs] = useState<DeveloperLogEntry[]>([]);
  const [pipelineStates, setPipelineStates] = useState(createInitialPipelineStates);
  const [activeMode, setActiveMode] = useState<DemoMode>(configuredMode);

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

  return (
    <div className="app-shell">
      <div className="ambient ambient-left" />
      <div className="ambient ambient-right" />
      <DemoModeBadge />

      <main className="layout">
        <HeroSection
          mode={activeMode}
          status={status}
          onRun={() => executeScenario(configuredMode)}
          onLoadMock={() => executeScenario("mock")}
        />

        <section className="top-strip">
          <div className="status-card">
            <div>
              <p className="section-kicker">État du run</p>
              <h2>Statut d’exécution</h2>
            </div>
            <StatusPill
              label={
                status === "idle"
                  ? "Prêt"
                  : status === "loading"
                    ? "Exécution"
                    : status === "success"
                      ? "Terminé"
                      : "Erreur"
              }
              tone={statusTone(status)}
            />
            {error ? <p className="error-text">{error}</p> : null}
            {report ? (
              <p className="subtle-text">
                Dernier run : {formatDateTime(report.summary.runDateUtc)} · source{" "}
                {run?.source === "mock" ? "mock" : "API"}
              </p>
            ) : (
              <p className="subtle-text">
                Lancez un scénario API ou chargez un exemple pour explorer le
                cockpit.
              </p>
            )}
          </div>

          <div className="status-card">
            <div>
              <p className="section-kicker">Sécurité</p>
              <h2>Cadre de démonstration</h2>
            </div>
            <ul className="detail-list">
              <li>Dry-run conservé sur les mécanismes sensibles.</li>
              <li>Aucune opération Git réelle depuis l’interface.</li>
              <li>La validation humaine reste hors de portée de la page.</li>
            </ul>
          </div>
        </section>

        {report ? <GlobalStatusBanner report={report} /> : null}

        {report && decisions && codex ? (
          <NarrativeSummary report={report} decisions={decisions} codex={codex} />
        ) : (
          <EmptyStatePanel />
        )}

        <PipelineVisualizer steps={toPipelineSteps(pipelineStates)} />

        <KpiGrid
          analyzedPullRequests={analyzedPullRequests}
          readyPullRequests={readyPullRequests}
          blockedPullRequests={blockedPullRequests}
          vulnerabilities={report?.vulnerabilities.openAlerts ?? 0}
          proposedActions={proposedActions}
        />

        <div className="content-grid">
          <div className="content-column">
            <DecisionSection actions={decisions?.actions ?? []} />
            {report && decisions && codex ? (
              <RunSummary report={report} decisions={decisions} codex={codex} />
            ) : (
              <section className="panel panel-reveal">
                <div className="panel-header">
                  <div>
                    <p className="section-kicker">Synthèse</p>
                    <h2>Résumé exécutif du run</h2>
                  </div>
                </div>
                <p className="empty-state">
                  Lancez une analyse ou chargez un exemple pour afficher la
                  synthèse consolidée, les messages importants et les réponses
                  proposées, ainsi que la narration du run.
                </p>
              </section>
            )}
          </div>

          <div className="content-column">
            <PromptSection prompts={prompts?.prompts ?? []} />
            <DeveloperPanel logs={logs} run={run} />
          </div>
        </div>
      </main>
    </div>
  );
}
