import { useState } from "react";
import type { GeneratedPrompt } from "../types";
import { truncate } from "../utils";

interface PromptSectionProps {
  prompts: GeneratedPrompt[];
}

function PromptRow({ prompt }: { prompt: GeneratedPrompt }) {
  const [copied, setCopied] = useState(false);

  async function copyPrompt() {
    await navigator.clipboard.writeText(prompt.promptText);
    setCopied(true);
    window.setTimeout(() => setCopied(false), 1500);
  }

  return (
    <details className="prompt-card">
      <summary>
        <div>
          <p className="decision-type">{prompt.promptType}</p>
          <h3>{prompt.pullRequestTitle || prompt.repository}</h3>
        </div>
        <span className="prompt-priority">{prompt.priority}</span>
      </summary>

      <div className="prompt-body">
        <div className="decision-meta">
          <span>{prompt.repository}</span>
          {prompt.pullRequestNumber ? <span>PR #{prompt.pullRequestNumber}</span> : null}
          <span>Checks : {prompt.context.checksStatus || "non précisé"}</span>
        </div>

        <p>{prompt.context.problemSummary}</p>
        <p className="recommendation-text">
          Recommandation : {prompt.context.recommendation}
        </p>

        <div className="prompt-preview">{truncate(prompt.promptText, 240)}</div>
        <pre>{prompt.promptText}</pre>

        <div className="prompt-actions">
          {prompt.pullRequestUrl ? (
            <a href={prompt.pullRequestUrl} target="_blank" rel="noreferrer">
              Ouvrir la pull request
            </a>
          ) : null}
          <button type="button" className="secondary-button" onClick={copyPrompt}>
            {copied ? "Prompt copié" : "Copier le prompt"}
          </button>
        </div>
      </div>
    </details>
  );
}

export function PromptSection({ prompts }: PromptSectionProps) {
  return (
    <section className="panel">
      <div className="panel-header">
        <div>
          <p className="section-kicker">Prompts générés</p>
          <h2>Prompts prêts pour Codex</h2>
        </div>
        <p className="subtle-text">
          Les templates restent lisibles, techniques et copiables directement.
        </p>
      </div>

      {prompts.length === 0 ? (
        <p className="empty-state">Aucun prompt n’a été généré pour ce run.</p>
      ) : (
        <div className="prompt-list">
          {prompts.map((prompt) => (
            <PromptRow
              key={`${prompt.repository}-${prompt.pullRequestNumber ?? "repo"}-${prompt.promptType}`}
              prompt={prompt}
            />
          ))}
        </div>
      )}
    </section>
  );
}
