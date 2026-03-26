import type { PipelineStepState } from "../types";

interface StatusPillProps {
  label: string;
  tone?: PipelineStepState | "neutral";
}

export function StatusPill({ label, tone = "neutral" }: StatusPillProps) {
  return <span className={`status-pill status-pill-${tone}`}>{label}</span>;
}
