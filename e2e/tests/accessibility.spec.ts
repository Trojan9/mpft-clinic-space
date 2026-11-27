import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";
import { signIn } from "./helpers";

// WCAG 2.2 A and AA, checked in a real browser so colour contrast is included.
async function expectAccessible(page: Page) {
  const results = await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "wcag22aa"]).analyze();
  expect(results.violations.map((v) => `${v.id}: ${v.help} (${v.nodes.length})`)).toEqual([]);
}

test.describe("Accessibility (axe-core, WCAG 2.2 AA)", () => {
  test("sign in page", async ({ page }) => {
    await page.goto("/sign-in");
    await expectAccessible(page);
    await page.getByRole("button", { name: "Sign in" }).click();
    await expectAccessible(page);
  });

  test("requester pages", async ({ page }) => {
    await signIn(page, "sarah.khan");
    await expectAccessible(page);
    await page.goto("/requests/new");
    await expectAccessible(page);
    await page.getByRole("button", { name: "Continue" }).click();
    await expectAccessible(page);
  });

  test("administrator pages", async ({ page }) => {
    await signIn(page, "olivia.bennett");
    await expectAccessible(page);
    await page.getByRole("button", { name: /All/ }).click();
    await page.getByRole("table").getByRole("link").first().click();
    await expect(page.getByRole("heading", { name: /review request/i })).toBeVisible();
    await expectAccessible(page);
  });

  test("keyboard users can skip to the main content", async ({ page }) => {
    await page.goto("/sign-in");
    await page.keyboard.press("Tab");
    const skip = page.getByRole("link", { name: "Skip to main content" });
    await expect(skip).toBeFocused();
    await page.keyboard.press("Enter");
    await expect(page.locator("#maincontent")).toBeFocused();
  });
});
