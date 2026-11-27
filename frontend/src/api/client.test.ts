import { afterEach, describe, expect, it, vi } from "vitest";
import { ApiError, apiFetch, setUnauthorisedListener, toQueryString } from "./client";

function respond(status: number, body?: unknown) {
  return vi.fn(async () =>
    new Response(body === undefined ? null : JSON.stringify(body), {
      status,
      headers: { "Content-Type": "application/json" },
    }),
  );
}

afterEach(() => {
  vi.unstubAllGlobals();
  setUnauthorisedListener(null);
});

describe("apiFetch", () => {
  it("copies the XSRF-TOKEN cookie into the header on POST", async () => {
    document.cookie = "XSRF-TOKEN=abc%20123; path=/";
    const fetchMock = respond(200, { ok: true });
    vi.stubGlobal("fetch", fetchMock);

    await apiFetch("/api/requests", { method: "POST", body: "{}" });

    const [, init] = fetchMock.mock.calls[0] as unknown as [string, RequestInit];
    expect(new Headers(init.headers).get("X-XSRF-TOKEN")).toBe("abc 123");
    expect(init.credentials).toBe("same-origin");
  });

  it("fetches a token first if there is none", async () => {
    const fetchMock = vi.fn(async (url: string) => {
      if (url === "/api/auth/csrf") {
        document.cookie = "XSRF-TOKEN=fresh; path=/";
        return new Response(null, { status: 204 });
      }
      return new Response("{}", { status: 200, headers: { "Content-Type": "application/json" } });
    });
    vi.stubGlobal("fetch", fetchMock);

    await apiFetch("/api/auth/logout", { method: "POST" });

    expect(fetchMock.mock.calls.map((c) => c[0])).toEqual(["/api/auth/csrf", "/api/auth/logout"]);
  });

  it("does not send the token on GET", async () => {
    document.cookie = "XSRF-TOKEN=abc; path=/";
    const fetchMock = respond(200, []);
    vi.stubGlobal("fetch", fetchMock);

    await apiFetch("/api/locations/regions");

    const [, init] = fetchMock.mock.calls[0] as unknown as [string, RequestInit];
    expect(new Headers(init.headers).has("X-XSRF-TOKEN")).toBe(false);
  });

  it("turns problem details into an ApiError with field errors", async () => {
    vi.stubGlobal("fetch", respond(400, { title: "There is a problem", detail: "Bad", errors: { CostCode: ["Enter a cost code"] } }));

    const error = await apiFetch("/api/requests").catch((e: unknown) => e);

    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).status).toBe(400);
    expect((error as ApiError).fieldErrors).toEqual({ costCode: "Enter a cost code" });
  });

  it("tells the auth layer when a session has ended", async () => {
    const listener = vi.fn();
    setUnauthorisedListener(listener);
    vi.stubGlobal("fetch", respond(401, {}));

    await expect(apiFetch("/api/requests/mine")).rejects.toBeInstanceOf(ApiError);
    expect(listener).toHaveBeenCalledOnce();
  });

  it("gives a friendly message when the network is down", async () => {
    vi.stubGlobal("fetch", vi.fn(async () => Promise.reject(new TypeError("Failed to fetch"))));

    await expect(apiFetch("/api/requests/mine")).rejects.toMatchObject({ status: 0, message: expect.stringContaining("could not reach") });
  });
});

describe("toQueryString", () => {
  it("skips empty values", () => {
    expect(toQueryString({ status: "Submitted", search: "", page: 2, regionId: undefined })).toBe("?status=Submitted&page=2");
    expect(toQueryString({})).toBe("");
  });
});
