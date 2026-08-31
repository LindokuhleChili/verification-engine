import { useEffect, useRef, useState } from "react";
import { uploadDocument } from "../lib/documentUpload";
import type { DocumentType } from "../types/domain";

interface SelfieCameraFieldProps {
  claimId: string;
  documentType: DocumentType;
  label: string;
  helpText: string;
  onUploaded: (documentId: string) => void;
}

type Phase = "idle" | "starting" | "live" | "captured" | "uploading" | "done" | "error" | "unavailable";

/**
 * Live front-camera capture for the selfie step, the way a banking app's KYC flow
 * does it, instead of routing through the OS file picker. Falls back to the plain
 * file input whenever getUserMedia is unsupported, blocked, or denied - a locked-down
 * laptop or a browser without camera permission still has to be able to finish this
 * step.
 */
export function SelfieCameraField({ claimId, documentType, label, helpText, onUploaded }: SelfieCameraFieldProps) {
  const videoRef = useRef<HTMLVideoElement>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const streamRef = useRef<MediaStream | null>(null);
  const [phase, setPhase] = useState<Phase>("idle");
  const [error, setError] = useState<string | null>(null);
  const [capturedUrl, setCapturedUrl] = useState<string | null>(null);
  const [capturedBlob, setCapturedBlob] = useState<Blob | null>(null);

  useEffect(() => {
    return () => {
      streamRef.current?.getTracks().forEach((track) => track.stop());
      if (capturedUrl) URL.revokeObjectURL(capturedUrl);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  function stopStream() {
    streamRef.current?.getTracks().forEach((track) => track.stop());
    streamRef.current = null;
  }

  async function startCamera() {
    setError(null);
    setPhase("starting");
    if (!navigator.mediaDevices?.getUserMedia) {
      setPhase("unavailable");
      return;
    }
    try {
      const stream = await navigator.mediaDevices.getUserMedia({
        video: { facingMode: "user", width: { ideal: 720 }, height: { ideal: 720 } },
      });
      streamRef.current = stream;
      if (videoRef.current) {
        videoRef.current.srcObject = stream;
        await videoRef.current.play();
      }
      setPhase("live");
    } catch {
      setPhase("unavailable");
    }
  }

  function capture() {
    const video = videoRef.current;
    const canvas = canvasRef.current;
    if (!video || !canvas) return;

    const size = Math.min(video.videoWidth, video.videoHeight);
    canvas.width = size;
    canvas.height = size;
    const ctx = canvas.getContext("2d");
    if (!ctx) return;

    // Crop to a centered square so a wide sensor doesn't stretch the photo.
    const sx = (video.videoWidth - size) / 2;
    const sy = (video.videoHeight - size) / 2;
    ctx.drawImage(video, sx, sy, size, size, 0, 0, size, size);

    canvas.toBlob(
      (blob) => {
        if (!blob) return;
        stopStream();
        setCapturedBlob(blob);
        setCapturedUrl(URL.createObjectURL(blob));
        setPhase("captured");
      },
      "image/jpeg",
      0.92,
    );
  }

  function retake() {
    if (capturedUrl) URL.revokeObjectURL(capturedUrl);
    setCapturedUrl(null);
    setCapturedBlob(null);
    void startCamera();
  }

  async function usePhoto() {
    if (!capturedBlob) return;
    setPhase("uploading");
    setError(null);
    try {
      const file = new File([capturedBlob], "selfie.jpg", { type: "image/jpeg" });
      const documentId = await uploadDocument(claimId, documentType, file);
      setPhase("done");
      onUploaded(documentId);
    } catch (err) {
      setPhase("error");
      setError(err instanceof Error ? err.message : "The upload failed. Please try again.");
    }
  }

  async function handleFileFallback(file: File) {
    setPhase("uploading");
    setError(null);
    try {
      const documentId = await uploadDocument(claimId, documentType, file);
      setPhase("done");
      onUploaded(documentId);
    } catch (err) {
      setPhase("error");
      setError(err instanceof Error ? err.message : "The upload failed. Please try again.");
    }
  }

  return (
    <div className="flex flex-col gap-2">
      <span className="font-label-md text-label-md text-ink-primary">{label}</span>

      <div className="flex flex-col items-center gap-3 rounded-md border-2 border-dashed border-border-hairline bg-bg-base p-4">
        {phase === "idle" && (
          <>
            <span className="material-symbols-outlined text-3xl text-accent-verified">photo_camera</span>
            <p className="text-center font-body text-body-md text-ink-secondary">{helpText}</p>
            <button
              type="button"
              onClick={() => void startCamera()}
              className="rounded bg-accent-verified px-6 py-3 font-label-md text-label-md text-white transition-colors hover:bg-[#0c5a4b]"
            >
              Turn on camera
            </button>
          </>
        )}

        {phase === "starting" && (
          <p className="font-body text-body-md text-ink-secondary">Requesting camera access…</p>
        )}

        {phase === "live" && (
          <>
            <div className="relative aspect-square w-full max-w-xs overflow-hidden rounded-full border-2 border-accent-verified">
              <video ref={videoRef} muted playsInline className="h-full w-full object-cover" />
            </div>
            <p className="font-body text-body-sm text-ink-secondary">Centre your face in the circle</p>
            <button
              type="button"
              onClick={capture}
              className="rounded bg-accent-verified px-6 py-3 font-label-md text-label-md text-white transition-colors hover:bg-[#0c5a4b]"
            >
              Capture
            </button>
          </>
        )}

        {(phase === "captured" || phase === "uploading" || phase === "error") && capturedUrl && (
          <>
            <div className="aspect-square w-full max-w-xs overflow-hidden rounded-full border-2 border-accent-verified">
              <img src={capturedUrl} alt="Captured selfie" className="h-full w-full object-cover" />
            </div>
            <div className="flex gap-3">
              <button
                type="button"
                disabled={phase === "uploading"}
                onClick={retake}
                className="rounded border border-border-hairline bg-bg-surface px-6 py-3 font-label-md text-label-md text-ink-primary transition-colors hover:bg-bg-subtle disabled:cursor-not-allowed"
              >
                Retake
              </button>
              <button
                type="button"
                disabled={phase === "uploading"}
                onClick={() => void usePhoto()}
                className="rounded bg-accent-verified px-6 py-3 font-label-md text-label-md text-white transition-colors hover:bg-[#0c5a4b] disabled:bg-ink-secondary/40 disabled:cursor-not-allowed"
              >
                {phase === "uploading" ? "Uploading…" : "Use this photo"}
              </button>
            </div>
          </>
        )}

        {phase === "done" && (
          <>
            <span className="material-symbols-outlined text-3xl text-accent-verified">check_circle</span>
            <p className="font-body text-body-md text-ink-secondary">Selfie captured</p>
          </>
        )}

        {phase === "unavailable" && (
          <>
            <p className="text-center font-body text-body-md text-ink-secondary">
              Couldn't reach the camera. You can upload a photo instead.
            </p>
            <label className="cursor-pointer rounded border border-border-hairline bg-bg-surface px-6 py-3 font-label-md text-label-md text-ink-primary transition-colors hover:bg-bg-subtle">
              Choose a photo
              <input
                type="file"
                accept="image/*"
                capture="user"
                className="hidden"
                onChange={(e) => {
                  const file = e.target.files?.[0];
                  if (file) void handleFileFallback(file);
                }}
              />
            </label>
          </>
        )}

        {error && <p className="font-body text-body-md text-status-error">{error}</p>}
      </div>

      <canvas ref={canvasRef} className="hidden" />
    </div>
  );
}
