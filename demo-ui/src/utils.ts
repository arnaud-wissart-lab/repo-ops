import type { DeveloperLogEntry, LogLevel } from "./types";

export const demoRepositorySlug = "arnaud-wissart/repoops-demo-weather-station";
export const demoRepositoryUrl = `https://github.com/${demoRepositorySlug}`;

export function formatDateTime(value?: string): string {
  if (!value) {
    return "non disponible";
  }

  return new Date(value).toLocaleString("fr-FR", {
    dateStyle: "medium",
    timeStyle: "medium",
  });
}

export function formatRelativeTime(value?: string): string {
  if (!value) {
    return "non disponible";
  }

  const target = new Date(value).getTime();
  const deltaSeconds = Math.max(0, Math.round((Date.now() - target) / 1000));

  if (deltaSeconds < 5) {
    return "à l’instant";
  }

  if (deltaSeconds < 60) {
    return `il y a ${deltaSeconds} s`;
  }

  const minutes = Math.round(deltaSeconds / 60);
  if (minutes < 60) {
    return `il y a ${minutes} min`;
  }

  const hours = Math.round(minutes / 60);
  return `il y a ${hours} h`;
}

export function formatDuration(milliseconds?: number): string {
  if (typeof milliseconds !== "number" || Number.isNaN(milliseconds)) {
    return "non disponible";
  }

  if (milliseconds < 1000) {
    return `${milliseconds} ms`;
  }

  return `${(milliseconds / 1000).toFixed(1)} s`;
}

export function detectScenarioLabel(
  failedChecks: number,
  criticalVulnerabilities: number,
  securityReviewExists: boolean,
  readyForMerge: number,
): string {
  if (criticalVulnerabilities > 0 && securityReviewExists) {
    return "Correction de vulnérabilité critique avec revue prioritaire";
  }

  if (failedChecks > 0) {
    return "Mise à jour de dépendance avec build cassé";
  }

  if (readyForMerge > 0) {
    return "Patch de dépendance prêt pour validation finale";
  }

  return "Cycle de maintenance réel sur le dépôt de démonstration";
}

export function resolvePrimaryRepository(scannedRepositories?: string[]): string {
  const firstRepository = scannedRepositories?.find((repository) => repository.trim().length > 0);
  return firstRepository ?? demoRepositorySlug;
}

export function toRepositoryUrl(repository: string): string {
  return `https://github.com/${repository}`;
}

export function truncate(text: string, length = 220): string {
  if (text.length <= length) {
    return text;
  }

  return `${text.slice(0, length).trim()}...`;
}

export function toPrettyJson(value: unknown): string {
  return JSON.stringify(value, null, 2);
}

export function detectLogLevel(message: string): LogLevel {
  const normalized = message.toLowerCase();

  if (
    normalized.includes("erreur") ||
    normalized.includes("error") ||
    normalized.includes("failed") ||
    normalized.includes("exception")
  ) {
    return "ERROR";
  }

  if (
    normalized.includes("warning") ||
    normalized.includes("warn") ||
    normalized.includes("indisponible") ||
    normalized.includes("absent")
  ) {
    return "WARN";
  }

  return "INFO";
}

export function createLogEntry(
  level: LogLevel,
  source: string,
  message: string,
): DeveloperLogEntry {
  return {
    timestamp: new Date().toISOString(),
    level,
    source,
    message,
  };
}

export function createDerivedLogEntries(
  source: string,
  lines: string[],
): DeveloperLogEntry[] {
  return lines.map((line) => ({
    timestamp: new Date().toISOString(),
    level: detectLogLevel(line),
    source,
    message: line,
  }));
}
