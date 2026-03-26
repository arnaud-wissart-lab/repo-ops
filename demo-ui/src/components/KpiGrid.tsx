interface KpiGridProps {
  analyzedPullRequests: number;
  readyPullRequests: number;
  blockedPullRequests: number;
  vulnerabilities: number;
  proposedActions: number;
}

const items = [
  { key: "analyzed", label: "PR analysées" },
  { key: "ready", label: "PR prêtes" },
  { key: "blocked", label: "PR bloquées" },
  { key: "vulnerabilities", label: "Vulnérabilités" },
  { key: "actions", label: "Actions proposées" },
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
        <article key={item.key} className="kpi-card">
          <p>{item.label}</p>
          <strong>{values[item.key]}</strong>
        </article>
      ))}
    </section>
  );
}
