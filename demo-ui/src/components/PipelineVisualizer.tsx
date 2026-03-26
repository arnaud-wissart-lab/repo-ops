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

  const allDone = steps.every((step) => step.state === "done");
  const hasFailure = steps.some((step) => step.state === "failed");
  const hasWarning = steps.some((step) => step.state === "warning");

  const completionLabel = hasFailure
    ? "Analyse terminée avec erreur"
    : hasWarning
      ? "Analyse terminée avec vigilance"
      : allDone
        ? "Analyse terminée avec succès"
        : "Pipeline prêt à s’exécuter";
  const completionNote = hasFailure
    ? "Un point bloquant a été détecté et documenté dans le run."
    : hasWarning
      ? "Le run est terminé, avec des décisions à relire avant toute suite."
      : allDone
        ? "Le pipeline s’est déroulé complètement et la synthèse est prête."
        : "Le système attend un déclenchement pour dérouler le run complet.";

  return (
    <section
      className={`panel panel-reveal ${
        allDone || hasWarning || hasFailure ? "pipeline-panel-complete" : ""
      }`}
    >
      <div className="panel-header">
        <div>
          <p className="section-kicker">Pipeline</p>
          <h2>Timeline du run</h2>
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

      <div
        className={`pipeline-completion pipeline-completion-${
          hasFailure ? "failed" : hasWarning ? "warning" : allDone ? "done" : "idle"
        }`}
      >
        <span className="pipeline-completion-dot" />
        <div className="pipeline-completion-copy">
          <strong>{completionLabel}</strong>
          <span>{completionNote}</span>
        </div>
      </div>
    </section>
  );
}
