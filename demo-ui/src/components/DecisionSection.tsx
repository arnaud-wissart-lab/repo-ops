import type { SupervisorAction } from "../types";
import { StatusPill } from "./StatusPill";

interface DecisionSectionProps {
  actions: SupervisorAction[];
}

function typeMeta(type: string): {
  label: string;
  icon: string;
  tone: "done" | "warning" | "failed" | "neutral";
} {
  const normalized = type.toLowerCase();

  if (normalized === "automergeeligible") {
    return { label: "Auto-merge éligible", icon: "AM", tone: "done" };
  }

  if (normalized === "fixrequired") {
    return { label: "Correctif requis", icon: "FX", tone: "failed" };
  }

  if (normalized === "review") {
    return { label: "Revue", icon: "RV", tone: "warning" };
  }

  return { label: "Ignoré", icon: "IG", tone: "neutral" };
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
                  <div className="decision-badges">
                    <span className={`decision-type-badge decision-type-badge-${typeMeta(action.type).tone}`}>
                      <span className="decision-type-icon">{typeMeta(action.type).icon}</span>
                      <span>{typeMeta(action.type).label}</span>
                    </span>
                    {action.isSecurityRelated ? (
                      <span className="decision-security-badge">
                        SEC {action.securitySeverity || "prioritaire"}
                      </span>
                    ) : null}
                  </div>
                  <h3>{action.pullRequestTitle || action.repository}</h3>
                </div>
                <StatusPill label={action.priority} tone={toneFromPriority(action.priority)} />
              </div>

              <div className="decision-meta">
                <span>{action.repository}</span>
                {action.pullRequestNumber ? <span>PR #{action.pullRequestNumber}</span> : null}
                {action.checksStatus ? <span>Checks : {action.checksStatus}</span> : null}
                <span>Statut : {typeMeta(action.type).label}</span>
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
