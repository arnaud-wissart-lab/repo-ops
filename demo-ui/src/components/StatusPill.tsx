import type { PipelineStepState } from "../types";
import { Badge } from "./ui/badge";

interface StatusPillProps {
  label: string;
  tone?: PipelineStepState | "neutral";
}

export function StatusPill({ label, tone = "neutral" }: StatusPillProps) {
  const variant =
    tone === "done"
      ? "success"
      : tone === "warning" || tone === "running"
        ? "warning"
        : tone === "failed"
          ? "danger"
          : "neutral";

  return <Badge variant={variant}>{label}</Badge>;
}
