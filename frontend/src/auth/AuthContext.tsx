import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { ApiError, setUnauthorisedListener } from "../api/client";
import { authApi } from "../api/endpoints";
import type { User } from "../api/types";

type AuthState =
  | { status: "loading"; user: null }
  | { status: "signed-out"; user: null; sessionExpired: boolean }
  | { status: "signed-in"; user: User };

interface AuthContextValue {
  state: AuthState;
  signIn: (username: string, password: string) => Promise<User>;
  signOut: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const [state, setState] = useState<AuthState>({ status: "loading", user: null });

  // On load, ask the API who we are. The session cookie is HttpOnly, so this is the only way to know.
  useEffect(() => {
    let cancelled = false;
    authApi
      .me()
      .then((user) => !cancelled && setState({ status: "signed-in", user }))
      .catch(() => !cancelled && setState({ status: "signed-out", user: null, sessionExpired: false }));
    return () => {
      cancelled = true;
    };
  }, []);

  // Any 401 from the API means the session ended (timed out, or the account was disabled).
  useEffect(() => {
    setUnauthorisedListener(() => {
      queryClient.clear();
      setState({ status: "signed-out", user: null, sessionExpired: true });
    });
    return () => setUnauthorisedListener(null);
  }, [queryClient]);

  const signIn = useCallback(async (username: string, password: string) => {
    const user = await authApi.login(username, password);
    queryClient.clear();
    setState({ status: "signed-in", user });
    return user;
  }, [queryClient]);

  const signOut = useCallback(async () => {
    try {
      await authApi.logout();
    } catch (error) {
      if (!(error instanceof ApiError)) throw error;
    } finally {
      queryClient.clear();
      setState({ status: "signed-out", user: null, sessionExpired: false });
    }
  }, [queryClient]);

  const value = useMemo(() => ({ state, signIn, signOut }), [state, signIn, signOut]);
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth must be used inside AuthProvider");
  return context;
}

/** The signed-in user. Only use inside routes protected by RequireRole. */
export function useCurrentUser(): User {
  const { state } = useAuth();
  if (state.status !== "signed-in") throw new Error("No signed-in user");
  return state.user;
}
