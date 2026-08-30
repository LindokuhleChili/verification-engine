import { useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { PageShell } from "../components/PageShell";
import { Card } from "../components/Card";
import { Button } from "../components/Button";
import { useAuth } from "../hooks/useAuth";

export function SignUp() {
  const { signUp, confirmSignUp, signIn } = useAuth();
  const navigate = useNavigate();

  const [step, setStep] = useState<"details" | "confirm">("details");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [code, setCode] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSignUp(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setIsSubmitting(true);
    try {
      await signUp(email, password);
      setStep("confirm");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not create your account.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function handleConfirm(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setIsSubmitting(true);
    try {
      await confirmSignUp(email, code);
      await signIn(email, password);
      navigate("/claims");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not confirm your account. Check the code and try again.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <PageShell>
      <div className="mx-auto flex max-w-form flex-col items-center px-margin-mobile py-24">
        <Card className="w-full max-w-md">
          {step === "details" ? (
            <>
              <h1 className="mb-1 font-display text-headline-lg text-ink-primary">Create your account</h1>
              <p className="mb-6 font-body text-body-md text-ink-secondary">
                You'll verify your email, then your identity, before any claim can be submitted.
              </p>
              <form onSubmit={handleSignUp} className="flex flex-col gap-4">
                <label className="flex flex-col gap-2">
                  <span className="font-label-md text-label-md text-ink-primary">Email address</span>
                  <input
                    type="email"
                    required
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    className="rounded border border-border-hairline bg-bg-surface px-4 py-3 font-body text-body-md text-ink-primary focus:border-accent-verified"
                  />
                </label>
                <label className="flex flex-col gap-2">
                  <span className="font-label-md text-label-md text-ink-primary">Password</span>
                  <input
                    type="password"
                    required
                    minLength={8}
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    className="rounded border border-border-hairline bg-bg-surface px-4 py-3 font-body text-body-md text-ink-primary focus:border-accent-verified"
                  />
                  <span className="font-body text-label-sm text-ink-secondary">
                    At least 8 characters, with an uppercase letter, a lowercase letter, and a digit.
                  </span>
                </label>

                {error && <p className="font-body text-body-md text-status-error">{error}</p>}

                <Button type="submit" disabled={isSubmitting} className="mt-2 w-full">
                  {isSubmitting ? "Creating account…" : "Create account"}
                </Button>
              </form>
            </>
          ) : (
            <>
              <h1 className="mb-1 font-display text-headline-lg text-ink-primary">Check your email</h1>
              <p className="mb-6 font-body text-body-md text-ink-secondary">
                We sent a verification code to {email}. Enter it below to confirm your account.
              </p>
              <form onSubmit={handleConfirm} className="flex flex-col gap-4">
                <label className="flex flex-col gap-2">
                  <span className="font-label-md text-label-md text-ink-primary">Verification code</span>
                  <input
                    type="text"
                    inputMode="numeric"
                    required
                    value={code}
                    onChange={(e) => setCode(e.target.value)}
                    className="rounded border border-border-hairline bg-bg-surface px-4 py-3 font-mono text-code-md text-ink-primary focus:border-accent-verified"
                  />
                </label>

                {error && <p className="font-body text-body-md text-status-error">{error}</p>}

                <Button type="submit" disabled={isSubmitting} className="mt-2 w-full">
                  {isSubmitting ? "Confirming…" : "Confirm and sign in"}
                </Button>
              </form>
            </>
          )}

          <p className="mt-6 text-center font-body text-body-md text-ink-secondary">
            Already have an account?{" "}
            <Link to="/login" className="text-accent-verified">
              Sign in
            </Link>
          </p>
        </Card>
      </div>
    </PageShell>
  );
}
