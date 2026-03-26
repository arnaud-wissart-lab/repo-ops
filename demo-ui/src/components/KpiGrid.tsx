interface KpiGridProps {
  analyzedPullRequests: number;
  readyPullRequests: number;
  blockedPullRequests: number;
  vulnerabilities: number;
  proposedActions: number;
}

const items = [
  { key: "analyzed", label: "PR analysées", icon: "✓", tone: "neutral" },
  { key: "ready", label: "PR prêtes", icon: "→", tone: "done" },
  { key: "blocked", label: "PR bloquées", icon: "!", tone: "warning" },
  { key: "vulnerabilities", label: "Vulnérabilités", icon: "🛡", tone: "failed" },
  { key: "actions", label: "Actions proposées", icon: "IA", tone: "info" },
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
            <p>{item.label}</p>
          </div>
          <strong>{values[item.key]}</strong>
          <span className="kpi-caption">Lecture instantanée du run</span>
        </article>
      ))}
    </section>
  );
}
