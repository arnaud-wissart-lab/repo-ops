import { useState } from "react";
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

  async function copyJson() {
    if (!run) {
      return;
    }

    await navigator.clipboard.writeText(toPrettyJson(run));
    setCopied(true);
    window.setTimeout(() => setCopied(false), 1500);
  }

  return (
    <section className="panel developer-panel">
      <div className="panel-header">
        <div>
          <p className="section-kicker">Panneau développeur</p>
          <h2>Logs et JSON brut</h2>
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
        <div className="developer-console">
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
      ) : (
        <div className="json-panel">
          <div className="json-toolbar">
            <span>Payload complet du scénario</span>
            <button
              type="button"
              className="secondary-button"
              onClick={() => void copyJson()}
              disabled={!run}
            >
              {copied ? "JSON copié" : "Copier"}
            </button>
          </div>
          <pre>{run ? toPrettyJson(run) : "Aucun JSON à afficher."}</pre>
        </div>
      )}
    </section>
  );
}
