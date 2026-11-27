/**
 * Captures the screenshots in docs/screenshots by walking through both user journeys in a real
 * browser against the running stack (docker compose up). Re-run any time to refresh the evidence:
 *
 *   cd e2e && npm ci && npx playwright install chromium && npm run screenshots
 *   (or, with Google Chrome installed:  PW_CHANNEL=chrome npm run screenshots)
 */
import { chromium } from "@playwright/test";
import { mkdir } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const BASE = process.env.E2E_BASE_URL ?? "http://localhost:8080";
const MAILPIT = process.env.MAILPIT_URL ?? "http://localhost:8025";
// Accounts from the supplied exercise workbook (Users sheet).
const REQUESTER = "alex.evans@example.nhs.uk"; // Has seeded sample requests
const OTHER_REQUESTER = "riley.green@example.nhs.uk";
const ADMIN = "pat.jones@example.nhs.uk";
const passwordFor = (username) => (username === ADMIN ? "AdminPass!23" : "Password123!");
const OUT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../../docs/screenshots");

const browser = await chromium.launch({ channel: process.env.PW_CHANNEL || undefined });
await mkdir(OUT, { recursive: true });

const desktop = { viewport: { width: 1280, height: 900 }, deviceScaleFactor: 1, locale: "en-GB", timezoneId: "Europe/London" };
const mobile = { viewport: { width: 390, height: 844 }, deviceScaleFactor: 2, isMobile: true, hasTouch: true, locale: "en-GB", timezoneId: "Europe/London" };

async function shot(page, name, fullPage = true) {
  await page.waitForLoadState("networkidle");
  // Blur so the focus ring from the last click does not appear in the picture.
  await page.evaluate(() => document.activeElement instanceof HTMLElement && document.activeElement.blur());
  await page.screenshot({ path: path.join(OUT, `${name}.png`), fullPage });
  console.log(`  ✓ ${name}.png`);
}

async function signIn(page, username) {
  await page.goto(`${BASE}/sign-in`);
  await page.getByLabel("Username").fill(username);
  await page.getByLabel("Password").fill(passwordFor(username));
  await page.getByRole("button", { name: "Sign in" }).click();
  await page.getByRole("button", { name: "Sign out" }).waitFor();
}

function ukDate(daysAhead) {
  const d = new Date(Date.now() + daysAhead * 86_400_000);
  const parts = Object.fromEntries(
    new Intl.DateTimeFormat("en-GB", { timeZone: "Europe/London", day: "numeric", month: "numeric", year: "numeric" })
      .formatToParts(d)
      .map((p) => [p.type, p.value]),
  );
  return parts;
}

console.log(`Capturing screenshots from ${BASE} into ${OUT}`);

// ---- Sign in ---------------------------------------------------------------------------------
const requesterContext = await browser.newContext(desktop);
const requester = await requesterContext.newPage();
await requester.goto(`${BASE}/sign-in`);
await shot(requester, "01-sign-in");
await requester.getByRole("button", { name: "Sign in" }).click();
await shot(requester, "02-sign-in-validation");

// ---- Requester: My requests and a new request ------------------------------------------------
await signIn(requester, REQUESTER);
await shot(requester, "03-my-requests");

await requester.goto(`${BASE}/requests/new`);
await requester.getByRole("button", { name: "Continue" }).click();
await shot(requester, "04-request-form-validation");

await requester.goto(`${BASE}/requests/new`);
const date = ukDate(7);
await requester.getByLabel("Cost code").fill("CC-4510");
await requester.getByLabel("Budget holder name").fill("Priya Patel");
await requester.getByLabel("Budget holder email address").fill("priya.patel@clinicspace.test");
// Alder House is where the seeded sample request is, so the review page shows the clash warning.
await requester.getByLabel("Region").selectOption({ label: "Staffordshire" });
await requester.getByLabel("Site").selectOption({ label: "Stafford Community Health Hub" });
await requester.getByLabel("Building").selectOption({ label: "Alder House" });
for (const [group, hour] of [["Start date and time", "10"], ["End date and time", "13"]]) {
  const g = requester.getByRole("group", { name: group });
  await g.getByLabel("Day").fill(date.day);
  await g.getByLabel("Month").fill(date.month);
  await g.getByLabel("Year").fill(date.year);
  await g.getByLabel("Hour").selectOption(hour);
  await g.getByLabel("Minute").selectOption("00");
}
await requester.getByRole("group", { name: "Is a sink required?" }).getByLabel("Yes").check();
await requester.getByRole("group", { name: "Is a clinical couch required?" }).getByLabel("Yes").check();
await requester.getByLabel("Other requirements (optional)").fill("Wheelchair access for patients, and a hearing loop if available.");
await requester.getByLabel("I have read and accept the terms and conditions").check();
await shot(requester, "05-request-form-completed");

await requester.getByRole("button", { name: "Continue" }).click();
await requester.getByRole("heading", { name: /check your answers/i }).waitFor();
await shot(requester, "06-check-answers");

await requester.getByRole("button", { name: "Submit request" }).click();
const reference = (await requester.getByTestId("confirmation-reference").textContent()).trim();
await shot(requester, "07-request-submitted");

await requester.goto(`${BASE}/requests`);
await requester.getByRole("link", { name: reference }).waitFor();
await shot(requester, "08-my-requests-with-new-request");
await requester.getByRole("link", { name: reference }).click();
await requester.getByRole("heading", { name: "Clinical space request" }).waitFor();
await shot(requester, "09-request-detail-submitted");
const requestPath = new URL(requester.url()).pathname;

// ---- Administrator: review and decide --------------------------------------------------------
const adminContext = await browser.newContext(desktop);
const admin = await adminContext.newPage();
await signIn(admin, ADMIN);
await admin.getByRole("link", { name: reference }).waitFor();
await shot(admin, "10-admin-requests-to-review");

await admin.getByRole("searchbox", { name: "Search" }).fill("Stafford");
await admin.getByRole("button", { name: /All$/ }).click();
await admin.getByRole("button", { name: "Apply" }).click();
await admin.getByTestId("result-count").waitFor();
await shot(admin, "11-admin-search-and-filter");

await admin.goto(`${BASE}/admin${requestPath}`);
await admin.getByRole("heading", { name: /review request/i }).waitFor();
await shot(admin, "12-admin-review-request");

await admin.getByRole("radio", { name: "Reject" }).check();
await admin.getByRole("button", { name: "Reject request" }).click();
await admin.getByRole("link", { name: "Enter a reason for rejecting the request" }).waitFor();
await shot(admin, "13-admin-reject-needs-reason");

await admin.getByRole("radio", { name: "Accept" }).check();
await admin.getByLabel(/notes for the requester/i).fill("Room 2 is booked for you. Collect the key from main reception.");
await admin.getByRole("button", { name: "Accept request" }).click();
await admin.getByRole("heading", { name: `Request ${reference} accepted` }).waitFor();
await shot(admin, "14-admin-request-accepted");

await admin.goto(`${BASE}/admin/requests?status=Accepted`);
await admin.getByRole("link", { name: reference }).waitFor();
await shot(admin, "15-admin-accepted-list");

// ---- Requester sees the decision --------------------------------------------------------------
await requester.goto(`${BASE}/requests`);
await requester.getByRole("link", { name: reference }).waitFor();
await shot(requester, "16-my-requests-status-updated");
await requester.goto(`${BASE}${requestPath}`);
await requester.getByText(/Accepted by Pat Jones/).waitFor();
await shot(requester, "17-request-detail-accepted");

// ---- Email (Mailpit) ---------------------------------------------------------------------------
const mailContext = await browser.newContext(desktop);
const mail = await mailContext.newPage();
let messageId;
for (let i = 0; i < 30 && !messageId; i++) {
  const response = await mail.request.get(`${MAILPIT}/api/v1/messages?limit=50`);
  const { messages } = await response.json();
  messageId = messages.find((m) => m.Subject.includes(`${reference} has been accepted`))?.ID;
  if (!messageId) await new Promise((r) => setTimeout(r, 1000));
}
if (!messageId) throw new Error("Decision email did not arrive in Mailpit");
await mail.goto(`${MAILPIT}/`);
await mail.getByText(`${reference} has been accepted`).first().waitFor();
await shot(mail, "18-email-inbox-mailpit", false);
await mail.goto(`${MAILPIT}/view/${messageId}`);
await mail.waitForTimeout(1500);
await shot(mail, "19-email-request-accepted", false);

// ---- Access control: another requester cannot open this request -----------------------------
const otherContext = await browser.newContext(desktop);
const other = await otherContext.newPage();
await signIn(other, OTHER_REQUESTER);
await other.goto(`${BASE}${requestPath}`);
await other.getByRole("heading", { name: "Page not found" }).waitFor();
await shot(other, "20-access-control-other-users-request");

// ---- Mobile -------------------------------------------------------------------------------------
const phoneContext = await browser.newContext(mobile);
const phone = await phoneContext.newPage();
await signIn(phone, REQUESTER);
await shot(phone, "21-mobile-my-requests");
await phone.goto(`${BASE}/requests/new`);
await phone.getByRole("button", { name: "Continue" }).click();
await shot(phone, "22-mobile-request-form-validation", false);

// ---- API reference (development only) -------------------------------------------------------------
await admin.goto(`${BASE}/api/docs`);
await admin.waitForTimeout(3000);
await shot(admin, "23-api-reference", false);

await browser.close();
console.log(`Done. Reference used: ${reference}`);
