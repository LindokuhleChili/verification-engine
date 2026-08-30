import { useRef, useState } from "react";

interface SignaturePadProps {
  onCapture: (pngBase64: string) => void;
}

/**
 * A minimal drawn-signature capture. This is deliberately not an accredited Advanced
 * Electronic Signature (LawTrust and similar require paid, vetted integration - see
 * README) - what it produces is hashed and timestamped server-side into a genuine,
 * independently-verifiable audit trail, just not a legally accredited one.
 */
export function SignaturePad({ onCapture }: SignaturePadProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const isDrawing = useRef(false);
  const [hasDrawn, setHasDrawn] = useState(false);

  function getContext() {
    const canvas = canvasRef.current;
    if (!canvas) return null;
    return canvas.getContext("2d");
  }

  function pointerPosition(canvas: HTMLCanvasElement, event: React.PointerEvent) {
    const rect = canvas.getBoundingClientRect();
    return { x: event.clientX - rect.left, y: event.clientY - rect.top };
  }

  function handlePointerDown(event: React.PointerEvent<HTMLCanvasElement>) {
    const canvas = canvasRef.current;
    const ctx = getContext();
    if (!canvas || !ctx) return;

    isDrawing.current = true;
    const { x, y } = pointerPosition(canvas, event);
    ctx.beginPath();
    ctx.moveTo(x, y);
  }

  function handlePointerMove(event: React.PointerEvent<HTMLCanvasElement>) {
    const canvas = canvasRef.current;
    const ctx = getContext();
    if (!canvas || !ctx || !isDrawing.current) return;

    const { x, y } = pointerPosition(canvas, event);
    ctx.strokeStyle = "#1B2430";
    ctx.lineWidth = 2;
    ctx.lineCap = "round";
    ctx.lineTo(x, y);
    ctx.stroke();
    setHasDrawn(true);
  }

  function handlePointerUp() {
    isDrawing.current = false;
  }

  function clear() {
    const canvas = canvasRef.current;
    const ctx = getContext();
    if (!canvas || !ctx) return;
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    setHasDrawn(false);
  }

  function capture() {
    const canvas = canvasRef.current;
    if (!canvas || !hasDrawn) return;
    const dataUrl = canvas.toDataURL("image/png");
    onCapture(dataUrl.replace(/^data:image\/png;base64,/, ""));
  }

  return (
    <div className="flex flex-col gap-3">
      <canvas
        ref={canvasRef}
        width={480}
        height={160}
        onPointerDown={handlePointerDown}
        onPointerMove={handlePointerMove}
        onPointerUp={handlePointerUp}
        onPointerLeave={handlePointerUp}
        className="w-full touch-none rounded border border-dashed border-border-hairline bg-bg-base"
      />
      <div className="flex gap-3">
        <button type="button" onClick={clear} className="font-label-sm text-label-sm text-ink-secondary underline">
          Clear
        </button>
        <button
          type="button"
          disabled={!hasDrawn}
          onClick={capture}
          className="ml-auto rounded bg-accent-verified px-4 py-2 font-label-sm text-label-sm text-white disabled:opacity-40"
        >
          Use this signature
        </button>
      </div>
    </div>
  );
}
