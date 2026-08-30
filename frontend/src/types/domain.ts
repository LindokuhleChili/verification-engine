/**
 * Mirrors the C# contracts in backend/VerificationEngine.Api/Contracts and the enums
 * in backend/VerificationEngine.Domain/Claims. Kept as plain string-literal unions
 * (not TypeScript enums) so they serialize/compare identically to the JSON the API
 * actually sends - System.Text.Json writes C# enums as their member names by default.
 */

export type ClaimType = "LivingShareholder" | "DeceasedEstate" | "LostCertificate";

export type ClaimStatus = "Draft" | "Pending" | "Verified" | "ActionNeeded" | "Complete" | "Rejected";

export type VerificationStepName =
  | "IdentityBiometric"
  | "BankAccount"
  | "DocumentExtraction"
  | "ExecutorIdentity"
  | "CourierDelivery"
  | "Signature";

export type VerificationStepStatus = "NotStarted" | "InProgress" | "Passed" | "Failed" | "AwaitingCounterparty";

export type DocumentType =
  | "Selfie"
  | "IdDocument"
  | "LetterOfExecutorship"
  | "DeathCertificate"
  | "CertificateEvidence"
  | "ProofOfAddress";

export interface ClaimSummary {
  claimId: string;
  claimType: ClaimType;
  status: ClaimStatus;
  companyName: string | null;
  amountCents: number | null;
  createdAt: string;
  updatedAt: string;
}

export interface StepResponse {
  name: VerificationStepName;
  label: string;
  status: VerificationStepStatus;
  detail: string | null;
  confidenceScore: number | null;
}

export interface DocumentResponse {
  documentId: string;
  documentType: DocumentType;
  contentType: string;
  sizeBytes: number;
  uploadedAt: string;
  rejectionReason: string | null;
}

export interface ClaimDetail {
  claimId: string;
  claimType: ClaimType;
  status: ClaimStatus;
  shareholderFullName: string | null;
  shareholderIdNumber: string | null;
  companyName: string | null;
  amountCents: number | null;
  certificateNumber: string | null;
  mastersReferenceNumber: string | null;
  createdAt: string;
  updatedAt: string;
  submittedAt: string | null;
  canSubmit: boolean;
  /** False means the viewer reached this claim as the invited executor, not its creator. */
  isOwner: boolean;
  steps: StepResponse[];
  documents: DocumentResponse[];
}

/** Mirrors VerificationEngine.Domain.Claims.ClaimWorkflow - which steps each claim type needs, in order. */
export const CLAIM_WORKFLOW: Record<ClaimType, VerificationStepName[]> = {
  LivingShareholder: ["IdentityBiometric", "BankAccount", "Signature"],
  DeceasedEstate: ["IdentityBiometric", "DocumentExtraction", "ExecutorIdentity", "CourierDelivery", "Signature"],
  LostCertificate: ["IdentityBiometric", "DocumentExtraction", "Signature"],
};

export const STEP_LABELS: Record<VerificationStepName, string> = {
  IdentityBiometric: "Identity",
  BankAccount: "Banking",
  DocumentExtraction: "Documents",
  ExecutorIdentity: "Executor",
  CourierDelivery: "Courier",
  Signature: "Sign",
};

export const CLAIM_TYPE_LABELS: Record<ClaimType, string> = {
  LivingShareholder: "Living Shareholder",
  DeceasedEstate: "Deceased Estate",
  LostCertificate: "Lost Certificate",
};

export function formatRands(cents: number | null): string {
  if (cents === null) return "—";
  return `R ${(cents / 100).toLocaleString("en-ZA", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}
