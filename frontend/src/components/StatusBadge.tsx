import type { ClaimStatus, VerificationStepStatus } from "../types/domain";

type Tone = "neutral" | "progress" | "success" | "warning" | "error";

const CLAIM_STATUS_TONE: Record<ClaimStatus, Tone> = {
  Draft: "neutral",
  Pending: "progress",
  Verified: "success",
  ActionNeeded: "warning",
  Complete: "success",
  Rejected: "error",
};

const STEP_STATUS_TONE: Record<VerificationStepStatus, Tone> = {
  NotStarted: "neutral",
  InProgress: "progress",
  Passed: "success",
  Failed: "error",
  AwaitingCounterparty: "warning",
};

// Tinted background + matching text, never color alone - always paired with an icon
// and label (design.md, Component notes + Accessibility).
const TONE_CLASSES: Record<Tone, string> = {
  neutral: "bg-bg-subtle text-ink-secondary",
  progress: "bg-accent-verified-tint text-accent-verified",
  success: "bg-accent-verified-tint text-accent-verified",
  warning: "bg-status-warning-tint text-status-warning",
  error: "bg-status-error-tint text-status-error",
};

const TONE_ICON: Record<Tone, string> = {
  neutral: "radio_button_unchecked",
  progress: "autorenew",
  success: "task_alt",
  warning: "priority_high",
  error: "error",
};

const CLAIM_STATUS_LABEL: Record<ClaimStatus, string> = {
  Draft: "Draft",
  Pending: "Pending",
  Verified: "Verified",
  ActionNeeded: "Action needed",
  Complete: "Complete",
  Rejected: "Rejected",
};

const STEP_STATUS_LABEL: Record<VerificationStepStatus, string> = {
  NotStarted: "Not started",
  InProgress: "In progress",
  Passed: "Verified",
  Failed: "Action needed",
  AwaitingCounterparty: "Awaiting counterparty",
};

function Badge({ tone, label }: { tone: Tone; label: string }) {
  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-full px-3 py-1 font-label-sm text-label-sm ${TONE_CLASSES[tone]}`}
    >
      <span className="material-symbols-outlined" style={{ fontSize: 14 }}>
        {TONE_ICON[tone]}
      </span>
      {label}
    </span>
  );
}

export function ClaimStatusBadge({ status }: { status: ClaimStatus }) {
  return <Badge tone={CLAIM_STATUS_TONE[status]} label={CLAIM_STATUS_LABEL[status]} />;
}

export function StepStatusBadge({ status }: { status: VerificationStepStatus }) {
  return <Badge tone={STEP_STATUS_TONE[status]} label={STEP_STATUS_LABEL[status]} />;
}
