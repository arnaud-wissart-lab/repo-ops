import type { DemoMode, UiStatus } from "../types";
import { StatusPill } from "./StatusPill";

interface HeroSectionProps {
  mode: DemoMode;
  status: UiStatus;
  deploymentStatus: UiStatus;
  onRun: () => Promise<void>;
  onLoadMock: () => Promise<void>;
  onDeploy: () => Promise<void>;
}

function modeLabel(mode: DemoMode): string {
  if (mode === "mock") {
    return "Source mock";
  }

  if (mode === "auto") {
    return "Mode auto";
  }

  return "Source API";
}

export function HeroSection({
  mode,
  status,
  deploymentStatus,
  onRun,
  onLoadMock,
  onDeploy,
}: HeroSectionProps) {
  const isLoading = status === "loading";
  const isDeploying = deploymentStatus === "loading";
  const isBusy = isLoading || isDeploying;

  return (
    <header className="hero-panel">
      <div className="hero-copy">
        <div className="hero-badges">
          <StatusPill label="Mode démonstration" tone="warning" />
          <StatusPill label="Dry-run" tone="done" />
          <StatusPill label="Aucune modification réelle" tone="neutral" />
        </div>
        <p className="section-kicker">RepoOps Live Demo</p>
        <h1>Suivre un run de maintenance logicielle, du signal GitHub à la synthèse.</h1>
        <p className="hero-description">
          Cette page sert à comprendre ce que fait RepoOps sur un cycle de maintenance :
          collecte, analyse, décisions, prompts et sortie technique. Elle doit d’abord
          être lisible, puis démonstrative.
        </p>
        <div className="hero-meta">
          <span>{modeLabel(mode)}</span>
          <span>Cycle expliqué étape par étape</span>
          <span>Validation humaine conservée</span>
          <span>Exemple disponible sans backend</span>
        </div>
      </div>

      <aside className="hero-actions-card">
        <div>
          <p className="section-kicker">Déclenchement</p>
          <h2>Commencer par un scénario lisible</h2>
          <p className="subtle-text">
            Si l’API locale est disponible, l’analyse utilise les endpoints réels.
            Sinon, le chargement d’exemple permet de comprendre immédiatement le rôle
            de l’application.
          </p>
          <p className="hero-note">
            Recommandation : commencez par <strong>Charger un exemple</strong> pour
            visualiser un run complet, puis passez à l’analyse réelle.
          </p>
        </div>

        <div className="hero-actions">
          <button
            type="button"
            className={isLoading ? "primary-button is-loading" : "primary-button"}
            onClick={() => void onRun()}
            disabled={isBusy}
          >
            <span className="primary-button-content">
              {isLoading ? <span className="button-spinner" aria-hidden="true" /> : null}
              <span>{isLoading ? "Analyse en cours..." : "Lancer une analyse"}</span>
            </span>
            <span className="primary-button-subtitle">
              {isLoading ? "Pipeline en exécution" : "Déclenchement principal"}
            </span>
          </button>
          <button
            type="button"
            className="secondary-button"
            onClick={() => void onLoadMock()}
            disabled={isBusy}
          >
            Charger un exemple
          </button>
          <button
            type="button"
            className={isDeploying ? "secondary-button is-loading" : "secondary-button"}
            onClick={() => void onDeploy()}
            disabled={isBusy}
          >
            {isDeploying ? "Déploiement en cours..." : "Déployer en local"}
          </button>
        </div>
        <p className="hero-feedback">
          {isLoading
            ? "Le pipeline se déroule étape par étape pour rendre l’exécution compréhensible."
            : isDeploying
              ? "Le worker déclenche le déploiement local configuré pour cette machine."
              : "La page reste en mode démonstration : les actions sensibles restent contrôlées et le scénario mock permet de comprendre RepoOps sans dépendre du backend."}
        </p>
      </aside>
    </header>
  );
}
