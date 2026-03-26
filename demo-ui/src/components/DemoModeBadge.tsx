export function DemoModeBadge() {
  return (
    <div className="demo-mode-badge" aria-label="Mode démonstration actif">
      <span className="demo-mode-dot" />
      <span>Mode démonstration · Dry-run · Sans modification réelle</span>
    </div>
  );
}
