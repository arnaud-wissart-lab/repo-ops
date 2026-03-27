export function EmptyStatePanel() {
  return (
    <section className="panel empty-state-panel">
      <div className="panel-header">
        <div>
          <p className="section-kicker">Guide rapide</p>
          <h2>Comment utiliser cette page</h2>
        </div>
      </div>

      <div className="empty-state-grid">
        <article className="empty-state-card">
          <h3>1. Comprendre le produit</h3>
          <p>
            RepoOps suit un run de maintenance logicielle : il collecte un état GitHub,
            le qualifie, produit des décisions puis affiche une synthèse exploitable.
          </p>
        </article>

        <article className="empty-state-card">
          <h3>2. Commencer sans risque</h3>
          <p>
            Utilisez <strong>Charger un exemple</strong> pour voir un run complet déjà
            rempli, sans dépendre d’une API locale.
          </p>
        </article>

        <article className="empty-state-card">
          <h3>3. Passer au réel</h3>
          <p>
            Utilisez ensuite <strong>Lancer une analyse</strong> pour exécuter le worker
            local si les endpoints backend sont disponibles.
          </p>
        </article>
      </div>
    </section>
  );
}
