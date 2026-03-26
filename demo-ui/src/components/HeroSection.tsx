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
        <h1>Le cockpit local qui raconte tout le pipeline de maintenance.</h1>
        <p className="hero-description">
          Cette interface met en scène le run du worker, les décisions,
          l’enchaînement superviseur et les sorties techniques, dans une
          démonstration sûre pensée pour la relecture technique.
        </p>
        <div className="hero-meta">
          <span>{modeLabel(mode)}</span>
          <span>Pipeline visible de bout en bout</span>
          <span>Validation humaine conservée</span>
          <span>Exemple chargeable sans backend</span>
        </div>
      </div>

      <aside className="hero-actions-card">
        <div>
          <p className="section-kicker">Déclenchement</p>
          <h2>Lancer un scénario contrôlé</h2>
          <p className="subtle-text">
            Le bouton principal appelle l’API locale si elle est disponible. Le
            scénario d’exemple charge un jeu réaliste pour la démonstration hors
            backend.
          </p>
          <p className="hero-note">
            Vous pouvez donc tester l’interface même sans API disponible.
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
            ? "Le pipeline se déroule étape par étape pour rendre l’exécution lisible."
            : isDeploying
              ? "Le worker déclenche le déploiement local configuré pour cette machine."
              : "Le déclenchement reste strictement en dry-run sur les actions sensibles et le déploiement vise la même machine locale."}
        </p>
      </aside>
    </header>
  );
}
