import { useState } from "react";
import { runDemoAnalysis } from "./api";
import type { DemoRunState, GeneratedPrompt, SupervisorAction } from "./types";

type UiStatus = "idle" | "loading" | "success" | "error";

function formatDate(value: string): string {
  return new Date(value).toLocaleString("fr-FR", {
    dateStyle: "medium",
    timeStyle: "medium",
  });
}

function truncate(text: string, length = 220): string {
  if (text.length <= length) {
    return text;
  }

  return `${text.slice(0, length).trim()}...`;
}

function StatusBadge({ status }: { status: string }) {
  const normalized = status.toLowerCase();
  const className =
    normalized === "success"
      ? "badge badge-success"
      : normalized === "partial"
        ? "badge badge-warning"
        : "badge badge-error";

  return <span className={className}>{status}</span>;
}

function DecisionCard({ action }: { action: SupervisorAction }) {
  return (
    <article className="card decision-card">
      <div className="card-header">
        <div>
          <p className="eyebrow">{action.type}</p>
          <h3>{action.repository}</h3>
        </div>
        <span className="badge badge-neutral">{action.priority}</span>
      </div>
      <p className="muted">
        {action.pullRequestNumber
          ? `PR #${action.pullRequestNumber}`
          : "Action au niveau dépôt"}
      </p>
      <p>{action.reason}</p>
      {action.pullRequestUrl ? (
        <a href={action.pullRequestUrl} target="_blank" rel="noreferrer">
          Ouvrir la pull request source
        </a>
      ) : null}
    </article>
  );
}

function PromptCard({ prompt }: { prompt: GeneratedPrompt }) {
  const [copied, setCopied] = useState(false);

  async function handleCopy(): Promise<void> {
    await navigator.clipboard.writeText(prompt.promptText);
    setCopied(true);
    window.setTimeout(() => setCopied(false), 1500);
  }

  return (
    <article className="card prompt-card">
      <div className="card-header">
        <div>
          <p className="eyebrow">{prompt.promptType}</p>
          <h3>{prompt.repository}</h3>
        </div>
        <span className="badge badge-neutral">{prompt.priority}</span>
      </div>
      <p className="muted">
        {prompt.pullRequestNumber
          ? `PR #${prompt.pullRequestNumber}`
          : "Prompt au niveau dépôt"}
      </p>
      <p>{prompt.context.problemSummary}</p>
      <div className="prompt-meta">
        <span>Checks : {prompt.context.checksStatus || "non précisé"}</span>
        <span>
          Sécurité :{" "}
          {prompt.context.isSecurityRelated
            ? prompt.context.securitySeverity || "oui"
            : "non"}
        </span>
      </div>
      <pre>{truncate(prompt.promptText, 360)}</pre>
      <button type="button" className="secondary-button" onClick={handleCopy}>
        {copied ? "Prompt copié" : "Copier le prompt"}
      </button>
    </article>
  );
}

export default function App() {
  const [status, setStatus] = useState<UiStatus>("idle");
  const [run, setRun] = useState<DemoRunState | null>(null);
  const [error, setError] = useState<string>("");

  async function handleRun(): Promise<void> {
    setStatus("loading");
    setError("");

    try {
      const result = await runDemoAnalysis();
      setRun(result);
      setStatus("success");
    } catch (caughtError) {
      setRun(null);
      setStatus("error");
      setError(
        caughtError instanceof Error
          ? caughtError.message
          : "Une erreur inconnue a empêché la démonstration de s'exécuter.",
      );
    }
  }

  const report = run?.report;
  const decisions = run?.decisions;
  const prompts = run?.prompts;

  return (
    <div className="page-shell">
      <header className="hero">
        <div className="hero-copy">
          <p className="eyebrow">repo-ops</p>
          <h1>Maintenance logicielle supervisée en mode démonstration</h1>
          <p className="hero-description">
            Cette interface déclenche un run sec du worker, calcule les
            décisions, génère les prompts et expose le résultat sans créer de
            commit, sans push et sans action irréversible.
          </p>
        </div>
        <div className="hero-actions">
          <button
            type="button"
            className="primary-button"
            onClick={handleRun}
            disabled={status === "loading"}
          >
            {status === "loading" ? "Analyse en cours..." : "Lancer une analyse"}
          </button>
          <p className="hint">
            Flux appelé : maintenance → décisions → prompts
          </p>
        </div>
      </header>

      <section className="grid two-columns">
        <article className="card">
          <h2>Mode démonstration</h2>
          <p>
            La page appelle uniquement les endpoints déjà disponibles du worker.
            Aucun merge réel, aucun commit et aucune pull request ne sont créés
            depuis cette interface.
          </p>
          <ul>
            <li>Renovate n'est pas relancé automatiquement.</li>
            <li>Le worker reste la source de vérité métier.</li>
            <li>La démonstration se limite au reporting et à la décision.</li>
          </ul>
        </article>

        <article className="card">
          <h2>État courant</h2>
          {status === "idle" ? (
            <p>Aucune analyse n'a encore été lancée depuis cette interface.</p>
          ) : null}
          {status === "loading" ? (
            <p>Le worker exécute le cycle complet et consolide les sorties.</p>
          ) : null}
          {status === "error" ? <p className="error-text">{error}</p> : null}
          {status === "success" && report ? (
            <div className="status-block">
              <StatusBadge status={report.summary.status} />
              <p>
                Run du {formatDate(report.summary.runDateUtc)} via{" "}
                <strong>{report.summary.inputSource}</strong>
              </p>
              {report.observability ? (
                <p className="muted">
                  RunId : {report.observability.runId} · durée :{" "}
                  {report.observability.durationMilliseconds} ms
                </p>
              ) : null}
            </div>
          ) : null}
        </article>
      </section>

      {report ? (
        <>
          <section className="card">
            <h2>Résumé global</h2>
            <div className="metrics-grid">
              <div className="metric">
                <span>Dépôts scannés</span>
                <strong>{report.summary.counts.scannedRepositories}</strong>
              </div>
              <div className="metric">
                <span>PR créées</span>
                <strong>{report.summary.counts.createdPullRequests}</strong>
              </div>
              <div className="metric">
                <span>PR fusionnées</span>
                <strong>{report.summary.counts.mergedPullRequests}</strong>
              </div>
              <div className="metric">
                <span>PR en échec</span>
                <strong>{report.summary.counts.failedPullRequests}</strong>
              </div>
              <div className="metric">
                <span>Vulnérabilités restantes</span>
                <strong>{report.summary.counts.remainingVulnerabilities}</strong>
              </div>
              <div className="metric">
                <span>Vulnérabilités corrigées</span>
                <strong>{report.summary.counts.fixedVulnerabilities}</strong>
              </div>
            </div>
          </section>

          <section className="grid two-columns">
            <article className="card">
              <h2>Actions proposées</h2>
              {report.recommendations.manualActions.length === 0 ? (
                <p>Aucune action manuelle supplémentaire n'est proposée.</p>
              ) : (
                <ul>
                  {report.recommendations.manualActions.map((action) => (
                    <li key={action}>{action}</li>
                  ))}
                </ul>
              )}
            </article>

            <article className="card">
              <h2>Sécurité</h2>
              <div className="security-grid">
                <div>
                  <span>Statut</span>
                  <strong>{report.vulnerabilities.status}</strong>
                </div>
                <div>
                  <span>Critiques</span>
                  <strong>{report.vulnerabilities.criticalCount}</strong>
                </div>
                <div>
                  <span>Élevées</span>
                  <strong>{report.vulnerabilities.highCount}</strong>
                </div>
                <div>
                  <span>Moyennes</span>
                  <strong>{report.vulnerabilities.mediumCount}</strong>
                </div>
              </div>
              {report.vulnerabilities.importantAlerts.length > 0 ? (
                <ul>
                  {report.vulnerabilities.importantAlerts.map((alert) => (
                    <li key={alert}>{alert}</li>
                  ))}
                </ul>
              ) : (
                <p>Aucune alerte critique ou élevée visible dans ce run.</p>
              )}
            </article>
          </section>
        </>
      ) : null}

      <section className="section-header">
        <div>
          <p className="eyebrow">Supervision</p>
          <h2>Décisions et prompts</h2>
        </div>
      </section>

      <section className="grid two-columns">
        <article className="card">
          <h2>Décisions du superviseur</h2>
          {decisions ? (
            <>
              <div className="metrics-grid compact">
                <div className="metric">
                  <span>Total</span>
                  <strong>{decisions.summary.totalActions}</strong>
                </div>
                <div className="metric">
                  <span>Revue</span>
                  <strong>{decisions.summary.reviewActions}</strong>
                </div>
                <div className="metric">
                  <span>Correctifs</span>
                  <strong>{decisions.summary.fixRequiredActions}</strong>
                </div>
                <div className="metric">
                  <span>Auto-merge éligible</span>
                  <strong>{decisions.summary.autoMergeEligibleActions}</strong>
                </div>
              </div>
              <div className="stack">
                {decisions.actions.length === 0 ? (
                  <p>Aucune action structurée n'a été produite pour ce run.</p>
                ) : (
                  decisions.actions.map((action) => (
                    <DecisionCard
                      key={`${action.repository}-${action.pullRequestNumber ?? "repo"}-${action.type}`}
                      action={action}
                    />
                  ))
                )}
              </div>
            </>
          ) : (
            <p>Lancez une analyse pour afficher les décisions.</p>
          )}
        </article>

        <article className="card">
          <h2>Prompts prêts à l'emploi</h2>
          {prompts ? (
            <>
              <div className="metrics-grid compact">
                <div className="metric">
                  <span>Total</span>
                  <strong>{prompts.summary.totalPrompts}</strong>
                </div>
                <div className="metric">
                  <span>Priorité haute</span>
                  <strong>{prompts.summary.highPriorityPrompts}</strong>
                </div>
                <div className="metric">
                  <span>Correction</span>
                  <strong>{prompts.summary.fixPrompts}</strong>
                </div>
                <div className="metric">
                  <span>Validation</span>
                  <strong>{prompts.summary.validationPrompts}</strong>
                </div>
              </div>
              <div className="stack">
                {prompts.prompts.length === 0 ? (
                  <p>Aucun prompt n'a été généré pour ce run.</p>
                ) : (
                  prompts.prompts.map((prompt) => (
                    <PromptCard
                      key={`${prompt.repository}-${prompt.pullRequestNumber ?? "repo"}-${prompt.promptType}`}
                      prompt={prompt}
                    />
                  ))
                )}
              </div>
            </>
          ) : (
            <p>Lancez une analyse pour afficher les prompts.</p>
          )}
        </article>
      </section>
    </div>
  );
}
