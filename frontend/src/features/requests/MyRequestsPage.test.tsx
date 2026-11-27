import { screen, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { axe } from "vitest-axe";
import type { RequestSummary } from "../../api/types";
import { mockApi, renderApp, requester } from "../../test/utils";

const summary = (overrides: Partial<RequestSummary>): RequestSummary => ({
  id: "r1",
  reference: "CSR-000001",
  status: "Submitted",
  requesterName: "Sarah Khan",
  region: "North Staffordshire",
  site: "Harplands Hospital",
  building: "Main Building",
  startsAt: "2026-12-08T09:00:00Z",
  endsAt: "2026-12-08T17:00:00Z",
  submittedAt: "2026-11-30T10:00:00Z",
  decidedAt: null,
  ...overrides,
});

describe("My requests", () => {
  it("lists the requester's requests with their status", async () => {
    mockApi(
      {
        "GET /api/requests/mine": () => ({
          body: {
            items: [
              summary({ id: "a", reference: "CSR-000003", status: "Submitted" }),
              summary({ id: "b", reference: "CSR-000002", status: "Accepted" }),
              summary({ id: "c", reference: "CSR-000001", status: "Rejected" }),
            ],
            page: 1,
            pageSize: 10,
            totalCount: 3,
            totalPages: 1,
          },
        }),
      },
      requester,
    );
    const { container } = renderApp("/requests");

    const table = await screen.findByRole("table", { name: "Your clinical space requests" });
    const rows = within(table).getAllByRole("row").slice(1);
    expect(rows.map((row) => within(row).getByTestId("status-tag").textContent)).toEqual(["Submitted", "Accepted", "Rejected"]);
    expect(within(rows[0]!).getByRole("link", { name: "CSR-000003" })).toHaveAttribute("href", "/requests/a");
    expect(await axe(container)).toHaveNoViolations();
  });

  it("explains when there are no requests yet", async () => {
    mockApi({ "GET /api/requests/mine": () => ({ body: { items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0 } }) }, requester);
    renderApp("/requests");

    expect(await screen.findByText("You have not made any requests yet.")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Request a clinical space" })).toHaveAttribute("href", "/requests/new");
  });
});
