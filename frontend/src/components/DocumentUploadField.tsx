import { useState } from "react";
import { api, uploadToPresignedUrl } from "../lib/api";
import type { DocumentType } from "../types/domain";

interface DocumentUploadFieldProps {
  claimId: string;
  documentType: DocumentType;
  label: string;
  helpText: string;
  /** Set for the selfie capture, so mobile browsers open the front camera directly. */
  cameraFacing?: "user" | "environment";
  onUploaded: (documentId: string) => void;
}

type Status = "idle" | "uploading" | "done" | "error";

/**
 * Dropzone with hairline dashed border and states for empty / uploading / uploaded /
 * rejected (design.md, Component notes). Uploads go straight to S3 via a presigned
 * URL - see IDocumentStore - so the file's bytes never pass through our API.
 */
export function DocumentUploadField({
  claimId,
  documentType,
  label,
  helpText,
  cameraFacing,
  onUploaded,
}: DocumentUploadFieldProps) {
  const [status, setStatus] = useState<Status>("idle");
  const [error, setError] = useState<string | null>(null);
  const [fileName, setFileName] = useState<string | null>(null);

  async function handleFile(file: File) {
    setStatus("uploading");
    setError(null);
    setFileName(file.name);

    try {
      const upload = await api.post<{ documentId: string; uploadUrl: string; expiresAt: string }>(
        `/claims/${claimId}/documents/upload-url`,
        { documentType, contentType: file.type || "application/octet-stream" },
      );

      await uploadToPresignedUrl(upload.uploadUrl, file);

      const s3Key = new URL(upload.uploadUrl).pathname.replace(/^\//, "");
      await api.post(`/claims/${claimId}/documents/confirm`, {
        documentId: upload.documentId,
        documentType,
        s3Key,
        contentType: file.type || "application/octet-stream",
        sizeBytes: file.size,
      });

      setStatus("done");
      onUploaded(upload.documentId);
    } catch (err) {
      setStatus("error");
      setError(err instanceof Error ? err.message : "The upload failed. Please try again.");
    }
  }

  return (
    <div className="flex flex-col gap-2">
      <span className="font-label-md text-label-md text-ink-primary">{label}</span>
      <label
        className={`flex cursor-pointer flex-col items-center gap-2 rounded-md border-2 border-dashed p-6 text-center transition-colors ${
          status === "done"
            ? "border-accent-verified bg-accent-verified-tint"
            : status === "error"
              ? "border-status-error bg-status-error-tint"
              : "border-border-hairline bg-bg-base hover:border-accent-verified"
        }`}
      >
        <span className="material-symbols-outlined text-3xl text-accent-verified">
          {status === "done" ? "check_circle" : status === "uploading" ? "hourglass_top" : "upload_file"}
        </span>
        <span className="font-body text-body-md text-ink-secondary">
          {status === "uploading" ? "Uploading…" : fileName ?? helpText}
        </span>
        <input
          type="file"
          accept="image/*,application/pdf"
          capture={cameraFacing}
          className="hidden"
          disabled={status === "uploading"}
          onChange={(e) => {
            const file = e.target.files?.[0];
            if (file) void handleFile(file);
          }}
        />
      </label>
      {error && <p className="font-body text-body-md text-status-error">{error}</p>}
    </div>
  );
}
