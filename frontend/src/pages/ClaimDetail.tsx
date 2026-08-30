import { useCallback, useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { PageShell } from "../components/PageShell";
import { Card } from "../components/Card";
import { Button } from "../components/Button";
import { StepIndicator } from "../components/StepIndicator";
import { StepStatusBadge, ClaimStatusBadge } from "../components/StatusBadge";
import { DocumentUploadField } from "../components/DocumentUploadField";
import { SignaturePad } from "../components/SignaturePad";
import { api } from "../lib/api";
import { CLAIM_TYPE_LABELS, formatRands, type ClaimDetail, type StepResponse } from "../types/domain";

export function ClaimDetailPage() {
  const { claimId } = useParams<{ claimId: string }>();
  const [claim, setClaim] = useState<ClaimDetail | null>(null);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(() => {
    if (!claimId) return;
    api
      .get<ClaimDetail>(`/claims/${claimId}`)
      .then(setClaim)
      .catch((err) => setError(err instanceof Error ? err.message : "Could not load this claim."));
  }, [claimId]);

  useEffect(refresh, [refresh]);

  if (error) {
    return (
      <PageShell>
        <div className="mx-auto max-w-form px-margin-mobile py-16">
          <p className="font-body text-body-md text-status-error">{error}</p>
        </div>
      </PageShell>
    );
  }

  if (!claim || !claimId) {
    return (
      <PageShell>
        <div className="mx-auto max-w-form px-margin-mobile py-16">
          <p className="font-body text-body-md text-ink-secondary">Loading…</p>
        </div>
      </PageShell>
    );
  }

  const currentStep = claim.steps.find((s) => s.status !== "Passed")?.name;

  return (
    <PageShell>
      <div className="mx-auto max-w-form px-margin-mobile py-16">
        <header className="mb-8 flex flex-col gap-4">
          <div className="flex items-center justify-between gap-4">
            <h1 className="font-display text-display-md text-ink-primary">
              {CLAIM_TYPE_LABELS[claim.claimType]}
            </h1>
            <ClaimStatusBadge status={claim.status} />
          </div>
          <p className="font-mono text-code-md text-ink-secondary">
            Claim {claim.claimId} · {formatRands(claim.amountCents)}
          </p>
          <StepIndicator steps={claim.steps} currentStep={currentStep} />
        </header>

        {claim.status === "Complete" ? (
          <ConfirmationCard claimId={claim.claimId} />
        ) : (
          <div className="flex flex-col gap-6">
            {claim.steps.map((step) => (
              <StepCard key={step.name} claim={claim} step={step} onChanged={refresh} />
            ))}
          </div>
        )}
      </div>
    </PageShell>
  );
}

function StepCard({ claim, step, onChanged }: { claim: ClaimDetail; step: StepResponse; onChanged: () => void }) {
  return (
    <Card>
      <div className="mb-4 flex items-center justify-between gap-4">
        <h2 className="font-display text-headline-md text-ink-primary">{step.label}</h2>
        <StepStatusBadge status={step.status} />
      </div>
      {step.detail && <p className="mb-4 font-body text-body-md text-ink-secondary">{step.detail}</p>}

      {step.status !== "Passed" && <StepBody claim={claim} step={step} onChanged={onChanged} />}
    </Card>
  );
}

function StepBody({ claim, step, onChanged }: { claim: ClaimDetail; step: StepResponse; onChanged: () => void }) {
  switch (step.name) {
    case "IdentityBiometric":
      return <IdentityStep claim={claim} onChanged={onChanged} />;
    case "ExecutorIdentity":
      return claim.isOwner ? (
        <InviteExecutorStep claim={claim} onChanged={onChanged} />
      ) : (
        <IdentityStep claim={claim} onChanged={onChanged} />
      );
    case "BankAccount":
      return <BankingStep claim={claim} onChanged={onChanged} />;
    case "DocumentExtraction":
      return <ExtractionStep claim={claim} onChanged={onChanged} />;
    case "CourierDelivery":
      return <CourierStep claim={claim} onChanged={onChanged} />;
    case "Signature":
      return <SignatureStep claim={claim} onChanged={onChanged} />;
    default:
      return null;
  }
}

function IdentityStep({ claim, onChanged }: { claim: ClaimDetail; onChanged: () => void }) {
  const [selfieId, setSelfieId] = useState<string | null>(null);
  const [idDocId, setIdDocId] = useState<string | null>(null);
  const [isComparing, setIsComparing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function compare() {
    if (!selfieId || !idDocId) return;
    setIsComparing(true);
    setError(null);
    try {
      await api.post(`/claims/${claim.claimId}/verification/face-compare`, {
        selfieDocumentId: selfieId,
        idDocumentDocumentId: idDocId,
      });
      onChanged();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Face comparison failed.");
    } finally {
      setIsComparing(false);
    }
  }

  return (
    <div className="flex flex-col gap-4">
      <DocumentUploadField
        claimId={claim.claimId}
        documentType="IdDocument"
        label="Your ID document"
        helpText="Upload a clear photo of your SA ID card or green book"
        onUploaded={setIdDocId}
      />
      <DocumentUploadField
        claimId={claim.claimId}
        documentType="Selfie"
        label="A selfie"
        helpText="Take a clear photo of your face"
        cameraFacing="user"
        onUploaded={setSelfieId}
      />
      <Button disabled={!selfieId || !idDocId || isComparing} onClick={() => void compare()}>
        {isComparing ? "Comparing…" : "Verify my identity"}
      </Button>
      {error && <p className="font-body text-body-md text-status-error">{error}</p>}
    </div>
  );
}

function BankingStep({ claim, onChanged }: { claim: ClaimDetail; onChanged: () => void }) {
  const [session, setSession] = useState<{ sessionId: string; scopes: string[] } | null>(null);
  const [isBusy, setIsBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function start() {
    setIsBusy(true);
    setError(null);
    try {
      const result = await api.post<{ sessionId: string; requestedScopes: string[] }>(
        `/claims/${claim.claimId}/verification/bank-link/start`,
      );
      setSession({ sessionId: result.sessionId, scopes: result.requestedScopes });
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not start the bank link.");
    } finally {
      setIsBusy(false);
    }
  }

  async function approve() {
    if (!session) return;
    setIsBusy(true);
    setError(null);
    try {
      await api.post(`/claims/${claim.claimId}/verification/bank-link/complete`, { sessionId: session.sessionId });
      onChanged();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not confirm the bank link.");
    } finally {
      setIsBusy(false);
    }
  }

  if (!session) {
    return (
      <Button disabled={isBusy} onClick={() => void start()}>
        {isBusy ? "Starting…" : "Link my bank account"}
      </Button>
    );
  }

  return (
    <div className="flex flex-col gap-4 rounded-md border border-border-hairline bg-bg-base p-4">
      <p className="font-body text-body-md text-ink-primary">This will share, read-only:</p>
      <ul className="list-inside list-disc font-body text-body-md text-ink-secondary">
        {session.scopes.map((scope) => (
          <li key={scope}>{scope}</li>
        ))}
      </ul>
      <Button disabled={isBusy} onClick={() => void approve()}>
        {isBusy ? "Confirming…" : "Approve and continue"}
      </Button>
      {error && <p className="font-body text-body-md text-status-error">{error}</p>}
    </div>
  );
}

function ExtractionStep({ claim, onChanged }: { claim: ClaimDetail; onChanged: () => void }) {
  const documentType = claim.claimType === "DeceasedEstate" ? "LetterOfExecutorship" : "CertificateEvidence";
  const [documentId, setDocumentId] = useState<string | null>(null);
  const [fields, setFields] = useState<Record<string, { value: string; confidence: number }> | null>(null);
  const [edited, setEdited] = useState<Record<string, string>>({});
  const [isBusy, setIsBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function extract() {
    if (!documentId) return;
    setIsBusy(true);
    setError(null);
    try {
      const result = await api.post<{ fields: Record<string, { value: string; confidence: number }> }>(
        `/claims/${claim.claimId}/verification/extract`,
        { documentId },
      );
      setFields(result.fields);
      setEdited(Object.fromEntries(Object.entries(result.fields).map(([k, v]) => [k, v.value])));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not read this document.");
    } finally {
      setIsBusy(false);
    }
  }

  async function confirm() {
    setIsBusy(true);
    setError(null);
    try {
      await api.post(`/claims/${claim.claimId}/verification/extract/confirm`, { confirmedFields: edited });
      onChanged();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not save the confirmed fields.");
    } finally {
      setIsBusy(false);
    }
  }

  if (!fields) {
    return (
      <div className="flex flex-col gap-4">
        <DocumentUploadField
          claimId={claim.claimId}
          documentType={documentType}
          label={documentType === "LetterOfExecutorship" ? "Letter of Executorship" : "Evidence of the certificate"}
          helpText="Upload a clear scan or photo"
          onUploaded={setDocumentId}
        />
        <Button disabled={!documentId || isBusy} onClick={() => void extract()}>
          {isBusy ? "Reading document…" : "Read document"}
        </Button>
        {error && <p className="font-body text-body-md text-status-error">{error}</p>}
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-4">
      <p className="font-body text-body-md text-ink-secondary">Check each field below before confirming.</p>
      {Object.entries(fields).map(([key, field]) => (
        <label key={key} className="flex flex-col gap-1">
          <span className="flex items-center justify-between font-label-md text-label-md text-ink-primary">
            {key}
            <span className="font-label-sm text-label-sm text-ink-secondary">
              {field.confidence.toFixed(0)}% confidence
            </span>
          </span>
          <input
            value={edited[key] ?? ""}
            onChange={(e) => setEdited((prev) => ({ ...prev, [key]: e.target.value }))}
            className="rounded border border-border-hairline bg-bg-surface px-4 py-2 font-mono text-code-md text-ink-primary focus:border-accent-verified"
          />
        </label>
      ))}
      <Button disabled={isBusy} onClick={() => void confirm()}>
        {isBusy ? "Saving…" : "Confirm fields"}
      </Button>
      {error && <p className="font-body text-body-md text-status-error">{error}</p>}
    </div>
  );
}

function InviteExecutorStep({ claim, onChanged }: { claim: ClaimDetail; onChanged: () => void }) {
  const [email, setEmail] = useState("");
  const [isBusy, setIsBusy] = useState(false);
  const [sent, setSent] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function invite() {
    setIsBusy(true);
    setError(null);
    try {
      await api.post(`/claims/${claim.claimId}/executor/invite`, { executorEmail: email });
      setSent(true);
      onChanged();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not send the invitation.");
    } finally {
      setIsBusy(false);
    }
  }

  if (sent) {
    return (
      <p className="font-body text-body-md text-ink-secondary">
        Invitation sent to {email}. We'll update this step once they've verified their identity.
      </p>
    );
  }

  return (
    <div className="flex flex-col gap-3">
      <label className="flex flex-col gap-2">
        <span className="font-label-md text-label-md text-ink-primary">Executor's email address</span>
        <input
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          className="rounded border border-border-hairline bg-bg-surface px-4 py-3 font-body text-body-md text-ink-primary focus:border-accent-verified"
        />
      </label>
      <Button disabled={!email || isBusy} onClick={() => void invite()}>
        {isBusy ? "Sending…" : "Send invitation"}
      </Button>
      {error && <p className="font-body text-body-md text-status-error">{error}</p>}
    </div>
  );
}

function CourierStep({ claim, onChanged }: { claim: ClaimDetail; onChanged: () => void }) {
  const [isBusy, setIsBusy] = useState(false);
  const [waybill, setWaybill] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function book() {
    setIsBusy(true);
    setError(null);
    try {
      const result = await api.post<{ waybillNumber: string }>(`/claims/${claim.claimId}/courier/book`);
      setWaybill(result.waybillNumber);
      onChanged();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not book a collection.");
    } finally {
      setIsBusy(false);
    }
  }

  async function confirmDelivered() {
    setIsBusy(true);
    setError(null);
    try {
      await api.post(`/claims/${claim.claimId}/courier/confirm-delivered`);
      onChanged();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not confirm delivery.");
    } finally {
      setIsBusy(false);
    }
  }

  return (
    <div className="flex flex-col gap-3">
      {!waybill ? (
        <Button disabled={isBusy} onClick={() => void book()}>
          {isBusy ? "Booking…" : "Book courier collection"}
        </Button>
      ) : (
        <>
          <p className="font-mono text-code-md text-ink-secondary">Waybill {waybill}</p>
          <Button disabled={isBusy} onClick={() => void confirmDelivered()}>
            {isBusy ? "Confirming…" : "Confirm original letter received"}
          </Button>
        </>
      )}
      {error && <p className="font-body text-body-md text-status-error">{error}</p>}
    </div>
  );
}

function SignatureStep({ claim, onChanged }: { claim: ClaimDetail; onChanged: () => void }) {
  const [signerName, setSignerName] = useState(claim.shareholderFullName ?? "");
  const [isBusy, setIsBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function submit(pngBase64: string) {
    setIsBusy(true);
    setError(null);
    try {
      await api.post(`/claims/${claim.claimId}/verification/signature`, {
        signerName,
        signatureImageBase64: pngBase64,
      });
      onChanged();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not save your signature.");
    } finally {
      setIsBusy(false);
    }
  }

  return (
    <div className="flex flex-col gap-4">
      <label className="flex flex-col gap-2">
        <span className="font-label-md text-label-md text-ink-primary">Your full name</span>
        <input
          value={signerName}
          onChange={(e) => setSignerName(e.target.value)}
          className="rounded border border-border-hairline bg-bg-surface px-4 py-3 font-body text-body-md text-ink-primary focus:border-accent-verified"
        />
      </label>
      <p className="font-body text-body-md text-ink-secondary">Sign in the box below.</p>
      <SignaturePad onCapture={(png) => void submit(png)} />
      {isBusy && <p className="font-body text-body-md text-ink-secondary">Generating your document…</p>}
      {error && <p className="font-body text-body-md text-status-error">{error}</p>}
    </div>
  );
}

function ConfirmationCard({ claimId }: { claimId: string }) {
  const [url, setUrl] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api
      .get<{ url: string }>(`/claims/${claimId}/document`)
      .then((r) => setUrl(r.url))
      .catch((err) => setError(err instanceof Error ? err.message : "Could not load your document."));
  }, [claimId]);

  return (
    <Card engraved className="text-center">
      <span className="material-symbols-outlined mb-4 text-5xl text-accent-verified">task_alt</span>
      <h2 className="mb-2 font-display text-headline-lg text-ink-primary">Your claim is complete</h2>
      <p className="mb-6 font-body text-body-md text-ink-secondary">
        Every verification step passed and your document has been generated.
      </p>
      {url && (
        <a href={url} target="_blank" rel="noreferrer">
          <Button>Download your document</Button>
        </a>
      )}
      {error && <p className="mt-4 font-body text-body-md text-status-error">{error}</p>}
    </Card>
  );
}
