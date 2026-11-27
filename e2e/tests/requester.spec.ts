import { expect, test } from "@playwright/test";
import { fillRequestForm, signIn, submitRequest, waitForEmail } from "./helpers";

test.describe("Requester journey", () => {
  test("signs in, creates a request and sees it as Submitted in My requests", async ({ page, request }) => {
    await signIn(page, "sarah.khan");
    await expect(page.getByRole("heading", { name: "My requests" })).toBeVisible();

    await page.getByRole("link", { name: "Request a clinical space" }).click();
    await expect(page.getByTestId("requester-name")).toHaveText("Sarah Khan");
    await expect(page.getByTestId("requester-email")).toHaveText("sarah.khan@clinicspace.test");

    await fillRequestForm(page, { costCode: "e2e-1001", notes: "Wheelchair access" });
    await page.getByRole("button", { name: "Continue" }).click();
    await expect(page.getByRole("heading", { name: /check your answers/i })).toBeVisible();
    await expect(page.getByText("E2E-1001")).toBeVisible();

    await page.getByRole("button", { name: "Submit request" }).click();
    const reference = (await page.getByTestId("confirmation-reference").textContent())!.trim();

    await page.getByRole("link", { name: "My requests" }).first().click();
    const row = page.getByRole("row").filter({ hasText: reference });
    await expect(row.getByTestId("status-tag")).toHaveText("Submitted");

    await waitForEmail(request, `received your clinical space request ${reference}`);
  });

  test("shows clear errors and keeps answers when the form is invalid", async ({ page }) => {
    await signIn(page, "sarah.khan");
    await page.goto("/requests/new");
    await page.getByLabel("Cost code").fill("bad code!");
    await page.getByRole("button", { name: "Continue" }).click();

    const summary = page.getByRole("alert");
    await expect(summary).toBeFocused();
    await expect(summary.getByRole("link", { name: /Cost code must only include/ })).toBeVisible();
    await expect(page).toHaveTitle(/^Error: /);

    await summary.getByRole("link", { name: "Select a region" }).click();
    await expect(page.getByLabel("Region")).toBeFocused();
    await expect(page.getByLabel("Cost code")).toHaveValue("bad code!");
  });

  test("cannot open another person's request", async ({ page }) => {
    await signIn(page, "sarah.khan");
    const reference = await submitRequest(page);
    const url = page.url();
    expect(reference).toBeTruthy();
    await page.getByRole("link", { name: "View your request" }).click();
    const requestUrl = page.url();
    expect(requestUrl).not.toBe(url);

    await page.getByRole("button", { name: "Sign out" }).click();
    await signIn(page, "james.wright");
    await page.goto(new URL(requestUrl).pathname);
    await expect(page.getByRole("heading", { name: "Page not found" })).toBeVisible();
  });

  test("works on a small screen @mobile", async ({ page }) => {
    await signIn(page, "james.wright");
    await expect(page.getByRole("heading", { name: "My requests" })).toBeVisible();
    const hasHorizontalScroll = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth);
    expect(hasHorizontalScroll).toBe(false);
  });
});
