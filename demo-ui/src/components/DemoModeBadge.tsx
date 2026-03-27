import type { ResolvedTheme, ThemePreference } from "../types";

interface DemoModeBadgeProps {
  themePreference: ThemePreference;
  resolvedTheme: ResolvedTheme;
  onThemeChange: (nextTheme: ThemePreference) => void;
}

function themeLabel(theme: ResolvedTheme): string {
  return theme === "light" ? "Clair actif" : "Sombre actif";
}

export function DemoModeBadge({
  themePreference,
  resolvedTheme,
  onThemeChange,
}: DemoModeBadgeProps) {
  return (
    <div className="demo-mode-badge" aria-label="Mode démonstration actif">
      <div className="demo-mode-copy">
        <span className="demo-mode-dot" />
        <span>Mode démonstration · Dry-run · Sans modification réelle</span>
      </div>
      <label className="theme-selector" htmlFor="theme-select">
        <span>Thème</span>
        <select
          id="theme-select"
          className="theme-select"
          value={themePreference}
          onChange={(event) => onThemeChange(event.target.value as ThemePreference)}
        >
          <option value="light">Clair</option>
          <option value="dark">Sombre</option>
          <option value="auto">Auto</option>
        </select>
      </label>
      <span className="theme-mode-indicator">{themeLabel(resolvedTheme)}</span>
    </div>
  );
}
