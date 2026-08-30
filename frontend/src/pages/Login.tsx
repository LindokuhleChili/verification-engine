import { useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { PageShell } from "../components/PageShell";
import { Card } from "../components/Card";
import { Button } from "../components/Button";
import { useAuth } from "../hooks/useAuth";

export function Login() {
  const { signIn } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setIsSubmitting(true);
    try {
      await signIn(email, password);
      navigate("/claims");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not sign in. Check your email and password.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <PageShell>
      <div className="mx-auto flex max-w-form flex-col items-center px-margin-mobile py-24">
        <Card className="w-full max-w-md">
          <h1 className="mb-1 font-display text-headline-lg text-ink-primary">Sign in</h1>
          <p className="mb-6 font-body text-body-md text-ink-secondary">Continue to your claims dashboard.</p>

          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
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
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className="rounded border border-border-hairline bg-bg-surface px-4 py-3 font-body text-body-md text-ink-primary focus:border-accent-verified"
              />
            </label>

            {error && <p className="font-body text-body-md text-status-error">{error}</p>}

            <Button type="submit" disabled={isSubmitting} className="mt-2 w-full">
              {isSubmitting ? "Signing in…" : "Sign in"}
            </Button>
          </form>

          <p className="mt-6 text-center font-body text-body-md text-ink-secondary">
            No account yet?{" "}
            <Link to="/signup" className="text-accent-verified">
              Create one
            </Link>
          </p>
        </Card>
      </div>
    </PageShell>
  );
}
