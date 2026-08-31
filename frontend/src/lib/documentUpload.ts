import { api, uploadToPresignedUrl } from "./api";
import type { DocumentType } from "../types/domain";

/**
 * The presigned-URL upload flow (ask the API for a URL, PUT straight to S3, then
 * confirm) - shared by the plain file dropzone and the live camera capture so both
 * paths stay in sync with the backend contract.
 */
export async function uploadDocument(claimId: string, documentType: DocumentType, file: File): Promise<string> {
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

  return upload.documentId;
}
