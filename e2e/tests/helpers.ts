import { expect, type APIRequestContext, type Page } from "@playwright/test";

/** Accounts from the supplied exercise workbook (Users sheet). */
export const USERS = {
  requester: "alex.evans@example.nhs.uk",
  otherRequester: "riley.green@example.nhs.uk",
  admin: "pat.jones@example.nhs.uk",
  otherAdmin: "aman.singh@example.nhs.uk",
} as const;

const ADMIN_EMAILS = new Set(["pat.jones@example.nhs.uk", "aman.singh@example.nhs.uk", "sam.taylor@example.nhs.uk", "jordan.brown@example.nhs.uk"]);

export function passwordFor(username: string) {
  return ADMIN_EMAILS.has(username) ? "AdminPass!23" : "Password123!";
}
export const MAILPIT = process.env.MAILPIT_URL ?? "http://localhost:8025";

export async function signIn(page: Page, username: string) {
  await page.goto("/sign-in");
  await page.getByLabel("Username").fill(username);
  await page.getByLabel("Password").fill(passwordFor(username));
  await page.getByRole("button", { name: "Sign in" }).click();
  await expect(page.getByRole("button", { name: "Sign out" })).toBeVisible();
}

/** A date in the near future, in UK wall-clock parts, for filling in the form. */
export function futureDate(daysAhead: number) {
  const d = new Date(Date.now() + daysAhead * 86_400_000);
  const parts = new Intl.DateTimeFormat("en-GB", { timeZone: "Europe/London", day: "numeric", month: "numeric", year: "numeric" })
    .formatToParts(d)
    .reduce<Record<string, string>>((acc, p) => ({ ...acc, [p.type]: p.value }), {});
  return { day: parts.day!, month: parts.month!, year: parts.year! };
}

export async function fillRequestForm(page: Page, options: { costCode?: string; daysAhead?: number; notes?: string } = {}) {
  const date = futureDate(options.daysAhead ?? 10);
  await page.getByLabel("Cost code").fill(options.costCode ?? "CC-4510");
  await page.getByLabel("Budget holder name").fill("Priya Patel");
  await page.getByLabel("Budget holder email address").fill("priya.patel@clinicspace.test");
  await page.getByLabel("Region").selectOption({ label: "Shropshire" });
  await page.getByLabel("Site").selectOption({ label: "Oswestry Community Clinic" });
  await page.getByLabel("Building").selectOption({ label: "Sycamore House" });

  const start = page.getByRole("group", { name: "Start date and time" });
  await start.getByLabel("Day").fill(date.day);
  await start.getByLabel("Month").fill(date.month);
  await start.getByLabel("Year").fill(date.year);
  await start.getByLabel("Hour").selectOption("09");
  await start.getByLabel("Minute").selectOption("00");
  const end = page.getByRole("group", { name: "End date and time" });
  await end.getByLabel("Day").fill(date.day);
  await end.getByLabel("Month").fill(date.month);
  await end.getByLabel("Year").fill(date.year);
  await end.getByLabel("Hour").selectOption("13");
  await end.getByLabel("Minute").selectOption("00");

  await page.getByRole("group", { name: "Is a sink required?" }).getByLabel("Yes").check();
  await page.getByRole("group", { name: "Is a clinical couch required?" }).getByLabel("Yes").check();
  if (options.notes) await page.getByLabel("Other requirements (optional)").fill(options.notes);
  await page.getByLabel("I have read and accept the terms and conditions").check();
}

/** Submits a new request through the UI as the signed-in requester and returns its reference. */
export async function submitRequest(page: Page, options: Parameters<typeof fillRequestForm>[1] = {}) {
  await page.goto("/requests/new");
  await fillRequestForm(page, options);
  await page.getByRole("button", { name: "Continue" }).click();
  await page.getByRole("button", { name: "Submit request" }).click();
  const reference = (await page.getByTestId("confirmation-reference").textContent())!.trim();
  expect(reference).toMatch(/^CSR-\d{6}$/);
  return reference;
}

interface MailpitMessage {
  ID: string;
  Subject: string;
  To: { Address: string }[];
}

/** Waits for an email whose subject contains the text (the outbox sends every few seconds). */
export async function waitForEmail(request: APIRequestContext, subjectContains: string): Promise<MailpitMessage> {
  let found: MailpitMessage | undefined;
  await expect
    .poll(
      async () => {
        const response = await request.get(`${MAILPIT}/api/v1/messages?limit=50`);
        const body = (await response.json()) as { messages: MailpitMessage[] };
        found = body.messages.find((m) => m.Subject.includes(subjectContains));
        return Boolean(found);
      },
      { timeout: 30_000, intervals: [1_000] },
    )
    .toBe(true);
  return found!;
}
