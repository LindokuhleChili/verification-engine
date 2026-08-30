import type { ButtonHTMLAttributes, ReactNode } from "react";

type Variant = "primary" | "secondary" | "ghost";

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant;
  children: ReactNode;
}

const VARIANT_CLASSES: Record<Variant, string> = {
  // Solid accent-verified for primary actions - no gradient fills (design.md, Component notes).
  primary:
    "bg-accent-verified text-white hover:bg-[#0c5a4b] disabled:bg-ink-secondary/40 disabled:cursor-not-allowed",
  secondary: "border border-border-hairline bg-bg-surface text-ink-primary hover:bg-bg-subtle",
  ghost: "text-accent-verified hover:bg-accent-verified-tint",
};

export function Button({ variant = "primary", className = "", children, ...props }: ButtonProps) {
  return (
    <button
      className={`inline-flex items-center justify-center gap-2 rounded px-6 py-3 font-label-md text-label-md transition-colors ${VARIANT_CLASSES[variant]} ${className}`}
      {...props}
    >
      {children}
    </button>
  );
}
