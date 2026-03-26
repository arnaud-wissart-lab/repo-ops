import { mockDemoRunState } from "./mocks/demoData";
import type {
  DeploymentExecutionResult,
  CodexExecutionResult,
  DemoMode,
  DemoRunState,
  GeneratedPromptResult,
  MaintenanceRunReport,
  SupervisorDecisionResult,
} from "./types";

interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
}

interface RunMaintenancePayload {
  inputSource: string;
  triggerRenovateExecution: boolean;
}

const headers = {
  "Content-Type": "application/json",
};

const defaultTimeoutMs = Number(import.meta.env.VITE_DEMO_API_TIMEOUT_MS ?? 30000);
const configuredMode = normalizeDemoMode(import.meta.env.VITE_DEMO_MODE);

function normalizeDemoMode(value?: string): DemoMode {
  if (value === "mock" || value === "auto") {
    return value;
  }

  return "api";
}

async function postJson<TRequest, TResponse>(
  path: string,
  payload: TRequest,
  timeoutMs = defaultTimeoutMs,
): Promise<TResponse> {
  const controller = new AbortController();
  const timeoutHandle = window.setTimeout(() => controller.abort(), timeoutMs);

  try {
    const response = await fetch(path, {
      method: "POST",
      headers,
      body: JSON.stringify(payload),
      signal: controller.signal,
    });

    const text = await response.text();
    const data = text ? JSON.parse(text) : null;

    if (data && typeof data === "object" && "summary" in data) {
      return data as TResponse;
    }

    const problem = data as ProblemDetails | null;
    const detail =
      problem?.detail ||
      problem?.title ||
      `L'appel ${path} a retourné le statut HTTP ${response.status}.`;

    throw new Error(detail);
  } catch (error) {
    if (error instanceof DOMException && error.name === "AbortError") {
      throw new Error(
        `Le délai d'attente de ${timeoutMs} ms a été dépassé pour ${path}.`,
      );
    }

    throw error;
  } finally {
    window.clearTimeout(timeoutHandle);
  }
}

function cloneMockState(): DemoRunState {
  return JSON.parse(JSON.stringify(mockDemoRunState)) as DemoRunState;
}

export function getConfiguredDemoMode(): DemoMode {
  return configuredMode;
}

export function getMockDemoRunState(): DemoRunState {
  return cloneMockState();
}

export async function runMaintenanceReport(): Promise<MaintenanceRunReport> {
  return postJson<RunMaintenancePayload, MaintenanceRunReport>("/maintenance/run", {
    inputSource: "demo-ui",
    triggerRenovateExecution: false,
  });
}

export async function buildSupervisorDecisions(
  report: MaintenanceRunReport,
): Promise<SupervisorDecisionResult> {
  return postJson<MaintenanceRunReport, SupervisorDecisionResult>(
    "/supervisor/decisions",
    report,
  );
}

export async function buildSupervisorPrompts(
  decisions: SupervisorDecisionResult,
): Promise<GeneratedPromptResult> {
  return postJson<SupervisorDecisionResult, GeneratedPromptResult>(
    "/supervisor/prompts",
    decisions,
  );
}

export async function executeCodexPrompts(
  prompts: GeneratedPromptResult,
): Promise<CodexExecutionResult> {
  return postJson<GeneratedPromptResult, CodexExecutionResult>(
    "/supervisor/codex/execute",
    prompts,
  );
}

export async function runLocalDeployment(): Promise<DeploymentExecutionResult> {
  return postJson<{ requestedBy: string }, DeploymentExecutionResult>("/deployment/run", {
    requestedBy: "demo-ui",
  });
}
