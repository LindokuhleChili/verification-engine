import type { StepResponse } from "../types/domain";

/**
 * A persistent, quiet step indicator: plain text labels joined by arrows, not
 * numbered circles with connecting lines (design.md, Layout concept) - the claim
 * flows are a real fixed sequence, so the numbered-circle pattern would be earned
 * here too, but this stays deliberately plainer to match the reference screens.
 */
export function StepIndicator({ steps, currentStep }: { steps: StepResponse[]; currentStep?: string }) {
  return (
    <nav aria-label="Claim progress" className="flex flex-wrap items-center gap-x-2 gap-y-1">
      {steps.map((step, index) => {
        const isCurrent = step.name === currentStep;
        const isDone = step.status === "Passed";

        return (
          <span key={step.name} className="flex items-center gap-2">
            <span
              className={`font-label-md text-label-md ${
                isCurrent
                  ? "text-accent-verified"
                  : isDone
                    ? "text-ink-primary"
                    : "text-ink-secondary"
              }`}
            >
              {step.label}
            </span>
            {index < steps.length - 1 && (
              <span aria-hidden="true" className="text-ink-secondary">
                →
              </span>
            )}
          </span>
        );
      })}
    </nav>
  );
}
