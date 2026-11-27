/**
 * Thin wrapper around fetch for the Clinic Space API.
 * - Same-origin requests only, so the HttpOnly session cookie is sent automatically.
 * - Copies the XSRF-TOKEN cookie into the X-XSRF-TOKEN header on state-changing calls.
 * - Turns RFC 9457 problem details into an ApiError the UI can show.
 */

export interface ProblemDetails {
  status?: number;
  title?: string;
  detail?: string;
  code?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  readonly status: number;
  readonly problem: ProblemDetails;

  constructor(status: number, problem: ProblemDetails) {
    super(problem.detail ?? problem.title ?? `Request failed with status ${status}`);
    this.name = "ApiError";
    this.status = status;
    this.problem = problem;
  }

  /** Field errors keyed by camelCase property name, as returned by the API. */
  get fieldErrors(): Record<string, string> {
    const result: Record<string, string> = {};
    for (const [key, messages] of Object.entries(this.problem.errors ?? {})) {
      const first = messages[0];
      if (first) result[key.charAt(0).toLowerCase() + key.slice(1)] = first;
    }
    return result;
  }
}

type UnauthorisedListener = () => void;
let onUnauthorised: UnauthorisedListener | null = null;

/** Lets the auth provider react when the session has expired (any 401). */
export function setUnauthorisedListener(listener: UnauthorisedListener | null) {
  onUnauthorised = listener;
}

export function readCookie(name: string): string | null {
  const match = document.cookie.split("; ").find((c) => c.startsWith(`${name}=`));
  return match ? decodeURIComponent(match.slice(name.length + 1)) : null;
}

let csrfPromise: Promise<void> | null = null;

/** Makes sure an anti-forgery token cookie exists before the first POST. */
async function ensureCsrfToken(): Promise<void> {
  if (readCookie("XSRF-TOKEN")) return;
  csrfPromise ??= fetch("/api/auth/csrf", { credentials: "same-origin" }).then(() => {
    csrfPromise = null;
  });
  await csrfPromise;
}

export async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  const method = (init.method ?? "GET").toUpperCase();
  const headers = new Headers(init.headers);
  headers.set("Accept", "application/json");
  if (init.body !== undefined) headers.set("Content-Type", "application/json");

  if (method !== "GET" && method !== "HEAD") {
    await ensureCsrfToken();
    const token = readCookie("XSRF-TOKEN");
    if (token) headers.set("X-XSRF-TOKEN", token);
  }

  let response: Response;
  try {
    response = await fetch(path, { ...init, method, headers, credentials: "same-origin" });
  } catch {
    throw new ApiError(0, {
      title: "Network error",
      detail: "We could not reach the service. Check your connection and try again.",
      code: "network_error",
    });
  }

  if (response.status === 204) return undefined as T;

  const isJson = response.headers.get("content-type")?.includes("json") ?? false;
  const body: unknown = isJson ? await response.json().catch(() => null) : null;

  if (!response.ok) {
    if (response.status === 401 && !path.startsWith("/api/auth/")) onUnauthorised?.();
    const problem = (body as ProblemDetails | null) ?? {};
    if (response.status >= 500 && !problem.detail) {
      problem.detail = "Sorry, there is a problem with the service. Try again later.";
    }
    throw new ApiError(response.status, { status: response.status, ...problem });
  }

  return body as T;
}

export function toQueryString(params: object): string {
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== "") search.set(key, String(value));
  }
  const text = search.toString();
  return text ? `?${text}` : "";
}
