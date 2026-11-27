import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { axe } from "vitest-axe";
import type { RequestDetail } from "../../api/types";
import { administrator, mockApi, renderApp } from "../../test/utils";

const request: RequestDetail = {
  id: "r1",
  reference: "CSR-000042",
  status: "Submitted",
  requesterName: "Sarah Khan",
  requesterEmail: "sarah.khan@clinicspace.test",
  costCode: "CC-4510",
  budgetHolderName: "Priya Patel",
  budgetHolderEmail: "priya.patel@nhs.net",
  location: { regionId: 1, region: "North Staffordshire", siteId: 10, site: "Harplands Hospital", buildingId: 100, building: "Main Building" },
  startsAt: "2026-12-08T09:00:00Z",
  endsAt: "2026-12-08T17:00:00Z",
  sinkRequired: true,
  clinicalCouchRequired: false,
  additionalRequirements: "Hearing loop",
  termsVersion: "2026-11-v1",
  termsAcceptedAt: "2026-11-30T10:00:00Z",
  submittedAt: "2026-11-30T10:00:00Z",
  updatedAt: "2026-11-30T10:00:00Z",
  decidedByName: null,
  decidedAt: null,
  decisionNotes: null,
  version: "AAAAAAAAB9E=",
  timeline: [{ type: "Submitted", actorName: "Sarah Khan", occurredAt: "2026-11-30T10:00:00Z", notes: null }],
  overlappingRequests: [
    { id: "r2", reference: "CSR-000043", status: "Accepted", requesterName: "James Wright", startsAt: "2026-12-08T10:00:00Z", endsAt: "2026-12-08T12:00:00Z" },
  ],
};

describe("Administrator review", () => {
  it("shows the request, warns about overlapping bookings and has no accessibility problems", async () => {
    mockApi({ "GET /api/requests/r1": () => ({ body: request }) }, administrator);
    const { container } = renderApp("/admin/requests/r1");

    expect(await screen.findByRole("heading", { name: /review request/i })).toBeInTheDocument();
    expect(screen.getByText(/1 other request for this building at the same time/i)).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "CSR-000043" })).toBeInTheDocument();
    expect(await axe(container)).toHaveNoViolations();
  });

  it("requires a reason before rejecting", async () => {
    const { calls } = mockApi({ "GET /api/requests/r1": () => ({ body: request }) }, administrator);
    renderApp("/admin/requests/r1");
    const user = userEvent.setup();

    await user.click(await screen.findByLabelText("Reject"));
    await user.click(screen.getByRole("button", { name: "Reject request" }));

    expect(await screen.findByRole("link", { name: "Enter a reason for rejecting the request" })).toBeInTheDocument();
    expect(calls.some((c) => c.method === "POST")).toBe(false);
  });

  it("accepts the request with the version it loaded and confirms the email", async () => {
    const accepted: RequestDetail = {
      ...request,
      status: "Accepted",
      decidedByName: "Olivia Bennett",
      decidedAt: "2026-11-30T11:00:00Z",
      decisionNotes: "Room 2",
      timeline: [...request.timeline, { type: "Accepted", actorName: "Olivia Bennett", occurredAt: "2026-11-30T11:00:00Z", notes: "Room 2" }],
    };
    let current = request;
    const { calls } = mockApi(
      {
        "GET /api/requests/r1": () => ({ body: current }),
        "POST /api/requests/r1/decision": () => {
          current = accepted;
          return { body: accepted };
        },
      },
      administrator,
    );
    renderApp("/admin/requests/r1");
    const user = userEvent.setup();

    await user.click(await screen.findByLabelText("Accept"));
    await user.type(screen.getByLabelText(/notes for the requester/i), "Room 2");
    await user.click(screen.getByRole("button", { name: "Accept request" }));

    expect(await screen.findByRole("heading", { name: "Request CSR-000042 accepted" })).toBeInTheDocument();
    expect(screen.getByText(/we have emailed sarah khan to tell them/i)).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Accept request" })).not.toBeInTheDocument();
    const post = calls.find((c) => c.method === "POST")!;
    expect(JSON.parse(String(post.init?.body))).toEqual({ decision: "Accept", notes: "Room 2", version: "AAAAAAAAB9E=" });
  });

  it("explains a conflict when someone else decided first", async () => {
    mockApi(
      {
        "GET /api/requests/r1": () => ({ body: request }),
        "POST /api/requests/r1/decision": () => ({
          status: 409,
          body: { title: "Conflict", detail: "This request was changed by someone else. Reload it to see the latest version.", code: "concurrency_conflict" },
        }),
      },
      administrator,
    );
    renderApp("/admin/requests/r1");
    const user = userEvent.setup();

    await user.click(await screen.findByLabelText("Accept"));
    await user.click(screen.getByRole("button", { name: "Accept request" }));

    expect(await screen.findByText(/changed by someone else/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /reload the latest version/i })).toBeInTheDocument();
  });
});
