import { LaptopMinimal, MoonStar, SunMedium } from "lucide-react";
import { useTheme } from "next-themes";
import { Badge } from "./ui/badge";
import { Button } from "./ui/button";

export function DemoModeBadge() {
  const { theme, setTheme } = useTheme();

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
        <span className="text-xs font-semibold uppercase tracking-[0.14em] text-muted-foreground">
          Thème
        </span>
        <div className="inline-flex rounded-md border border-border bg-secondary p-1">
          <Button
            type="button"
            size="sm"
            variant={theme === "light" ? "secondary" : "ghost"}
            className="h-8 gap-1.5 px-2.5"
            onClick={() => setTheme("light")}
          >
            <SunMedium className="size-3.5" />
            Clair
          </Button>
          <Button
            type="button"
            size="sm"
            variant={theme === "dark" ? "secondary" : "ghost"}
            className="h-8 gap-1.5 px-2.5"
            onClick={() => setTheme("dark")}
          >
            <MoonStar className="size-3.5" />
            Sombre
          </Button>
          <Button
            type="button"
            size="sm"
            variant={theme === "system" ? "secondary" : "ghost"}
            className="h-8 gap-1.5 px-2.5"
            onClick={() => setTheme("system")}
          >
            <LaptopMinimal className="size-3.5" />
            Auto
          </Button>
        </div>
      </div>
    </div>
  );
}
