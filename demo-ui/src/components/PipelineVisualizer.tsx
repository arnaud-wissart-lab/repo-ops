import type { PipelineStep } from "../types";

interface PipelineVisualizerProps {
  steps: PipelineStep[];
}

export function PipelineVisualizer({ steps }: PipelineVisualizerProps) {
  const stateLabels: Record<PipelineStep["state"], string> = {
    idle: "En attente",
    running: "En cours",
    done: "Terminé",
    warning: "Vigilance",
    failed: "Erreur",
  };

  return (
    <section className="panel">
      <div className="panel-header">
        <div>
          <p className="section-kicker">Pipeline</p>
          <h2>Chaîne de traitement visualisée</h2>
        </div>
        <p className="subtle-text">
          Chaque étape reflète l’état courant du run, sans automatisation
          dangereuse.
        </p>
      </div>

      <div className="pipeline-timeline">
        {steps.map((step, index) => (
          <article key={step.key} className={`pipeline-step pipeline-step-${step.state}`}>
            {index < steps.length - 1 ? <span className="pipeline-connector" aria-hidden="true" /> : null}
            <div className="pipeline-marker">
              <span>{index + 1}</span>
            </div>
            <div className="pipeline-content">
              <p className="pipeline-label">{step.label}</p>
              <p className="pipeline-description">{step.description}</p>
            </div>
            <div className={`pipeline-state pipeline-state-${step.state}`}>
              <span className="pipeline-dot" />
              <span>{stateLabels[step.state]}</span>
            </div>
          </article>
        ))}
      </div>
    </section>
  );
}
