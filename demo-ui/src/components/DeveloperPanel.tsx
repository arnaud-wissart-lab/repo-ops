import { useEffect, useRef, useState } from "react";
import type { DeveloperLogEntry, DemoRunState } from "../types";
import { formatDateTime, toPrettyJson } from "../utils";

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
    <section className="panel developer-panel panel-reveal">
      <div className="panel-header">
        <div>
          <p className="section-kicker">Panneau développeur</p>
          <h2>Logs et JSON brut</h2>
          <p className="subtle-text">Sortie technique brute prête pour inspection ou copie.</p>
        </div>
        <div className="tab-bar">
          <button
            type="button"
            className={activeTab === "logs" ? "tab-button is-active" : "tab-button"}
            onClick={() => setActiveTab("logs")}
          >
            Logs
          </button>
          <button
            type="button"
            className={activeTab === "json" ? "tab-button is-active" : "tab-button"}
            onClick={() => setActiveTab("json")}
          >
            JSON brut
          </button>
        </div>
      </div>

      {activeTab === "logs" ? (
        <>
          <div className="developer-toolbar">
            <label className="toggle-option">
              <input
                type="checkbox"
                checked={autoScroll}
                onChange={(event) => setAutoScroll(event.target.checked)}
              />
              <span>Auto-scroll</span>
            </label>
          </div>
          <div ref={consoleRef} className="developer-console">
          {logs.length === 0 ? (
            <p className="empty-state">Aucun log disponible pour l’instant.</p>
          ) : (
            logs.map((log, index) => (
              <div key={`${log.timestamp}-${index}`} className={`log-line log-${log.level.toLowerCase()}`}>
                <span className="log-time">{formatDateTime(log.timestamp)}</span>
                <span className="log-level">{log.level}</span>
                <span className="log-source">{log.source}</span>
                <span className="log-message">{log.message}</span>
              </div>
            ))
          )}
          </div>
        </>
      ) : (
        <div className="json-panel">
          <div className="json-toolbar">
            <span>Sortie technique brute</span>
            <button
              type="button"
              className="secondary-button"
              onClick={() => void copyJson()}
              disabled={!run}
            >
              {copied ? "JSON copié" : "Copier"}
            </button>
          </div>
          <div className="json-editor">
            <div className="json-editor-gutter">
              {jsonLines.map((_, index) => (
                <span key={`line-${index + 1}`}>{index + 1}</span>
              ))}
            </div>
            <pre className="json-editor-content">{jsonContent}</pre>
          </div>
        </div>
      )}
    </section>
  );
}
