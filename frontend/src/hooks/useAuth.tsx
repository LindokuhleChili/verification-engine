import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import {
  confirmSignUp as amplifyConfirmSignUp,
  fetchAuthSession,
  getCurrentUser,
  signIn as amplifySignIn,
  signOut as amplifySignOut,
  signUp as amplifySignUp,
} from "aws-amplify/auth";
import "../lib/amplify";

interface AuthState {
  isLoading: boolean;
  isAuthenticated: boolean;
  email: string | null;
  signUp: (email: string, password: string) => Promise<void>;
  confirmSignUp: (email: string, code: string) => Promise<void>;
  signIn: (email: string, password: string) => Promise<void>;
  signOut: () => Promise<void>;
  /** The Cognito access token API Gateway's JWT authorizer expects on every authenticated request. */
  getAccessToken: () => Promise<string | null>;
}

const AuthContext = createContext<AuthState | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [isLoading, setIsLoading] = useState(true);
  const [email, setEmail] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    try {
      const user = await getCurrentUser();
      setEmail(user.signInDetails?.loginId ?? user.username);
    } catch {
      setEmail(null);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  const value = useMemo<AuthState>(
    () => ({
      isLoading,
      isAuthenticated: email !== null,
      email,

      signUp: async (userEmail, password) => {
        await amplifySignUp({
          username: userEmail,
          password,
          options: { userAttributes: { email: userEmail } },
        });
      },

      confirmSignUp: async (userEmail, code) => {
        await amplifyConfirmSignUp({ username: userEmail, confirmationCode: code });
      },

      signIn: async (userEmail, password) => {
        await amplifySignIn({ username: userEmail, password });
        await refresh();
      },

      signOut: async () => {
        await amplifySignOut();
        setEmail(null);
      },

      getAccessToken: async () => {
        try {
          const session = await fetchAuthSession();
          return session.tokens?.accessToken?.toString() ?? null;
        } catch {
          return null;
        }
      },
    }),
    [isLoading, email, refresh],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthState {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth must be used within an AuthProvider.");
  return context;
}
