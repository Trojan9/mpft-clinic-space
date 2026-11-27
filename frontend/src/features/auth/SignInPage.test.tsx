import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { axe } from "vitest-axe";
import { mockApi, renderApp, requester } from "../../test/utils";

describe("Sign in", () => {
  it("has no detectable accessibility problems", async () => {
    mockApi({}, null);
    const { container } = renderApp("/sign-in");
    await screen.findByRole("heading", { name: /sign in/i });

    expect(await axe(container)).toHaveNoViolations();
  });

  it("shows an error summary linked to each empty field", async () => {
    mockApi({}, null);
    renderApp("/sign-in");
    const user = userEvent.setup();

    await user.click(await screen.findByRole("button", { name: "Sign in" }));

    const summary = await screen.findByRole("alert");
    expect(summary).toHaveTextContent("There is a problem");
    expect(screen.getByRole("link", { name: "Enter your username" })).toHaveAttribute("href", "#username");
    expect(screen.getByRole("link", { name: "Enter your password" })).toHaveAttribute("href", "#password");
    expect(screen.getByLabelText("Username")).toHaveAttribute("aria-invalid", "true");
    expect(document.title).toBe("Error: Sign in - MPFT Clinic Space");
  });

  it("signs a requester in and takes them to My requests", async () => {
    const { calls } = mockApi(
      {
        "POST /api/auth/login": () => ({ body: requester }),
        "GET /api/requests/mine": () => ({ body: { items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0 } }),
      },
      null,
    );
    renderApp("/sign-in");
    const user = userEvent.setup();

    await user.type(await screen.findByLabelText("Username"), "sarah.khan");
    await user.type(screen.getByLabelText("Password"), "correct horse");
    await user.click(screen.getByRole("button", { name: "Sign in" }));

    expect(await screen.findByRole("heading", { name: "My requests" })).toBeInTheDocument();
    const login = calls.find((c) => c.path === "/api/auth/login")!;
    expect(JSON.parse(String(login.init?.body))).toEqual({ username: "sarah.khan", password: "correct horse" });
    expect(new Headers(login.init?.headers).get("X-XSRF-TOKEN")).toBe("test-token");
  });

  it("shows the API's generic message when sign in fails", async () => {
    mockApi(
      {
        "POST /api/auth/login": () => ({
          status: 401,
          body: { title: "Sign in failed", detail: "Incorrect username or password." },
        }),
      },
      null,
    );
    renderApp("/sign-in");
    const user = userEvent.setup();

    await user.type(await screen.findByLabelText("Username"), "sarah.khan");
    await user.type(screen.getByLabelText("Password"), "wrong");
    await user.click(screen.getByRole("button", { name: "Sign in" }));

    await waitFor(() => expect(screen.getByRole("alert")).toHaveTextContent("Incorrect username or password."));
  });

  it("ignores return addresses that point to another site", async () => {
    mockApi(
      {
        "POST /api/auth/login": () => ({ body: requester }),
        "GET /api/requests/mine": () => ({ body: { items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0 } }),
      },
      null,
    );
    const { router } = renderApp("/sign-in?returnTo=//evil.example/phish");
    const user = userEvent.setup();

    await user.type(await screen.findByLabelText("Username"), "sarah.khan");
    await user.type(screen.getByLabelText("Password"), "pw");
    await user.click(screen.getByRole("button", { name: "Sign in" }));

    await screen.findByRole("heading", { name: "My requests" });
    expect(router.state.location.pathname).toBe("/requests");
  });
});
