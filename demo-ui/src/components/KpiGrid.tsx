interface KpiGridProps {
  analyzedPullRequests: number;
  readyPullRequests: number;
  blockedPullRequests: number;
  vulnerabilities: number;
  proposedActions: number;
}

const items = [
  { key: "analyzed", label: "PR analysées", icon: "✓", accent: "Vue d’ensemble", tone: "neutral" },
  { key: "ready", label: "PR prêtes", icon: "→", accent: "Prêtes à traiter", tone: "done" },
  { key: "blocked", label: "PR bloquées", icon: "!", accent: "Points d’attention", tone: "warning" },
  { key: "vulnerabilities", label: "Vulnérabilités", icon: "🛡", accent: "Risque sécurité", tone: "failed" },
  { key: "actions", label: "Actions proposées", icon: "IA", accent: "Sortie superviseur", tone: "info" },
] as const;

export function KpiGrid({
  analyzedPullRequests,
  readyPullRequests,
  blockedPullRequests,
  vulnerabilities,
  proposedActions,
}: KpiGridProps) {
  const values = {
    analyzed: analyzedPullRequests,
    ready: readyPullRequests,
    blocked: blockedPullRequests,
    vulnerabilities,
    actions: proposedActions,
  };

  return (
    <section className="kpi-grid">
      {items.map((item) => (
        <article key={item.key} className={`kpi-card kpi-card-${item.tone}`}>
          <div className="kpi-topline">
            <span className="kpi-icon">{item.icon}</span>
            <div className="kpi-copy">
              <p>{item.label}</p>
              <span>{item.accent}</span>
            </div>
          </div>
          <strong>{values[item.key]}</strong>
          <span className="kpi-caption">Lecture instantanée du run</span>
        </article>
      ))}
    </section>
  );
}
