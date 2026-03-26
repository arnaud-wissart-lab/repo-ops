export interface MaintenanceRunReport {
  summary: {
    status: string;
    mode: string;
    inputSource: string;
    runDateUtc: string;
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
    durationMilliseconds: number;
    metrics: {
      analyzedPullRequests: number;
      autoMergedPullRequests: number;
      blockedPullRequests: number;
      errorCount: number;
    };
  };
  recommendations: {
    manualActions: string[];
  };
  vulnerabilities: {
    status: string;
    criticalCount: number;
    highCount: number;
    mediumCount: number;
    lowCount: number;
    importantAlerts: string[];
  };
  autoMerge: {
    readyForMerge: string[];
    blockedPullRequests: string[];
    autoMergedPullRequests: string[];
    manualReviewPullRequests: string[];
  };
}

export interface SupervisorDecisionResult {
  summary: {
    totalActions: number;
    reviewActions: number;
    autoMergeEligibleActions: number;
    fixRequiredActions: number;
    ignoreActions: number;
    highPriorityActions: number;
  };
  actions: SupervisorAction[];
}

export interface SupervisorAction {
  type: string;
  repository: string;
  pullRequestNumber?: number;
  pullRequestTitle?: string;
  pullRequestUrl?: string;
  priority: string;
  reason: string;
}

export interface GeneratedPromptResult {
  summary: {
    totalPrompts: number;
    highPriorityPrompts: number;
    reviewPrompts: number;
    fixPrompts: number;
    validationPrompts: number;
  };
  prompts: GeneratedPrompt[];
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

export interface DemoRunState {
  report: MaintenanceRunReport;
  decisions: SupervisorDecisionResult;
  prompts: GeneratedPromptResult;
}
