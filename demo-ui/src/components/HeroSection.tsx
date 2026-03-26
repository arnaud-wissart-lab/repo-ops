import type { DemoMode, UiStatus } from "../types";
import { StatusPill } from "./StatusPill";

interface HeroSectionProps {
  mode: DemoMode;
  status: UiStatus;
  onRun: () => Promise<void>;
  onLoadMock: () => Promise<void>;
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
  onRun,
  onLoadMock,
}: HeroSectionProps) {
  const isLoading = status === "loading";

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
        </div>

        <div className="hero-actions">
          <button
            type="button"
            className={isLoading ? "primary-button is-loading" : "primary-button"}
            onClick={() => void onRun()}
            disabled={isLoading}
          >
            <span className="primary-button-content">
              {isLoading ? <span className="button-spinner" aria-hidden="true" /> : null}
              <span>{isLoading ? "Analyse en cours..." : "Lancer une analyse"}</span>
            </span>
          </button>
          <button
            type="button"
            className="secondary-button"
            onClick={() => void onLoadMock()}
            disabled={isLoading}
          >
            Charger un exemple
          </button>
        </div>
        <p className="hero-feedback">
          {isLoading
            ? "Le pipeline se déroule étape par étape pour rendre l’exécution lisible."
            : "Le déclenchement reste strictement en dry-run et ne lance aucune action irréversible."}
        </p>
      </aside>
    </header>
  );
}
