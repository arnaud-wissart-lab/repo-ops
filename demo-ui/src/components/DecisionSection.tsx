import type { SupervisorAction } from "../types";
import { StatusPill } from "./StatusPill";

interface DecisionSectionProps {
  actions: SupervisorAction[];
}

function toneFromPriority(priority: string): "done" | "warning" | "failed" | "neutral" {
  const normalized = priority.toLowerCase();

  if (normalized === "high") {
    return "failed";
  }

  if (normalized === "medium") {
    return "warning";
  }

  if (normalized === "low") {
    return "done";
  }

  return "neutral";
}

export function DecisionSection({ actions }: DecisionSectionProps) {
  return (
    <section className="panel">
      <div className="panel-header">
        <div>
          <p className="section-kicker">Décisions</p>
          <h2>Actions structurées</h2>
        </div>
        <p className="subtle-text">
          Le moteur de décision reste explicable. Rien n’est exécuté depuis
          cette vue.
        </p>
      </div>

      {actions.length === 0 ? (
        <p className="empty-state">
          Aucune action structurée n’a été produite pour ce scénario.
        </p>
      ) : (
        <div className="decision-list">
          {actions.map((action) => (
            <article
              key={`${action.repository}-${action.pullRequestNumber ?? "repo"}-${action.type}`}
              className="decision-card"
            >
              <div className="decision-topline">
                <div>
                  <p className="decision-type">{action.type}</p>
                  <h3>{action.pullRequestTitle || action.repository}</h3>
                </div>
                <StatusPill label={action.priority} tone={toneFromPriority(action.priority)} />
              </div>

              <div className="decision-meta">
                <span>{action.repository}</span>
                {action.pullRequestNumber ? <span>PR #{action.pullRequestNumber}</span> : null}
                {action.checksStatus ? <span>Checks : {action.checksStatus}</span> : null}
              </div>

              <p>{action.reason}</p>

              {action.recommendation ? (
                <p className="recommendation-text">
                  Recommandation : {action.recommendation}
                </p>
              ) : null}

              {action.pullRequestUrl ? (
                <a href={action.pullRequestUrl} target="_blank" rel="noreferrer">
                  Ouvrir la pull request source
                </a>
              ) : null}
            </article>
          ))}
        </div>
      )}
    </section>
  );
}
