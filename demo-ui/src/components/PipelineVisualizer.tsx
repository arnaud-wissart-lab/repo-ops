import type { PipelineStep } from "../types";

interface PipelineVisualizerProps {
  steps: PipelineStep[];
}

export function PipelineVisualizer({ steps }: PipelineVisualizerProps) {
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

      <div className="pipeline-grid">
        {steps.map((step, index) => (
          <article key={step.key} className={`pipeline-step pipeline-step-${step.state}`}>
            <div className="pipeline-marker">
              <span>{index + 1}</span>
            </div>
            <div className="pipeline-content">
              <p className="pipeline-label">{step.label}</p>
              <p className="pipeline-description">{step.description}</p>
            </div>
            <div className={`pipeline-state pipeline-state-${step.state}`}>
              <span className="pipeline-dot" />
              <span>{step.state}</span>
            </div>
          </article>
        ))}
      </div>
    </section>
  );
}
