import { MonitorCog, MoonStar, SunMedium } from "lucide-react";
import { useTheme } from "next-themes";
import { Badge } from "./ui/badge";

export function DemoModeBadge() {
  const { theme, resolvedTheme, setTheme } = useTheme();

  return (
    <div className="sticky top-4 z-30 mx-auto flex w-full max-w-[1480px] justify-end px-4 sm:px-6 lg:px-8">
      <div
        aria-label="Mode démonstration actif"
        className="glass-panel flex flex-wrap items-center gap-3 rounded-2xl border border-border/80 px-4 py-3 shadow-lg shadow-slate-900/5"
      >
        <div className="flex items-center gap-2">
          <Badge variant="warning">Mode démonstration</Badge>
          <Badge variant="info">Dry-run</Badge>
          <Badge variant="neutral">Sans modification réelle</Badge>
        </div>
        <label className="flex items-center gap-2 text-xs font-semibold uppercase tracking-[0.14em] text-muted-foreground" htmlFor="theme-select">
          <span>Thème</span>
        </label>
        <div className="relative">
          <select
            id="theme-select"
            className="h-9 min-w-36 rounded-lg border border-border bg-card pl-10 pr-3 text-sm text-foreground shadow-xs transition-colors hover:border-slate-300 focus-visible:ring-2 focus-visible:ring-ring"
            value={theme ?? "light"}
            onChange={(event) => setTheme(event.target.value)}
          >
            <option value="light">Clair</option>
            <option value="dark">Sombre</option>
            <option value="system">Auto</option>
          </select>
          <span className="pointer-events-none absolute inset-y-0 left-3 flex items-center text-muted-foreground">
            {theme === "dark" ? (
              <MoonStar className="size-4" />
            ) : theme === "system" ? (
              <MonitorCog className="size-4" />
            ) : (
              <SunMedium className="size-4" />
            )}
          </span>
        </div>
        <span className="text-sm text-muted-foreground">
          {resolvedTheme === "dark" ? "Sombre actif" : "Clair actif"}
        </span>
      </div>
    </div>
  );
}
