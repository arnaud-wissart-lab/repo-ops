import type { DeveloperLogEntry, LogLevel } from "./types";

export function formatDateTime(value?: string): string {
  if (!value) {
    return "non disponible";
  }

  return new Date(value).toLocaleString("fr-FR", {
    dateStyle: "medium",
    timeStyle: "medium",
  });
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
