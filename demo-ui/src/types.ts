export type UiStatus = "idle" | "loading" | "success" | "error";

export type DemoMode = "api" | "mock" | "auto";
export type ThemePreference = "light" | "dark" | "auto";
export type ResolvedTheme = "light" | "dark";

export type PipelineStepKey =
  | "github"
  | "analysis"
  | "decision"
  | "prompts"
  | "codex"
  | "validation"
  | "result";

export type PipelineStepState =
  | "idle"
  | "running"
  | "done"
  | "warning"
  | "failed";

export type LogLevel = "INFO" | "WARN" | "ERROR";

export interface DeveloperLogEntry {
  timestamp: string;
  level: LogLevel;
  source: string;
  message: string;
}

export interface PipelineStep {
  key: PipelineStepKey;
  label: string;
  description: string;
  state: PipelineStepState;
}

export interface MaintenanceRunReport {
  summary: {
    status: string;
    mode: string;
    inputSource: string;
    runDateUtc: string;
    scannedRepositories?: string[];
    createdPullRequests?: string[];
    mergedPullRequests?: string[];
    failedPullRequests?: string[];
    remainingVulnerabilities?: string[];
    counts: {
      scannedRepositories: number;
      createdPullRequests: number;
      mergedPullRequests: number;
      failedPullRequests: number;
      remainingVulnerabilities: number;
      fixedVulnerabilities: number;
    };
  };
  observability?: {
    runId: string;
    startedAtUtc?: string;
    finishedAtUtc?: string;
    durationMilliseconds: number;
    metrics: {
      analyzedPullRequests: number;
      autoMergedPullRequests: number;
      blockedPullRequests: number;
      errorCount: number;
    };
  };
  renovateExecution?: {
    status: string;
    triggerRequested: boolean;
    includedFromLatestKnownExecution: boolean;
    startedAtUtc?: string;
    finishedAtUtc?: string;
    durationSeconds?: number;
    mode: string;
    command: string;
    exitCode?: number;
    summary: string;
    logs: string[];
    errors: string[];
  };
  messages: {
    logs: string[];
    notes: string[];
  };
  recommendations: {
    manualActions: string[];
  };
  vulnerabilities: {
    status: string;
    openAlerts: number;
    fixedAlerts: number;
    criticalCount: number;
    highCount: number;
    mediumCount: number;
    lowCount: number;
    prioritizedPullRequests: string[];
    importantAlerts: string[];
    notes: string[];
    repositories: Array<{
      repository: string;
      status: string;
      openAlerts: number;
      fixedAlerts: number;
      criticalCount: number;
      highCount: number;
      mediumCount: number;
      lowCount: number;
    }>;
  };
  pullRequestStatuses: {
    readyForReview: string[];
    blocked: string[];
    failedChecks: string[];
    mergedRecently: string[];
    closedWithoutMerge: string[];
  };
  autoMerge: {
    enabled: boolean;
    dryRunEnabled: boolean;
    mergeMethod: string;
    allowedUpdateTypes: string[];
    allowedMergeableStates: string[];
    readyForMerge: string[];
    blockedPullRequests: string[];
    autoMergedPullRequests: string[];
    manualReviewPullRequests: string[];
    failedPullRequests: string[];
    evaluations: Array<{
      repository: string;
      number?: number;
      title?: string;
      htmlUrl?: string;
      versionType?: string;
      checksStatus?: string;
      mergeable?: boolean;
      mergeableState?: string;
      decision?: string;
      actionStatus?: string;
      summary?: string;
      reasons?: string[];
      isSecurityUpdate?: boolean;
      securitySeverity?: string;
    }>;
  };
  digest: {
    subject: string;
    plainTextBody: string;
    htmlBody?: string;
  };
}

export interface SupervisorDecisionResult {
  generatedAtUtc?: string;
  sourceReportStatus: string;
  summary: {
    totalActions: number;
    reviewActions: number;
    autoMergeEligibleActions: number;
    fixRequiredActions: number;
    ignoreActions: number;
    highPriorityActions: number;
  };
  actions: SupervisorAction[];
  digest?: {
    subject: string;
    plainTextBody: string;
  };
  notes?: string[];
}

export interface SupervisorAction {
  type: string;
  repository: string;
  pullRequestNumber?: number;
  pullRequestTitle?: string;
  pullRequestUrl?: string;
  checksStatus?: string;
  priority: string;
  reason: string;
  recommendation?: string;
  isSecurityRelated?: boolean;
  securitySeverity?: string;
}

export interface GeneratedPromptResult {
  generatedAtUtc?: string;
  sourceReportStatus: string;
  summary: {
    totalPrompts: number;
    highPriorityPrompts: number;
    reviewPrompts: number;
    fixPrompts: number;
    validationPrompts: number;
  };
  prompts: GeneratedPrompt[];
  digest?: {
    subject: string;
    plainTextBody: string;
  };
  notes?: string[];
}

export interface GeneratedPrompt {
  actionType: string;
  repository: string;
  pullRequestNumber?: number;
  pullRequestTitle?: string;
  pullRequestUrl?: string;
  priority: string;
  promptType: string;
  promptText: string;
  context: {
    problemSummary: string;
    checksStatus: string;
    recommendation: string;
    isSecurityRelated: boolean;
    securitySeverity: string;
  };
}

export interface CodexExecutionResult {
  generatedAtUtc?: string;
  sourceReportStatus: string;
  executorMode: string;
  summary: {
    totalResponses: number;
    analysisResponses: number;
    proposedFixResponses: number;
    refactorResponses: number;
    highConfidenceResponses: number;
    requiresHumanValidationResponses: number;
  };
  responses: CodexExecutionResponse[];
  digest?: {
    subject: string;
    plainTextBody: string;
  };
  notes?: string[];
}

export interface CodexExecutionResponse {
  actionId: string;
  actionType: string;
  repository: string;
  pullRequestNumber?: number;
  pullRequestTitle?: string;
  pullRequestUrl?: string;
  priority: string;
  promptType: string;
  initialPromptText: string;
  responseText: string;
  proposedUnifiedDiff?: string;
  summary: string;
  responseType: string;
  confidenceLevel: string;
  requiresHumanValidation: boolean;
  readyForExecution: boolean;
}

export interface DemoRunState {
  source: "api" | "mock";
  report: MaintenanceRunReport;
  decisions: SupervisorDecisionResult;
  prompts: GeneratedPromptResult;
  codex: CodexExecutionResult;
}

export interface DeploymentExecutionResult {
  status: string;
  requestedBy: string;
  targetName: string;
  verificationUrl: string;
  dryRunEnabled: boolean;
  startedAtUtc?: string;
  finishedAtUtc?: string;
  durationSeconds?: number;
  command: string;
  workingDirectory: string;
  exitCode?: number;
  verificationStatus: string;
  verificationMessage: string;
  summary: string;
  logs: string[];
  errors: string[];
}
