import { Link } from "react-router-dom";
import { PageShell } from "../components/PageShell";
import { useAuth } from "../hooks/useAuth";
import heroImage from "../assets/landing-hero.jpg";

export function Landing() {
  const { isAuthenticated } = useAuth();

  return (
    <PageShell>
      <section className="relative overflow-hidden px-margin-mobile pb-32 pt-24 md:px-margin-desktop">
        <div
          className="pointer-events-none absolute inset-0 opacity-50"
          style={{
            backgroundImage: "radial-gradient(#DDE3E0 1px, transparent 1px)",
            backgroundSize: "24px 24px",
          }}
        />
        <div className="relative z-10 mx-auto grid max-w-7xl grid-cols-1 items-center gap-12 lg:grid-cols-12">
          <div className="flex flex-col items-start gap-6 lg:col-span-6">
            <div className="mb-2 inline-flex items-center gap-2 rounded-full border border-accent-verified/10 bg-accent-verified-tint px-3 py-1">
              <span className="h-2 w-2 rounded-full bg-accent-verified" />
              <span className="font-label-sm text-label-sm uppercase tracking-wider text-accent-verified">
                Secure Dividends Recovery
              </span>
            </div>
            <h1 className="font-display text-display-lg leading-tight text-ink-primary">
              Claim what's yours, <br />
              <span className="text-accent-verified">without the paperwork.</span>
            </h1>
            <p className="mt-2 max-w-lg font-body text-body-lg text-ink-secondary">
              We assist South African shareholders in recovering unpaid dividends and dormant shares. By
              using biometric identity verification and open banking, a process that used to take months
              becomes a matter of minutes.
            </p>
            <div className="mt-8 flex w-full flex-col gap-4 sm:w-auto sm:flex-row">
              <Link
                to={isAuthenticated ? "/claims/new" : "/signup"}
                className="inline-flex transform items-center justify-center rounded bg-accent-verified px-8 py-4 font-label-md text-label-md text-white shadow-sm transition-all hover:-translate-y-0.5 hover:bg-[#0c5a4b] hover:shadow-md"
              >
                Start your claim
              </Link>
              <a
                href="#how-it-works"
                className="inline-flex items-center justify-center rounded border border-border-hairline bg-bg-surface px-8 py-4 font-label-md text-label-md text-ink-primary transition-colors hover:bg-bg-subtle"
              >
                Learn more
              </a>
            </div>
          </div>
          <div className="relative lg:col-span-6">
            <div className="relative mx-auto aspect-square w-full max-w-lg overflow-hidden rounded-xl border border-border-hairline bg-bg-surface p-8 shadow-[0px_8px_24px_rgba(27,36,48,0.08)]">
              <div className="pointer-events-none absolute inset-0 bg-gradient-to-tr from-bg-subtle to-bg-surface" />
              <img
                src={heroImage}
                alt="Abstract render of overlapping glass and marble planes in white and emerald green, evoking secure digital identity verification"
                className="relative z-10 h-full w-full rounded-lg object-cover mix-blend-multiply"
              />
            </div>
          </div>
        </div>
      </section>

      <section id="how-it-works" className="border-y border-border-hairline bg-bg-surface px-margin-mobile py-24 md:px-margin-desktop">
        <div className="mx-auto max-w-7xl">
          <div className="mb-16 max-w-2xl">
            <h2 className="font-display text-display-md text-ink-primary">
              Precision verification.
              <br />
              Zero physical forms.
            </h2>
            <p className="mt-4 font-body text-body-lg text-ink-secondary">
              Our engine connects identity verification and document extraction with institutional-grade
              process, so your claim is handled securely and definitively.
            </p>
          </div>
          <div className="grid grid-cols-1 gap-6 md:grid-cols-3">
            {[
              {
                icon: "fingerprint",
                title: "Biometric Identity",
                body: "We confirm your identity by comparing a live photo against your ID document, directly from your device.",
              },
              {
                icon: "account_balance",
                title: "Open Banking Link",
                body: "Securely confirm your South African bank account. We never see your banking credentials.",
              },
              {
                icon: "task_alt",
                title: "Automated Documents",
                body: "Once verified, we generate and store the exact legal form your claim type requires.",
              },
            ].map((item) => (
              <div
                key={item.title}
                className="flex flex-col gap-6 rounded-lg border border-border-hairline bg-bg-base p-8 transition-shadow hover:shadow-[0px_4px_12px_rgba(27,36,48,0.05)]"
              >
                <div className="flex h-12 w-12 items-center justify-center rounded-md bg-bg-subtle text-accent-verified">
                  <span className="material-symbols-outlined text-2xl">{item.icon}</span>
                </div>
                <div>
                  <h3 className="mb-2 font-display text-headline-md text-ink-primary">{item.title}</h3>
                  <p className="font-body text-body-md text-ink-secondary">{item.body}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="bg-bg-subtle px-margin-mobile py-32 md:px-margin-desktop">
        <div className="mx-auto flex max-w-form flex-col items-center text-center">
          <span className="material-symbols-outlined mb-6 text-4xl text-accent-verified">shield_lock</span>
          <h2 className="mb-6 font-display text-display-md text-ink-primary">Built on Institutional Trust</h2>
          <p className="mb-12 font-body text-body-lg text-ink-secondary">
            Verification Engine is built for data privacy and regulatory compliance. Your information is
            treated with the same care required of financial institutions.
          </p>
          <div className="grid w-full grid-cols-1 gap-4 text-left sm:grid-cols-2">
            <div className="flex items-start gap-4 rounded-md border border-border-hairline bg-bg-surface p-6 shadow-sm">
              <span className="material-symbols-outlined mt-1 text-accent-verified">verified_user</span>
              <div>
                <h4 className="font-label-md text-label-md text-ink-primary">POPIA Compliant</h4>
                <p className="mt-1 font-body text-body-md text-sm text-ink-secondary">
                  Strict adherence to the Protection of Personal Information Act. We collect only what a
                  claim legally requires.
                </p>
              </div>
            </div>
            <div className="flex items-start gap-4 rounded-md border border-border-hairline bg-bg-surface p-6 shadow-sm">
              <span className="material-symbols-outlined mt-1 text-accent-verified">enhanced_encryption</span>
              <div>
                <h4 className="font-label-md text-label-md text-ink-primary">Encrypted at Rest &amp; in Transit</h4>
                <p className="mt-1 font-body text-body-md text-sm text-ink-secondary">
                  Documents live in a private store behind short-lived, single-use links - never a public
                  URL.
                </p>
              </div>
            </div>
          </div>
        </div>
      </section>
    </PageShell>
  );
}
