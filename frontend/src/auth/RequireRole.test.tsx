import { screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { administrator, mockApi, renderApp, requester } from "../test/utils";

const emptyPage = { items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0 };

describe("Route guards", () => {
  it("sends signed-out visitors to sign in and remembers where they were going", async () => {
    mockApi({}, null);
    const { router } = renderApp("/requests/new");

    await screen.findByRole("heading", { name: /sign in/i });
    expect(router.state.location.pathname).toBe("/sign-in");
    expect(router.state.location.search).toContain("returnTo=%2Frequests%2Fnew");
  });

  it("sends a requester away from administrator pages", async () => {
    mockApi({ "GET /api/requests/mine": () => ({ body: emptyPage }) }, requester);
    const { router } = renderApp("/admin/requests");

    await screen.findByRole("heading", { name: "My requests" });
    expect(router.state.location.pathname).toBe("/requests");
  });

  it("sends an administrator away from requester pages", async () => {
    mockApi(
      {
        "GET /api/requests": () => ({ body: emptyPage }),
        "GET /api/requests/summary": () => ({ body: { submitted: 0, accepted: 0, rejected: 0, total: 0 } }),
        "GET /api/locations/regions": () => ({ body: [] }),
      },
      administrator,
    );
    const { router } = renderApp("/requests/new");

    await screen.findByRole("heading", { name: /clinical space requests/i });
    expect(router.state.location.pathname).toBe("/admin/requests");
  });
});
