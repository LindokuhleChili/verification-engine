import { Link, useNavigate } from "react-router-dom";
import type { ReactNode } from "react";
import { useAuth } from "../hooks/useAuth";

export function PageShell({ children }: { children: ReactNode }) {
  return (
    <div className="flex min-h-screen flex-col bg-bg-base text-ink-primary">
      <TopNav />
      <main className="flex-grow">{children}</main>
      <Footer />
    </div>
  );
}

function TopNav() {
  const { isAuthenticated, signOut } = useAuth();
  const navigate = useNavigate();

  return (
    <nav className="sticky top-0 z-50 w-full border-b border-border-hairline bg-bg-surface">
      <div className="mx-auto flex w-full max-w-7xl items-center justify-between px-margin-mobile py-4 md:px-margin-desktop">
        <div className="flex items-center gap-8">
          <Link to="/" className="font-display text-headline-md font-semibold tracking-tight text-accent-verified">
            Verification Engine
          </Link>
          {isAuthenticated && (
            <Link
              to="/claims"
              className="hidden font-body text-body-md text-ink-secondary transition-colors hover:text-accent-verified md:block"
            >
              My Claims
            </Link>
          )}
        </div>
        <div className="flex items-center gap-4">
          {isAuthenticated ? (
            <button
              onClick={() => {
                void signOut().then(() => navigate("/"));
              }}
              className="font-label-md text-label-md text-ink-secondary transition-colors hover:text-accent-verified"
            >
              Sign out
            </button>
          ) : (
            <Link to="/login" className="font-label-md text-label-md text-accent-verified">
              Sign in
            </Link>
          )}
        </div>
      </div>
    </nav>
  );
}

function Footer() {
  return (
    <footer className="w-full border-t border-border-hairline bg-bg-subtle px-margin-mobile py-8 md:px-margin-desktop">
      <div className="mx-auto flex max-w-7xl flex-col items-center justify-between gap-gutter md:flex-row">
        <div className="flex flex-col items-center gap-2 md:items-start">
          <span className="font-display text-headline-md text-accent-verified">Verification Engine</span>
          <span className="font-body text-label-sm text-ink-secondary">
            A portfolio project. Not a real financial service - see the README for what's real and what's simulated.
          </span>
        </div>
        <nav className="flex flex-wrap justify-center gap-6">
          <span className="font-body text-label-sm text-ink-secondary">POPIA Policy</span>
          <span className="font-body text-label-sm text-ink-secondary">Security Information</span>
        </nav>
      </div>
    </footer>
  );
}
