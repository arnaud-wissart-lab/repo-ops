import type {
  CodexExecutionResult,
  DeploymentExecutionResult,
  DemoRunState,
  GeneratedPromptResult,
  MaintenanceRunReport,
  SupervisorDecisionResult,
} from "../types";

const report: MaintenanceRunReport = {
  summary: {
    status: "Partial",
    mode: "daily-maintenance",
    inputSource: "demo-ui-mock",
    runDateUtc: "2026-03-26T08:30:00.000Z",
    scannedRepositories: ["owner/api-platform", "owner/web-portal", "owner/shared-kernel"],
    createdPullRequests: ["owner/api-platform#142", "owner/web-portal#87"],
    mergedPullRequests: ["owner/shared-kernel#56"],
    failedPullRequests: ["owner/api-platform#139"],
    remainingVulnerabilities: ["GHSA-xxxx-critical", "GHSA-yyyy-high"],
    counts: {
      scannedRepositories: 3,
      createdPullRequests: 2,
      mergedPullRequests: 1,
      failedPullRequests: 1,
      remainingVulnerabilities: 4,
      fixedVulnerabilities: 2,
    },
  },
  observability: {
    runId: "demo-20260326-083000",
    startedAtUtc: "2026-03-26T08:29:56.000Z",
    finishedAtUtc: "2026-03-26T08:30:07.000Z",
    durationMilliseconds: 11024,
    metrics: {
      analyzedPullRequests: 6,
      autoMergedPullRequests: 0,
      blockedPullRequests: 2,
      errorCount: 1,
    },
  },
  renovateExecution: {
    status: "PullRequestsUpdated",
    triggerRequested: false,
    includedFromLatestKnownExecution: true,
    startedAtUtc: "2026-03-26T08:26:00.000Z",
    finishedAtUtc: "2026-03-26T08:27:49.000Z",
    durationSeconds: 109,
    mode: "daily-report-last-known",
    command: "docker compose --profile maintenance run --rm renovate",
    exitCode: 0,
    summary:
      "Renovate a mis à jour deux pull requests et a signalé une dépendance critique nécessitant une revue prioritaire.",
    logs: [
      "INFO: Repository started owner/api-platform",
      "INFO: Branch updated renovate/serilog-4.x",
      "INFO: Pull request updated owner/api-platform#142",
    ],
    errors: [],
  },
  messages: {
    logs: [
      "[github] 3 dépôts inspectés avec succès.",
      "[github] 1 PR Renovate en échec détectée sur owner/api-platform#139.",
      "[security] 1 vulnérabilité critique ouverte sur owner/web-portal.",
      "[automerge] Dry-run actif, aucune fusion réelle tentée.",
    ],
    notes: [
      "Une PR patch est prête mais reste en attente de validation humaine finale.",
      "La collecte Dependabot est partielle sur un dépôt privé sans permission d'alerte étendue.",
    ],
  },
  recommendations: {
    manualActions: [
      "Relire en priorité owner/web-portal#87 car la PR corrige une vulnérabilité critique.",
      "Inspecter owner/api-platform#139 et corriger les checks GitHub avant nouvelle tentative.",
      "Valider la PR patch prête pour auto-merge avant de passer en mode réel sur le dépôt pilote.",
    ],
  },
  vulnerabilities: {
    status: "Partial",
    openAlerts: 4,
    fixedAlerts: 2,
    criticalCount: 1,
    highCount: 2,
    mediumCount: 1,
    lowCount: 0,
    prioritizedPullRequests: ["owner/web-portal#87"],
    importantAlerts: [
      "owner/web-portal : 1 alerte critique sur la chaîne d'authentification front.",
      "owner/api-platform : 2 alertes élevées encore ouvertes sur des dépendances de journalisation.",
    ],
    notes: [
      "Les alertes corrigées sont disponibles sur deux dépôts seulement dans ce jeu de démonstration.",
    ],
    repositories: [
      {
        repository: "owner/api-platform",
        status: "Available",
        openAlerts: 2,
        fixedAlerts: 1,
        criticalCount: 0,
        highCount: 2,
        mediumCount: 0,
        lowCount: 0,
      },
      {
        repository: "owner/web-portal",
        status: "Available",
        openAlerts: 2,
        fixedAlerts: 1,
        criticalCount: 1,
        highCount: 0,
        mediumCount: 1,
        lowCount: 0,
      },
      {
        repository: "owner/shared-kernel",
        status: "Unavailable",
        openAlerts: 0,
        fixedAlerts: 0,
        criticalCount: 0,
        highCount: 0,
        mediumCount: 0,
        lowCount: 0,
      },
    ],
  },
  pullRequestStatuses: {
    readyForReview: ["owner/api-platform#142", "owner/web-portal#87"],
    blocked: ["owner/shared-kernel#54"],
    failedChecks: ["owner/api-platform#139"],
    mergedRecently: ["owner/shared-kernel#56"],
    closedWithoutMerge: ["owner/api-platform#131"],
  },
  autoMerge: {
    enabled: true,
    dryRunEnabled: true,
    mergeMethod: "squash",
    allowedUpdateTypes: ["patch"],
    allowedMergeableStates: ["clean"],
    readyForMerge: ["owner/api-platform#142"],
    blockedPullRequests: ["owner/shared-kernel#54"],
    autoMergedPullRequests: [],
    manualReviewPullRequests: ["owner/web-portal#87"],
    failedPullRequests: ["owner/api-platform#139"],
    evaluations: [
      {
        repository: "owner/api-platform",
        number: 142,
        title: "chore(deps): update Serilog to 4.1.0",
        htmlUrl: "https://github.com/owner/api-platform/pull/142",
        versionType: "Patch",
        checksStatus: "Success",
        mergeable: true,
        mergeableState: "clean",
        decision: "AutoMerge",
        actionStatus: "DryRunReady",
        summary: "Patch éligible au dry-run d'auto-merge.",
        reasons: ["Checks verts", "Mise à jour patch", "Origine Renovate confirmée"],
        isSecurityUpdate: false,
        securitySeverity: "",
      },
      {
        repository: "owner/web-portal",
        number: 87,
        title: "fix(deps): update vite to 8.0.0",
        htmlUrl: "https://github.com/owner/web-portal/pull/87",
        versionType: "Minor",
        checksStatus: "Success",
        mergeable: true,
        mergeableState: "clean",
        decision: "ManualReview",
        actionStatus: "ReviewRequired",
        summary: "Correction de sécurité prioritaire nécessitant une revue manuelle.",
        reasons: ["Mise à jour minor", "PR liée à une vulnérabilité critique"],
        isSecurityUpdate: true,
        securitySeverity: "critical",
      },
    ],
  },
  digest: {
    subject: "[repo-ops] Synthèse maintenance du 2026-03-26",
    plainTextBody:
      "Synthèse premium de démonstration\n- 3 dépôts scannés\n- 6 PR analysées\n- 1 PR patch prête pour validation finale\n- 1 PR en échec nécessitant correction\n- 1 vulnérabilité critique ouverte à traiter en priorité",
    htmlBody: "",
  },
};

const decisions: SupervisorDecisionResult = {
  generatedAtUtc: "2026-03-26T08:30:08.000Z",
  sourceReportStatus: "Partial",
  summary: {
    totalActions: 4,
    reviewActions: 2,
    autoMergeEligibleActions: 1,
    fixRequiredActions: 1,
    ignoreActions: 0,
    highPriorityActions: 2,
  },
  actions: [
    {
      type: "AutoMergeEligible",
      repository: "owner/api-platform",
      pullRequestNumber: 142,
      pullRequestTitle: "chore(deps): update Serilog to 4.1.0",
      pullRequestUrl: "https://github.com/owner/api-platform/pull/142",
      checksStatus: "Success",
      priority: "Medium",
      reason:
        "La PR patch est prête, les checks sont verts et la politique d'auto-merge la classe comme éligible.",
      recommendation:
        "Effectuer une validation finale courte avant activation réelle sur le dépôt pilote.",
      isSecurityRelated: false,
      securitySeverity: "",
    },
    {
      type: "Review",
      repository: "owner/web-portal",
      pullRequestNumber: 87,
      pullRequestTitle: "fix(deps): update vite to 8.0.0",
      pullRequestUrl: "https://github.com/owner/web-portal/pull/87",
      checksStatus: "Success",
      priority: "High",
      reason:
        "La PR corrige une vulnérabilité critique mais reste une mise à jour minor, donc la revue humaine reste obligatoire.",
      recommendation:
        "Relire le diff et valider la compatibilité front avant toute fusion.",
      isSecurityRelated: true,
      securitySeverity: "critical",
    },
    {
      type: "FixRequired",
      repository: "owner/api-platform",
      pullRequestNumber: 139,
      pullRequestTitle: "chore(deps): update Microsoft.Extensions.*",
      pullRequestUrl: "https://github.com/owner/api-platform/pull/139",
      checksStatus: "Failed",
      priority: "High",
      reason:
        "Les checks GitHub sont en échec ; la PR ne doit pas être poussée plus loin tant que le build n'est pas stabilisé.",
      recommendation:
        "Analyser la casse de build et préparer une correction ciblée.",
      isSecurityRelated: false,
      securitySeverity: "",
    },
    {
      type: "Review",
      repository: "owner/shared-kernel",
      pullRequestNumber: 54,
      pullRequestTitle: "chore(deps): update xunit to 3.0.0",
      pullRequestUrl: "https://github.com/owner/shared-kernel/pull/54",
      checksStatus: "Pending",
      priority: "Medium",
      reason:
        "La PR est encore bloquée par des checks en attente et nécessite une revue après stabilisation.",
      recommendation:
        "Attendre la fin des checks puis requalifier la PR dans un prochain cycle.",
      isSecurityRelated: false,
      securitySeverity: "",
    },
  ],
  digest: {
    subject: "[repo-ops] Décisions superviseur du 2026-03-26",
    plainTextBody:
      "4 actions structurées produites : 1 auto-merge éligible, 2 revues, 1 correctif prioritaire.",
  },
  notes: [
    "Le moteur de décision reste déterministe et n'exécute aucune action.",
  ],
};

const prompts: GeneratedPromptResult = {
  generatedAtUtc: "2026-03-26T08:30:09.000Z",
  sourceReportStatus: "Partial",
  summary: {
    totalPrompts: 4,
    highPriorityPrompts: 2,
    reviewPrompts: 2,
    fixPrompts: 1,
    validationPrompts: 1,
  },
  prompts: [
    {
      actionType: "FixRequired",
      repository: "owner/api-platform",
      pullRequestNumber: 139,
      pullRequestTitle: "chore(deps): update Microsoft.Extensions.*",
      pullRequestUrl: "https://github.com/owner/api-platform/pull/139",
      priority: "High",
      promptType: "fix-required",
      promptText:
        "Contexte\n- Dépôt cible : owner/api-platform\n- PR cible : #139\n- Résumé du problème : les checks GitHub échouent après la mise à jour Renovate.\n\nObjectif\nIdentifier précisément la casse et proposer un correctif minimal.\n\nContraintes\n- Ne pas refondre le dépôt\n- Conserver la mise à jour de dépendance si possible\n- Produire un diff ciblé et testable\n\nSortie attendue\n- analyse de la cause\n- patch proposé\n- validations à exécuter",
      context: {
        problemSummary:
          "Les checks GitHub sont en échec après une mise à jour de dépendances .NET.",
        checksStatus: "Failed",
        recommendation:
          "Préparer une correction ciblée et relancer un build local avant nouvelle tentative.",
        isSecurityRelated: false,
        securitySeverity: "",
      },
    },
    {
      actionType: "Review",
      repository: "owner/web-portal",
      pullRequestNumber: 87,
      pullRequestTitle: "fix(deps): update vite to 8.0.0",
      pullRequestUrl: "https://github.com/owner/web-portal/pull/87",
      priority: "High",
      promptType: "vulnerability-priority",
      promptText:
        "Contexte\n- Dépôt cible : owner/web-portal\n- PR cible : #87\n- Cette PR corrige une vulnérabilité critique.\n\nObjectif\nRelire le diff, confirmer l'absence de régression front et formuler une recommandation de fusion.\n\nContraintes\n- Focus sur le risque de sécurité et l'impact build\n- Pas de changement hors périmètre\n\nSortie attendue\n- points de vigilance\n- recommandation claire\n- validations manuelles à exécuter",
      context: {
        problemSummary:
          "PR minor liée à une vulnérabilité critique sur la chaîne front.",
        checksStatus: "Success",
        recommendation:
          "Prioriser la revue et confirmer la compatibilité front avant fusion.",
        isSecurityRelated: true,
        securitySeverity: "critical",
      },
    },
    {
      actionType: "AutoMergeEligible",
      repository: "owner/api-platform",
      pullRequestNumber: 142,
      pullRequestTitle: "chore(deps): update Serilog to 4.1.0",
      pullRequestUrl: "https://github.com/owner/api-platform/pull/142",
      priority: "Medium",
      promptType: "auto-merge-eligible",
      promptText:
        "Contexte\n- Dépôt cible : owner/api-platform\n- PR cible : #142\n- Checks verts et patch éligible à l'auto-merge.\n\nObjectif\nEffectuer une validation finale courte avant activation réelle.\n\nContraintes\n- Vérifier qu'aucune rupture métier n'est visible\n- Confirmer la stabilité du diff\n\nSortie attendue\n- verdict de validation\n- risques résiduels éventuels",
      context: {
        problemSummary:
          "Patch Renovate prêt pour validation finale avant auto-merge contrôlé.",
        checksStatus: "Success",
        recommendation:
          "Confirmer l'absence de régression avant passage éventuel en mode réel.",
        isSecurityRelated: false,
        securitySeverity: "",
      },
    },
    {
      actionType: "Review",
      repository: "owner/shared-kernel",
      pullRequestNumber: 54,
      pullRequestTitle: "chore(deps): update xunit to 3.0.0",
      pullRequestUrl: "https://github.com/owner/shared-kernel/pull/54",
      priority: "Medium",
      promptType: "review",
      promptText:
        "Contexte\n- Dépôt cible : owner/shared-kernel\n- PR cible : #54\n- Les checks sont encore en attente.\n\nObjectif\nPréparer une grille de revue pour la PR lorsque les checks seront stabilisés.\n\nContraintes\n- Ne pas conclure tant que les checks ne sont pas terminés\n\nSortie attendue\n- checklist de revue\n- critères de requalification",
      context: {
        problemSummary:
          "PR encore bloquée par des checks en attente sur un package de test.",
        checksStatus: "Pending",
        recommendation:
          "Attendre la fin des checks puis relancer la qualification.",
        isSecurityRelated: false,
        securitySeverity: "",
      },
    },
  ],
  digest: {
    subject: "[repo-ops] Prompts superviseur du 2026-03-26",
    plainTextBody:
      "4 prompts prêts à l'emploi, dont 2 prioritaires et 1 dédié à un correctif urgent.",
  },
  notes: [
    "Les prompts restent déterministes et n'appellent aucun service externe.",
  ],
};

const codex: CodexExecutionResult = {
  generatedAtUtc: "2026-03-26T08:30:11.000Z",
  sourceReportStatus: "Partial",
  executorMode: "Stub",
  summary: {
    totalResponses: 4,
    analysisResponses: 2,
    proposedFixResponses: 1,
    refactorResponses: 1,
    highConfidenceResponses: 1,
    requiresHumanValidationResponses: 4,
  },
  responses: [
    {
      actionId: "owner-api-platform-139-fix-required",
      actionType: "FixRequired",
      repository: "owner/api-platform",
      pullRequestNumber: 139,
      pullRequestTitle: "chore(deps): update Microsoft.Extensions.*",
      pullRequestUrl: "https://github.com/owner/api-platform/pull/139",
      priority: "High",
      promptType: "fix-required",
      initialPromptText: prompts.prompts[0].promptText,
      responseText:
        "Réponse simulée : la casse semble localisée autour de la configuration d'injection de dépendances et du package Microsoft.Extensions.Hosting.",
      proposedUnifiedDiff: "",
      summary:
        "Proposition de correction ciblée à relire avant toute application dans un dépôt pilote.",
      responseType: "ProposedFix",
      confidenceLevel: "Medium",
      requiresHumanValidation: true,
      readyForExecution: false,
    },
    {
      actionId: "owner-web-portal-87-review",
      actionType: "Review",
      repository: "owner/web-portal",
      pullRequestNumber: 87,
      pullRequestTitle: "fix(deps): update vite to 8.0.0",
      pullRequestUrl: "https://github.com/owner/web-portal/pull/87",
      priority: "High",
      promptType: "vulnerability-priority",
      initialPromptText: prompts.prompts[1].promptText,
      responseText:
        "Réponse simulée : la PR semble saine, mais la chaîne de build front et les plugins Vite doivent être vérifiés avant toute fusion.",
      proposedUnifiedDiff: "",
      summary:
        "Analyse prioritaire de sécurité demandant une validation humaine finale.",
      responseType: "Analysis",
      confidenceLevel: "High",
      requiresHumanValidation: true,
      readyForExecution: false,
    },
  ],
  digest: {
    subject: "[repo-ops] Réponses superviseur du 2026-03-26",
    plainTextBody:
      "2 réponses structurées mises en avant dans ce scénario de démonstration, toutes en validation humaine obligatoire.",
  },
  notes: [
    "Le client Codex reste simulé dans l'interface de démonstration.",
  ],
};

export const mockDemoRunState: DemoRunState = {
  source: "mock",
  report,
  decisions,
  prompts,
  codex,
};

export const mockDeploymentExecutionResult: DeploymentExecutionResult = {
  status: "DryRun",
  requestedBy: "demo-ui-mock",
  targetName: "repoops.arnaudwissart.fr",
  verificationUrl: "https://repoops.arnaudwissart.fr",
  dryRunEnabled: true,
  startedAtUtc: "2026-03-26T08:30:12.000Z",
  finishedAtUtc: "2026-03-26T08:30:14.000Z",
  durationSeconds: 2.1,
  command: "powershell -NoProfile -ExecutionPolicy Bypass -File scripts/deploy-local.ps1 -DryRun",
  workingDirectory: "C:/Users/ArnaudW/source/repos/repo-ops",
  exitCode: 0,
  verificationStatus: "Skipped",
  verificationMessage:
    "La vérification publique de https://repoops.arnaudwissart.fr reste ignorée en dry-run.",
  summary:
    "Le déploiement a été simulé avec succès pour repoops.arnaudwissart.fr. La stack Docker Compose aurait été reconstruite puis relancée.",
  logs: [
    "[deploy] Cible : machine locale",
    "[deploy] Répertoire : C:/Users/ArnaudW/source/repos/repo-ops",
    "[deploy] Mode dry-run actif.",
    "[deploy] Vérification attendue : docker compose config",
    "[deploy] Action attendue : docker compose up -d --build",
  ],
  errors: [],
};
