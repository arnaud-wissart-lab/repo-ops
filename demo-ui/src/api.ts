import type {
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

const headers = {
  "Content-Type": "application/json",
};

async function postJson<TRequest, TResponse>(
  path: string,
  payload: TRequest,
): Promise<TResponse> {
  const response = await fetch(path, {
    method: "POST",
    headers,
    body: JSON.stringify(payload),
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
}

export async function runDemoAnalysis(): Promise<DemoRunState> {
  const report = await postJson<
    { inputSource: string; triggerRenovateExecution: boolean },
    MaintenanceRunReport
  >("/maintenance/run", {
    inputSource: "demo-ui",
    triggerRenovateExecution: false,
  });

  const decisions = await postJson<MaintenanceRunReport, SupervisorDecisionResult>(
    "/supervisor/decisions",
    report,
  );

  const prompts = await postJson<SupervisorDecisionResult, GeneratedPromptResult>(
    "/supervisor/prompts",
    decisions,
  );

  return {
    report,
    decisions,
    prompts,
  };
}
