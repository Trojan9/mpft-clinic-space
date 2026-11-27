import { screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { axe } from "vitest-axe";
import { mockApi, renderApp, requester } from "../../test/utils";

const locationRoutes = {
  "GET /api/locations/regions": () => ({ body: [{ id: 1, name: "North Staffordshire" }] }),
  "GET /api/locations/regions/1/sites": () => ({ body: [{ id: 10, name: "Harplands Hospital" }] }),
  "GET /api/locations/sites/10/buildings": () => ({ body: [{ id: 100, name: "Main Building" }, { id: 101, name: "Therapies Wing" }] }),
};

describe("Request a clinical space", () => {
  it("shows the requester's name and email from their account", async () => {
    mockApi(locationRoutes, requester);
    renderApp("/requests/new");

    expect(await screen.findByTestId("requester-name")).toHaveTextContent("Sarah Khan");
    expect(screen.getByTestId("requester-email")).toHaveTextContent("sarah.khan@clinicspace.test");
  });

  it("has no detectable accessibility problems", async () => {
    mockApi(locationRoutes, requester);
    const { container } = renderApp("/requests/new");
    await screen.findByRole("option", { name: "North Staffordshire" });

    expect(await axe(container)).toHaveNoViolations();
  });

  it("loads sites and buildings as the region and site are chosen", async () => {
    mockApi(locationRoutes, requester);
    renderApp("/requests/new");
    const user = userEvent.setup();

    const site = await screen.findByLabelText("Site");
    expect(site).toBeDisabled();
    expect(screen.getByLabelText("Building")).toBeDisabled();

    await user.selectOptions(await screen.findByLabelText("Region"), "1");
    await waitFor(() => expect(screen.getByLabelText("Site")).toBeEnabled());
    await user.selectOptions(screen.getByLabelText("Site"), await screen.findByRole("option", { name: "Harplands Hospital" }));
    await waitFor(() => expect(screen.getByLabelText("Building")).toBeEnabled());

    const buildingOptions = within(screen.getByLabelText("Building")).getAllByRole("option").map((o) => o.textContent);
    expect(buildingOptions).toEqual(["Select a building", "Main Building", "Therapies Wing"]);
  });

  it("lists every problem in the error summary when submitted empty", async () => {
    mockApi(locationRoutes, requester);
    renderApp("/requests/new");
    const user = userEvent.setup();

    await user.click(await screen.findByRole("button", { name: "Continue" }));

    const summary = await screen.findByRole("alert");
    const messages = within(summary).getAllByRole("link").map((link) => link.textContent);
    expect(messages).toEqual([
      "Enter a cost code",
      "Enter the budget holder's name",
      "Enter the budget holder's email address",
      "Select a region",
      "Select a site",
      "Select a building",
      "Enter a start date",
      "Select a start time",
      "Enter an end date",
      "Select an end time",
      "Select yes if a sink is required",
      "Select yes if a clinical couch is required",
      "Confirm that you accept the terms and conditions",
    ]);
    expect(summary).toHaveFocus();
  });

  it("goes to check your answers, then submits and shows the reference", async () => {
    const { calls } = mockApi(
      {
        ...locationRoutes,
        "POST /api/requests": () => ({ status: 201, body: { id: "abc", reference: "CSR-000123" } }),
      },
      requester,
    );
    renderApp("/requests/new");
    const user = userEvent.setup();
    const nextYear = String(new Date().getFullYear() + 1);

    await user.type(await screen.findByLabelText("Cost code"), "cc-4510");
    await user.type(screen.getByLabelText("Budget holder name"), "Priya Patel");
    await user.type(screen.getByLabelText("Budget holder email address"), "priya.patel@nhs.net");
    await user.selectOptions(screen.getByLabelText("Region"), await screen.findByRole("option", { name: "North Staffordshire" }));
    await user.selectOptions(screen.getByLabelText("Site"), await screen.findByRole("option", { name: "Harplands Hospital" }));
    await user.selectOptions(screen.getByLabelText("Building"), await screen.findByRole("option", { name: "Main Building" }));

    const start = screen.getByRole("group", { name: "Start date and time" });
    await user.type(within(start).getByLabelText("Day"), "8");
    await user.type(within(start).getByLabelText("Month"), "1");
    await user.type(within(start).getByLabelText("Year"), nextYear);
    await user.selectOptions(within(start).getByLabelText("Hour"), "09");
    await user.selectOptions(within(start).getByLabelText("Minute"), "00");
    const end = screen.getByRole("group", { name: "End date and time" });
    await user.type(within(end).getByLabelText("Day"), "8");
    await user.type(within(end).getByLabelText("Month"), "1");
    await user.type(within(end).getByLabelText("Year"), nextYear);
    await user.selectOptions(within(end).getByLabelText("Hour"), "17");
    await user.selectOptions(within(end).getByLabelText("Minute"), "30");

    await user.click(within(screen.getByRole("group", { name: "Is a sink required?" })).getByLabelText("Yes"));
    await user.click(within(screen.getByRole("group", { name: "Is a clinical couch required?" })).getByLabelText("No"));
    await user.click(screen.getByLabelText("I have read and accept the terms and conditions"));
    await user.click(screen.getByRole("button", { name: "Continue" }));

    expect(await screen.findByRole("heading", { name: /check your answers/i })).toBeInTheDocument();
    expect(screen.getByText("CC-4510")).toBeInTheDocument();
    expect(screen.getByText("Harplands Hospital")).toBeInTheDocument();
    expect(screen.getByText(new RegExp(`8 January ${nextYear}, 9am to 5:30pm`))).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Submit request" }));

    expect(await screen.findByTestId("confirmation-reference")).toHaveTextContent("CSR-000123");
    const post = calls.find((c) => c.method === "POST" && c.path === "/api/requests")!;
    expect(JSON.parse(String(post.init?.body))).toMatchObject({
      costCode: "CC-4510",
      buildingId: 100,
      sinkRequired: true,
      clinicalCouchRequired: false,
      acceptTerms: true,
      startsAt: `${nextYear}-01-08T09:00:00.000Z`,
      endsAt: `${nextYear}-01-08T17:30:00.000Z`,
    });
  });
});
