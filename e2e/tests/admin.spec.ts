import { expect, test } from "@playwright/test";
import { MAILPIT, signIn, submitRequest, waitForEmail } from "./helpers";

test.describe("Administrator journey", () => {
  test("finds a submitted request, accepts it, and the requester is emailed and sees Accepted", async ({ browser, request }) => {
    const requesterPage = await (await browser.newContext()).newPage();
    await signIn(requesterPage, "sarah.khan");
    const reference = await submitRequest(requesterPage, { costCode: "E2E-ACCEPT" });

    const adminPage = await (await browser.newContext()).newPage();
    await signIn(adminPage, "olivia.bennett");
    await expect(adminPage.getByRole("heading", { name: /clinical space requests/i })).toBeVisible();

    await adminPage.getByRole("searchbox", { name: "Search" }).fill(reference);
    await adminPage.getByRole("button", { name: "Apply" }).click();
    await adminPage.getByRole("link", { name: reference }).click();

    await expect(adminPage.getByRole("heading", { name: /review request/i })).toBeVisible();
    await adminPage.getByRole("radio", { name: "Accept" }).check();
    await adminPage.getByLabel(/notes for the requester/i).fill("Room 2. Collect the key from reception.");
    await adminPage.getByRole("button", { name: "Accept request" }).click();
    await expect(adminPage.getByRole("heading", { name: `Request ${reference} accepted` })).toBeVisible();

    const email = await waitForEmail(request, `${reference} has been accepted`);
    expect(email.To[0]!.Address).toBe("sarah.khan@clinicspace.test");
    const full = await (await request.get(`${MAILPIT}/api/v1/message/${email.ID}`)).json();
    expect(full.Text).toContain("Room 2. Collect the key from reception.");

    await requesterPage.goto("/requests");
    await expect(requesterPage.getByRole("row").filter({ hasText: reference }).getByTestId("status-tag")).toHaveText("Accepted");
  });

  test("must give a reason to reject, and the requester sees it", async ({ browser, request }) => {
    const requesterPage = await (await browser.newContext()).newPage();
    await signIn(requesterPage, "james.wright");
    const reference = await submitRequest(requesterPage, { costCode: "E2E-REJECT" });

    const adminPage = await (await browser.newContext()).newPage();
    await signIn(adminPage, "daniel.okafor");
    await adminPage.getByRole("searchbox", { name: "Search" }).fill(reference);
    await adminPage.getByRole("button", { name: "Apply" }).click();
    await adminPage.getByRole("link", { name: reference }).click();

    await adminPage.getByRole("radio", { name: "Reject" }).check();
    await adminPage.getByRole("button", { name: "Reject request" }).click();
    await expect(adminPage.getByRole("link", { name: "Enter a reason for rejecting the request" })).toBeVisible();

    await adminPage.getByLabel("Reason for rejecting").fill("The building is closed that day. Please choose another date.");
    await adminPage.getByRole("button", { name: "Reject request" }).click();
    await expect(adminPage.getByRole("heading", { name: `Request ${reference} rejected` })).toBeVisible();

    await waitForEmail(request, `${reference} has been rejected`);
    await requesterPage.goto("/requests");
    await requesterPage.getByRole("link", { name: reference }).click();
    await expect(requesterPage.getByText("The building is closed that day.", { exact: false }).first()).toBeVisible();
  });

  test("a requester cannot reach the administrator pages", async ({ page }) => {
    await signIn(page, "sarah.khan");
    await page.goto("/admin/requests");
    await expect(page).toHaveURL(/\/requests$/);
  });
});
