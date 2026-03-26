export function EmptyStatePanel() {
  return (
    <section className="panel empty-state-panel">
      <div className="panel-header">
        <div>
          <p className="section-kicker">Scénario de démonstration</p>
          <h2>Prêt à raconter un run crédible</h2>
        </div>
      </div>

      <div className="empty-state-grid">
        <article className="empty-state-card">
          <h3>Sans backend</h3>
          <p>
            Utilisez <strong>Charger un exemple</strong> pour afficher un cycle
            complet réaliste avec PR, sécurité, décisions et prompts.
          </p>
        </article>

        <article className="empty-state-card">
          <h3>Avec backend</h3>
          <p>
            Utilisez <strong>Lancer une analyse</strong> pour exécuter le worker
            local et voir le pipeline réel étape par étape.
          </p>
        </article>

        <article className="empty-state-card">
          <h3>Ce que la démo montre</h3>
          <p>
            Une collecte GitHub, une qualification métier, des décisions
            structurées, des prompts et une sortie technique lisible.
          </p>
        </article>
      </div>
    </section>
  );
}
