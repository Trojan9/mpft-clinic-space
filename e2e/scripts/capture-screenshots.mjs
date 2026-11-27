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
const PASSWORD = process.env.DEMO_USER_PASSWORD ?? "ClinicSpace-Demo-2026";
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
  await page.getByLabel("Password").fill(PASSWORD);
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
const sarah = await requesterContext.newPage();
await sarah.goto(`${BASE}/sign-in`);
await shot(sarah, "01-sign-in");
await sarah.getByRole("button", { name: "Sign in" }).click();
await shot(sarah, "02-sign-in-validation");

// ---- Requester: My requests and a new request ------------------------------------------------
await signIn(sarah, "sarah.khan");
await shot(sarah, "03-my-requests");

await sarah.goto(`${BASE}/requests/new`);
await sarah.getByRole("button", { name: "Continue" }).click();
await shot(sarah, "04-request-form-validation");

await sarah.goto(`${BASE}/requests/new`);
const date = ukDate(7);
await sarah.getByLabel("Cost code").fill("CC-4510");
await sarah.getByLabel("Budget holder name").fill("Priya Patel");
await sarah.getByLabel("Budget holder email address").fill("priya.patel@clinicspace.test");
await sarah.getByLabel("Region").selectOption({ label: "Stafford and Surrounds" });
await sarah.getByLabel("Site").selectOption({ label: "St George's Hospital" });
await sarah.getByLabel("Building").selectOption({ label: "Main Building" });
for (const [group, hour] of [["Start date and time", "10"], ["End date and time", "13"]]) {
  const g = sarah.getByRole("group", { name: group });
  await g.getByLabel("Day").fill(date.day);
  await g.getByLabel("Month").fill(date.month);
  await g.getByLabel("Year").fill(date.year);
  await g.getByLabel("Hour").selectOption(hour);
  await g.getByLabel("Minute").selectOption("00");
}
await sarah.getByRole("group", { name: "Is a sink required?" }).getByLabel("Yes").check();
await sarah.getByRole("group", { name: "Is a clinical couch required?" }).getByLabel("Yes").check();
await sarah.getByLabel("Other requirements (optional)").fill("Wheelchair access for patients, and a hearing loop if available.");
await sarah.getByLabel("I have read and accept the terms and conditions").check();
await shot(sarah, "05-request-form-completed");

await sarah.getByRole("button", { name: "Continue" }).click();
await sarah.getByRole("heading", { name: /check your answers/i }).waitFor();
await shot(sarah, "06-check-answers");

await sarah.getByRole("button", { name: "Submit request" }).click();
const reference = (await sarah.getByTestId("confirmation-reference").textContent()).trim();
await shot(sarah, "07-request-submitted");

await sarah.goto(`${BASE}/requests`);
await sarah.getByRole("link", { name: reference }).waitFor();
await shot(sarah, "08-my-requests-with-new-request");
await sarah.getByRole("link", { name: reference }).click();
await sarah.getByRole("heading", { name: "Clinical space request" }).waitFor();
await shot(sarah, "09-request-detail-submitted");
const requestPath = new URL(sarah.url()).pathname;

// ---- Administrator: review and decide --------------------------------------------------------
const adminContext = await browser.newContext(desktop);
const olivia = await adminContext.newPage();
await signIn(olivia, "olivia.bennett");
await olivia.getByRole("link", { name: reference }).waitFor();
await shot(olivia, "10-admin-requests-to-review");

await olivia.getByRole("searchbox", { name: "Search" }).fill("St George");
await olivia.getByRole("button", { name: /All$/ }).click();
await olivia.getByRole("button", { name: "Apply" }).click();
await olivia.getByTestId("result-count").waitFor();
await shot(olivia, "11-admin-search-and-filter");

await olivia.goto(`${BASE}/admin${requestPath}`);
await olivia.getByRole("heading", { name: /review request/i }).waitFor();
await shot(olivia, "12-admin-review-request");

await olivia.getByRole("radio", { name: "Reject" }).check();
await olivia.getByRole("button", { name: "Reject request" }).click();
await olivia.getByRole("link", { name: "Enter a reason for rejecting the request" }).waitFor();
await shot(olivia, "13-admin-reject-needs-reason");

await olivia.getByRole("radio", { name: "Accept" }).check();
await olivia.getByLabel(/notes for the requester/i).fill("Room 2 is booked for you. Collect the key from main reception.");
await olivia.getByRole("button", { name: "Accept request" }).click();
await olivia.getByRole("heading", { name: `Request ${reference} accepted` }).waitFor();
await shot(olivia, "14-admin-request-accepted");

await olivia.goto(`${BASE}/admin/requests?status=Accepted`);
await olivia.getByRole("link", { name: reference }).waitFor();
await shot(olivia, "15-admin-accepted-list");

// ---- Requester sees the decision --------------------------------------------------------------
await sarah.goto(`${BASE}/requests`);
await sarah.getByRole("link", { name: reference }).waitFor();
await shot(sarah, "16-my-requests-status-updated");
await sarah.goto(`${BASE}${requestPath}`);
await sarah.getByText(/Accepted by Olivia Bennett/).waitFor();
await shot(sarah, "17-request-detail-accepted");

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

// ---- Access control: another requester cannot open Sarah's request -----------------------------
const jamesContext = await browser.newContext(desktop);
const james = await jamesContext.newPage();
await signIn(james, "james.wright");
await james.goto(`${BASE}${requestPath}`);
await james.getByRole("heading", { name: "Page not found" }).waitFor();
await shot(james, "20-access-control-other-users-request");

// ---- Mobile -------------------------------------------------------------------------------------
const phoneContext = await browser.newContext(mobile);
const phone = await phoneContext.newPage();
await signIn(phone, "sarah.khan");
await shot(phone, "21-mobile-my-requests");
await phone.goto(`${BASE}/requests/new`);
await phone.getByRole("button", { name: "Continue" }).click();
await shot(phone, "22-mobile-request-form-validation", false);

// ---- API reference (development only) -------------------------------------------------------------
await olivia.goto(`${BASE}/api/docs`);
await olivia.waitForTimeout(3000);
await shot(olivia, "23-api-reference", false);

await browser.close();
console.log(`Done. Reference used: ${reference}`);
