import { Navigate, Outlet, useLocation } from "react-router";
import type { UserRole } from "../api/types";
import { Loading } from "../components/Loading";
import { useAuth } from "./AuthContext";

/**
 * Route guard. Not signed in: go to sign in, then come back. Wrong role: go to that role's home page.
 * This only shapes the UI. The API enforces the same rules on every call.
 */
export function RequireRole({ allow }: { allow: UserRole }) {
  const { state } = useAuth();
  const location = useLocation();

  if (state.status === "loading") return <Loading label="Checking your session" />;

  if (state.status === "signed-out") {
    const returnTo = encodeURIComponent(location.pathname + location.search);
    const reason = state.sessionExpired ? "&reason=expired" : "";
    return <Navigate to={`/sign-in?returnTo=${returnTo}${reason}`} replace />;
  }

  if (state.user.role !== allow) return <Navigate to={homeFor(state.user.role)} replace />;

  return <Outlet />;
}

export function homeFor(role: UserRole): string {
  return role === "Administrator" ? "/admin/requests" : "/requests";
}

/** Sends "/" to the right home page for the signed-in user. */
export function HomeRedirect() {
  const { state } = useAuth();
  if (state.status === "loading") return <Loading label="Checking your session" />;
  if (state.status === "signed-out") return <Navigate to="/sign-in" replace />;
  return <Navigate to={homeFor(state.user.role)} replace />;
}
