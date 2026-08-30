import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { PageShell } from "../components/PageShell";
import { Button } from "../components/Button";
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

interface ClaimDetails {
  shareholderFullName: string;
  shareholderIdNumber: string;
  companyName: string;
  amountRands: string;
  certificateNumber: string;
}

const EMPTY_DETAILS: ClaimDetails = {
  shareholderFullName: "",
  shareholderIdNumber: "",
  companyName: "",
  amountRands: "",
  certificateNumber: "",
};

export function ClaimTypeSelector() {
  const navigate = useNavigate();
  const [selectedType, setSelectedType] = useState<ClaimType | null>(null);
  const [details, setDetails] = useState<ClaimDetails>(EMPTY_DETAILS);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!selectedType) return;

    setIsSubmitting(true);
    setError(null);
    try {
      const claim = await api.post<ClaimSummary>("/claims", {
        claimType: selectedType,
        shareholderFullName: details.shareholderFullName || null,
        shareholderIdNumber: details.shareholderIdNumber || null,
        companyName: details.companyName || null,
        amountCents: details.amountRands ? Math.round(parseFloat(details.amountRands) * 100) : null,
        certificateNumber: details.certificateNumber || null,
      });
      navigate(`/claims/${claim.claimId}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not start a new claim. Please try again.");
    } finally {
      setIsSubmitting(false);
    }
  }

  if (selectedType) {
    return (
      <PageShell>
        <div className="mx-auto max-w-form px-margin-mobile py-16">
          <button
            onClick={() => setSelectedType(null)}
            className="mb-6 flex items-center gap-1 font-label-md text-label-md text-ink-secondary hover:text-accent-verified"
          >
            <span className="material-symbols-outlined text-lg">arrow_back</span>
            Back
          </button>

          <h1 className="mb-2 font-display text-display-md text-ink-primary">
            {OPTIONS.find((o) => o.type === selectedType)?.title}
          </h1>
          <p className="mb-8 font-body text-body-md text-ink-secondary">
            Tell us about the shareholding — you'll confirm and verify everything else in the next steps.
          </p>

          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            <label className="flex flex-col gap-2">
              <span className="font-label-md text-label-md text-ink-primary">
                {selectedType === "DeceasedEstate" ? "Deceased shareholder's full name" : "Shareholder's full name"}
              </span>
              <input
                required
                value={details.shareholderFullName}
                onChange={(e) => setDetails((d) => ({ ...d, shareholderFullName: e.target.value }))}
                className="rounded border border-border-hairline bg-bg-surface px-4 py-3 font-body text-body-md text-ink-primary focus:border-accent-verified"
              />
            </label>

            <label className="flex flex-col gap-2">
              <span className="font-label-md text-label-md text-ink-primary">South African ID number</span>
              <input
                required
                inputMode="numeric"
                maxLength={13}
                value={details.shareholderIdNumber}
                onChange={(e) => setDetails((d) => ({ ...d, shareholderIdNumber: e.target.value }))}
                className="rounded border border-border-hairline bg-bg-surface px-4 py-3 font-mono text-code-md text-ink-primary focus:border-accent-verified"
              />
            </label>

            <label className="flex flex-col gap-2">
              <span className="font-label-md text-label-md text-ink-primary">Company the shares are registered with</span>
              <input
                required
                value={details.companyName}
                onChange={(e) => setDetails((d) => ({ ...d, companyName: e.target.value }))}
                className="rounded border border-border-hairline bg-bg-surface px-4 py-3 font-body text-body-md text-ink-primary focus:border-accent-verified"
              />
            </label>

            {selectedType === "LivingShareholder" && (
              <label className="flex flex-col gap-2">
                <span className="font-label-md text-label-md text-ink-primary">Dividend amount you're claiming (ZAR)</span>
                <input
                  inputMode="decimal"
                  value={details.amountRands}
                  onChange={(e) => setDetails((d) => ({ ...d, amountRands: e.target.value }))}
                  className="rounded border border-border-hairline bg-bg-surface px-4 py-3 font-mono text-code-md text-ink-primary focus:border-accent-verified"
                />
              </label>
            )}

            {selectedType === "LostCertificate" && (
              <label className="flex flex-col gap-2">
                <span className="font-label-md text-label-md text-ink-primary">Certificate number (if you remember it)</span>
                <input
                  value={details.certificateNumber}
                  onChange={(e) => setDetails((d) => ({ ...d, certificateNumber: e.target.value }))}
                  className="rounded border border-border-hairline bg-bg-surface px-4 py-3 font-mono text-code-md text-ink-primary focus:border-accent-verified"
                />
              </label>
            )}

            {error && <p className="font-body text-body-md text-status-error">{error}</p>}

            <Button type="submit" disabled={isSubmitting} className="mt-2">
              {isSubmitting ? "Starting your claim…" : "Continue to verification"}
            </Button>
          </form>
        </div>
      </PageShell>
    );
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
              onClick={() => setSelectedType(option.type)}
              className="group flex items-center gap-5 rounded-lg border border-border-hairline bg-bg-surface p-6 text-left transition-colors hover:border-accent-verified hover:bg-accent-verified-tint"
            >
              <span className="flex h-12 w-12 flex-shrink-0 items-center justify-center rounded-md bg-bg-subtle text-accent-verified group-hover:bg-white">
                <span className="material-symbols-outlined text-2xl">{option.icon}</span>
              </span>
              <div>
                <h2 className="font-display text-headline-md text-ink-primary">{option.title}</h2>
                <p className="font-body text-body-md text-ink-secondary">{option.body}</p>
              </div>
              <span className="material-symbols-outlined ml-auto flex-shrink-0 self-center text-ink-secondary transition-colors group-hover:text-accent-verified">
                arrow_forward
              </span>
            </button>
          ))}
        </div>
      </div>
    </PageShell>
  );
}
