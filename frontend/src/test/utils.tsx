import { render } from "@testing-library/react";
import { QueryClientProvider } from "@tanstack/react-query";
import { createMemoryRouter, RouterProvider } from "react-router";
import { vi } from "vitest";
import { createQueryClient, routes } from "../App";
import { AuthProvider } from "../auth/AuthContext";
import type { User } from "../api/types";

export const requester: User = {
  id: "11111111-1111-1111-1111-111111111111",
  username: "sarah.khan",
  displayName: "Sarah Khan",
  email: "sarah.khan@clinicspace.test",
  role: "Requester",
};

export const administrator: User = {
  id: "22222222-2222-2222-2222-222222222222",
  username: "olivia.bennett",
  displayName: "Olivia Bennett",
  email: "olivia.bennett@clinicspace.test",
  role: "Administrator",
};

type Handler = (init: RequestInit | undefined, url: URL) => { status?: number; body?: unknown } | Promise<{ status?: number; body?: unknown }>;

/**
 * Fake API: maps "METHOD /path" to a handler. Unmatched calls fail loudly so tests cannot
 * silently depend on an endpoint they did not set up.
 */
export function mockApi(routesMap: Record<string, Handler>, user: User | null) {
  const calls: { method: string; path: string; init?: RequestInit }[] = [];
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = new URL(String(input), "http://localhost");
    const method = (init?.method ?? "GET").toUpperCase();
    calls.push({ method, path: url.pathname + url.search, init });

    const key = `${method} ${url.pathname}`;
    let result: { status?: number; body?: unknown };
    if (key === "GET /api/auth/me" && !routesMap[key]) {
      result = user ? { body: user } : { status: 401, body: { title: "Unauthorised" } };
    } else if (key === "GET /api/auth/csrf") {
      document.cookie = "XSRF-TOKEN=test-token; path=/";
      result = { status: 204 };
    } else if (routesMap[key]) {
      result = await routesMap[key]!(init, url);
    } else {
      throw new Error(`Unexpected API call in test: ${key}`);
    }

    const status = result.status ?? 200;
    return new Response(status === 204 ? null : JSON.stringify(result.body ?? {}), {
      status,
      headers: { "Content-Type": status >= 400 ? "application/problem+json" : "application/json" },
    });
  });
  vi.stubGlobal("fetch", fetchMock);
  return { fetchMock, calls };
}

/** Renders the real app routes at a starting URL, with fresh query and auth state. */
export function renderApp(path: string) {
  const router = createMemoryRouter(routes, { initialEntries: [path] });
  const queryClient = createQueryClient();
  queryClient.setDefaultOptions({ queries: { retry: false, staleTime: 0 } });
  const view = render(
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <RouterProvider router={router} />
      </AuthProvider>
    </QueryClientProvider>,
  );
  return { ...view, router };
}
