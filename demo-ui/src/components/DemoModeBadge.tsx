import { MonitorCog, MoonStar, SunMedium } from "lucide-react";
import { useTheme } from "next-themes";
import { Badge } from "./ui/badge";

export function DemoModeBadge() {
  const { theme, resolvedTheme, setTheme } = useTheme();

  return (
    <div
      aria-label="Mode démonstration actif"
      className="glass-panel flex flex-wrap items-center gap-3 rounded-xl border border-border px-3 py-2"
    >
      <div className="flex items-center gap-2">
        <Badge variant="warning">Mode démonstration</Badge>
        <Badge variant="info">Dry-run</Badge>
      </div>

      <div className="flex items-center gap-2">
        <label
          className="text-xs font-semibold uppercase tracking-[0.14em] text-muted-foreground"
          htmlFor="theme-select"
        >
          Thème
        </label>

        <div className="relative">
          <select
            id="theme-select"
            className="h-9 min-w-32 rounded-md border border-border bg-card pl-9 pr-3 text-sm text-foreground"
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
      </div>

      <span className="text-sm text-muted-foreground">
        {resolvedTheme === "dark" ? "Sombre actif" : "Clair actif"}
      </span>
    </div>
  );
}
