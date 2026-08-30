import { useEffect, useState } from "react";
import { Navigate, useSearchParams } from "react-router-dom";
import { PageShell } from "../components/PageShell";
import { Card } from "../components/Card";
import { api } from "../lib/api";

export function ExecutorAccept() {
  const [params] = useSearchParams();
  const token = params.get("token");
  const [claimId, setClaimId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!token) return;
    api
      .post<{ claimId: string }>("/executor/accept", { token })
      .then((r) => setClaimId(r.claimId))
      .catch((err) => setError(err instanceof Error ? err.message : "This invitation link is not valid."));
  }, [token]);

  if (!token) {
    return (
      <PageShell>
        <div className="mx-auto max-w-form px-margin-mobile py-16">
          <p className="font-body text-body-md text-status-error">This link is missing its invitation token.</p>
        </div>
      </PageShell>
    );
  }

  if (claimId) return <Navigate to={`/claims/${claimId}`} replace />;

  return (
    <PageShell>
      <div className="mx-auto max-w-form px-margin-mobile py-16">
        <Card>
          {error ? (
            <p className="font-body text-body-md text-status-error">{error}</p>
          ) : (
            <p className="font-body text-body-md text-ink-secondary">Accepting your invitation…</p>
          )}
        </Card>
      </div>
    </PageShell>
  );
}
