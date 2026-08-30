import { fetchAuthSession } from "aws-amplify/auth";
import { config } from "./config";

export class ApiError extends Error {
  status: number;

  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

/**
 * A thin fetch wrapper, not a generated client: this project's API surface is small
 * enough that a typed generator would be more ceremony than it saves. Every call
 * attaches the current Cognito access token - the same token API Gateway's JWT
 * authorizer validates before a request ever reaches Lambda (see
 * VerificationEngineStack.Api.cs and VerificationEngine.Api.Security.CurrentUser).
 */
async function request<T>(method: string, path: string, body?: unknown): Promise<T> {
  const session = await fetchAuthSession();
  const token = session.tokens?.accessToken?.toString();

  const response = await fetch(`${config.apiBaseUrl}${path}`, {
    method,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  });

  if (!response.ok) {
    const problem = await response.json().catch(() => null);
    throw new ApiError(response.status, problem?.error ?? `Request to ${path} failed with status ${response.status}.`);
  }

  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

export const api = {
  get: <T,>(path: string) => request<T>("GET", path),
  post: <T,>(path: string, body?: unknown) => request<T>("POST", path, body),
};

/**
 * Uploads directly to S3 using a presigned URL - bytes never pass through our own API,
 * which keeps large document uploads off both the API Gateway payload limit and the
 * Lambda duration bill. Not routed through the `api` helper above since S3 expects the
 * raw file body, not JSON, and no Authorization header (the presigned URL itself is
 * the credential).
 */
export async function uploadToPresignedUrl(uploadUrl: string, file: File): Promise<void> {
  const response = await fetch(uploadUrl, {
    method: "PUT",
    headers: { "Content-Type": file.type },
    body: file,
  });

  if (!response.ok) {
    throw new ApiError(response.status, "The file upload to storage failed. Please try again.");
  }
}
