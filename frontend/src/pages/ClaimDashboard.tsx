import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { PageShell } from "../components/PageShell";
import { Button } from "../components/Button";
import { ClaimStatusBadge } from "../components/StatusBadge";
import { api } from "../lib/api";
import { CLAIM_TYPE_LABELS, formatRands, type ClaimSummary } from "../types/domain";

export function ClaimDashboard() {
  const [claims, setClaims] = useState<ClaimSummary[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api
      .get<ClaimSummary[]>("/claims")
      .then(setClaims)
      .catch((err) => setError(err instanceof Error ? err.message : "Could not load your claims."));
  }, []);

  return (
    <PageShell>
      <div className="mx-auto max-w-7xl px-margin-mobile py-16 md:px-margin-desktop">
        <header className="mb-10 flex flex-col items-start justify-between gap-4 md:flex-row md:items-center">
          <div className="flex flex-col gap-2">
            <h1 className="font-display text-display-md text-ink-primary">Your Claims</h1>
            <p className="max-w-2xl font-body text-body-md text-ink-secondary">
              Review and track the status of every claim you've started.
            </p>
          </div>
          <Link to="/claims/new">
            <Button>
              <span className="material-symbols-outlined text-lg">add</span>
              Start a new claim
            </Button>
          </Link>
        </header>

        {error && <p className="font-body text-body-md text-status-error">{error}</p>}

        {claims === null && !error && <p className="font-body text-body-md text-ink-secondary">Loading…</p>}

        {claims?.length === 0 && (
          <div className="rounded-lg border border-dashed border-border-hairline bg-bg-surface p-12 text-center">
            <p className="font-body text-body-md text-ink-secondary">You haven't started a claim yet.</p>
          </div>
        )}

        {claims && claims.length > 0 && (
          <div className="overflow-x-auto rounded-lg border border-border-hairline bg-bg-surface">
            <table className="w-full min-w-[640px] border-collapse text-left">
              <thead>
                <tr className="border-b border-border-hairline">
                  <th className="whitespace-nowrap px-gutter py-3 font-label-md text-label-md text-ink-secondary">
                    Claim
                  </th>
                  <th className="whitespace-nowrap px-gutter py-3 font-label-md text-label-md text-ink-secondary">
                    Status
                  </th>
                  <th className="whitespace-nowrap px-gutter py-3 text-right font-label-md text-label-md text-ink-secondary">
                    Amount
                  </th>
                  <th className="w-10 px-gutter py-3" />
                </tr>
              </thead>
              <tbody>
                {claims.map((claim) => (
                  <tr key={claim.claimId} className="border-b border-border-hairline last:border-0 hover:bg-bg-subtle">
                    <td className="px-gutter py-4">
                      <Link to={`/claims/${claim.claimId}`} className="block">
                        <div className="font-label-md text-label-md text-ink-primary">
                          {CLAIM_TYPE_LABELS[claim.claimType]}
                        </div>
                        <div className="font-mono text-code-md text-ink-secondary">
                          {claim.claimId.slice(0, 8)} · {claim.companyName ?? "Company not yet specified"}
                        </div>
                      </Link>
                    </td>
                    <td className="px-gutter py-4">
                      <ClaimStatusBadge status={claim.status} />
                    </td>
                    <td className="px-gutter py-4 text-right font-mono text-code-md text-ink-primary">
                      {formatRands(claim.amountCents)}
                    </td>
                    <td className="px-gutter py-4 text-right">
                      <Link to={`/claims/${claim.claimId}`} className="text-accent-verified">
                        <span className="material-symbols-outlined align-middle">arrow_forward</span>
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </PageShell>
  );
}
