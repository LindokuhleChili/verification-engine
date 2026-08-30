import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { PageShell } from "../components/PageShell";
import { api } from "../lib/api";
import type { ClaimSummary, ClaimType } from "../types/domain";

const OPTIONS: { type: ClaimType; icon: string; title: string; body: string }[] = [
  {
    type: "LivingShareholder",
    icon: "person",
    title: "I'm the shareholder",
    body: "I want to claim dividends or update my details on shares registered in my own name.",
  },
  {
    type: "DeceasedEstate",
    icon: "diversity_3",
    title: "I'm inheriting shares",
    body: "I am an executor, administrator, or beneficiary of a deceased estate.",
  },
  {
    type: "LostCertificate",
    icon: "find_in_page",
    title: "I lost my certificate",
    body: "I am the registered shareholder but need to replace a lost or damaged paper certificate.",
  },
];

export function ClaimTypeSelector() {
  const navigate = useNavigate();
  const [pending, setPending] = useState<ClaimType | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function selectType(type: ClaimType) {
    setPending(type);
    setError(null);
    try {
      const claim = await api.post<ClaimSummary>("/claims", { claimType: type });
      navigate(`/claims/${claim.claimId}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not start a new claim. Please try again.");
      setPending(null);
    }
  }

  return (
    <PageShell>
      <div className="mx-auto max-w-form px-margin-mobile py-16">
        <div className="mb-10">
          <h1 className="mb-4 font-display text-display-md text-ink-primary">What are you claiming today?</h1>
          <p className="font-body text-body-lg text-ink-secondary">
            Select the option that best describes your situation to begin verification.
          </p>
        </div>

        <div className="flex flex-col gap-4">
          {OPTIONS.map((option) => (
            <button
              key={option.type}
              disabled={pending !== null}
              onClick={() => void selectType(option.type)}
              className="group flex items-center gap-5 rounded-lg border border-border-hairline bg-bg-surface p-6 text-left transition-colors hover:border-accent-verified hover:bg-accent-verified-tint disabled:opacity-60"
            >
              <span className="flex h-12 w-12 flex-shrink-0 items-center justify-center rounded-md bg-bg-subtle text-accent-verified group-hover:bg-white">
                <span className="material-symbols-outlined text-2xl">{option.icon}</span>
              </span>
              <div>
                <h2 className="font-display text-headline-md text-ink-primary">{option.title}</h2>
                <p className="font-body text-body-md text-ink-secondary">{option.body}</p>
              </div>
              <span className="material-symbols-outlined ml-auto flex-shrink-0 self-center text-ink-secondary transition-colors group-hover:text-accent-verified">
                {pending === option.type ? "hourglass_top" : "arrow_forward"}
              </span>
            </button>
          ))}
        </div>

        {error && <p className="mt-4 font-body text-body-md text-status-error">{error}</p>}
      </div>
    </PageShell>
  );
}
