import type { HTMLAttributes, ReactNode } from "react";

interface CardProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode;
  /** The one deliberate flourish (design.md, "Signature element") - only for a Verified
   *  status card or the final claim-confirmation card. Never anywhere else. */
  engraved?: boolean;
}

/** 1px hairline border, no heavy drop shadow, 12px radius - design.md's card spec. */
export function Card({ children, engraved = false, className = "", ...props }: CardProps) {
  return (
    <div
      className={`rounded-lg border border-border-hairline bg-bg-surface p-6 shadow-[0_4px_12px_rgba(27,36,48,0.05)] ${
        engraved ? "engraved-corners" : ""
      } ${className}`}
      {...props}
    >
      {children}
    </div>
  );
}
