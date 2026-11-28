# MPFT Clinic Space

[![CI](https://github.com/Trojan9/mpft-clinic-space/actions/workflows/ci.yml/badge.svg)](https://github.com/Trojan9/mpft-clinic-space/actions/workflows/ci.yml)

An internal web application where staff **request a clinical space** and administrators **accept or reject** those requests. The requester gets an **email** when a decision is made.

This is my submission for the Band 6 Applications Developer practical exercise at Midlands Partnership University NHS Foundation Trust. It is built on the Trust's core stack: **.NET 10 (C#) Web API, SQL Server, React, TypeScript and Tailwind CSS**. It runs with one command in Docker.

<p align="center">
  <img src="docs/screenshots/12-admin-review-request.png" alt="Administrator reviewing a clinical space request, with a warning about an overlapping booking" width="560">
</p>

## Contents

1. [Quick start](#1-quick-start)
2. [Test accounts](#2-test-accounts)
3. [What it does](#3-what-it-does)
4. [Running locally without Docker](#4-running-locally-without-docker)
5. [Database, migrations and seed data](#5-database-migrations-and-seed-data)
6. [Email notifications: how to see them](#6-email-notifications-how-to-see-them)
7. [Running the tests](#7-running-the-tests)
8. [Architecture](#8-architecture)
9. [Security](#9-security)
10. [Accessibility and design](#10-accessibility-and-design)
11. [Assumptions](#11-assumptions)
12. [Design decisions and trade-offs](#12-design-decisions-and-trade-offs)
13. [Use of AI-assisted development tools](#13-use-of-ai-assisted-development-tools)
14. [What I would do next](#14-what-i-would-do-next)
15. [Screenshots](#15-screenshots)
16. [Repository layout and further documentation](#16-repository-layout-and-further-documentation)

---

## 1. Quick start

**You need:** Docker Desktop (or Docker Engine with Compose v2). Nothing else: the .NET SDK, Node and SQL Server all run in containers.

```bash
git clone https://github.com/Trojan9/mpft-clinic-space.git
cd mpft-clinic-space
docker compose up --build
```

The first start takes a few minutes while the images build and SQL Server initialises. When the `web` container reports healthy, open:

| What | Where |
|---|---|
| **The app** | http://localhost:8080 |
| **Email inbox** (Mailpit: catches every email the app sends) | http://localhost:8025 |
| **API reference** (OpenAPI, development only) | http://localhost:8080/api/docs |
| SQL Server (for SSMS or Azure Data Studio) | `localhost,1433`, user `sa`, password `Dev_Only_Passw0rd!` |

On start-up the API applies the database migrations, imports the **supplied exercise data** (locations and user accounts from `data/exercise-data.xlsx`), and creates a few sample requests so every screen has something to show.

Mailpit has no sign-in on purpose. It's a local development tool standing in for a real mail server, it only listens on `127.0.0.1`, and it isn't part of the app. In production the app sends through the Trust's mail relay (NHSmail), and people read their emails in Outlook. The sign-in and role checks the brief asks for apply to the app itself.

To stop it: `docker compose down`. To reset everything, including the database: `docker compose down -v`.

> **Apple Silicon (M1/M2/M3) Macs:** Microsoft publishes SQL Server images for x86-64 only. Compose sets `platform: linux/amd64`, and Docker Desktop runs it through Rosetta. Turn on *Settings → General → Use Rosetta for x86_64/amd64 emulation* if SQL Server fails to start.

> **Port already in use?** Ports 8080, 8025 and 1433 are bound to `127.0.0.1` only. Stop whatever is using them, or change the left-hand port numbers in `docker-compose.yml`.

## 2. Test accounts

The accounts come from the **Users sheet of the supplied exercise workbook**. Sign in with the **email address** as the username. Passwords are hashed (PBKDF2) when they're imported; the plain-text values are never stored.

| Role | Accounts | Password |
|---|---|---|
| Administrator | `pat.jones@example.nhs.uk`, `aman.singh@example.nhs.uk`, `sam.taylor@example.nhs.uk`, `jordan.brown@example.nhs.uk` | `AdminPass!23` |
| Requester | `alex.evans@example.nhs.uk`, `riley.green@example.nhs.uk`, `morgan.wood@example.nhs.uk`, `sara.khan@example.nhs.uk`, `jamie.clarke@example.nhs.uk`, `lee.wilson@example.nhs.uk`, `casey.hughes@example.nhs.uk`, `taylor.patel@example.nhs.uk`, `drew.cooper@example.nhs.uk`, `kai.morris@example.nhs.uk` | `Password123!` |

The sample requests belong to **Alex Evans** and **Casey Hughes**, so sign in as `alex.evans@example.nhs.uk` to see a populated My Requests. Two administrator accounts make it easy to show two people deciding the same request (the second gets a conflict message).

The sign-in page lists two of these accounts in local builds only (the `VITE_SHOW_DEMO_ACCOUNTS` build flag).

## 3. What it does

### Requester
- **Signs in** with a username and password.
- **Creates a request.** The form shows their name and email from their account (read-only). It captures:
  - cost code, budget holder name and email
  - location, chosen with dependent drop-downs: **Region → Site → Building**
  - start and end date and time, in UK time
  - sink required (yes/no), clinical couch required (yes/no) and optional other requirements
  - acceptance of the placeholder terms and conditions
- **Checks their answers** on a summary page with *Change* links, then submits. The request is saved with status **Submitted**, and they get a reference such as `CSR-000123` and a confirmation email.
- **Views My Requests**, with status tags, a status filter and paging. They can open any of their own requests to see the details and a history timeline.

### Administrator
- **Views all requests**, opening on the "To review" queue. Status cards show counts. The list can be searched (reference, requester, cost code, budget holder, site or building), filtered by region, sorted and paged.
- **Reviews a request**, including a warning if other requests for the same building overlap in time.
- **Accepts or rejects it.** A reason is required to reject; notes are optional when accepting. The status updates at once, the requester sees it in My Requests, and they are **emailed**.

### Extras beyond the brief
- Audit trail: created and updated timestamps on every record, plus an append-only event history per request (who did what, and when).
- Optimistic concurrency: if two administrators decide the same request, the second gets a clear "changed by someone else" message.
- A confirmation email on submission, as well as the decision email.
- Clash warning for overlapping bookings in the same building.
- Location importer for CSV or Excel, so the supplied data file can be loaded without code changes.
- Accessibility to WCAG 2.2 AA, checked automatically (axe) in component tests and in a real browser.
- A mobile layout with no sideways scrolling.

## 4. Running locally without Docker

Use this when you want hot reload while developing.

**You need:** .NET 10 SDK, Node.js 22, and SQL Server (local install, or just the database container).

```bash
# 1. Database only
docker compose up -d db mail

# 2. API on http://localhost:5080 (applies migrations and seeds data in Development)
cd backend
dotnet run --project src/ClinicSpace.Api

# 3. Web app on http://localhost:5173 (Vite proxies /api to the API)
cd frontend
npm ci
VITE_SHOW_DEMO_ACCOUNTS=true npm run dev
```

Development settings are in `backend/src/ClinicSpace.Api/appsettings.Development.json`. Any setting can be overridden with an environment variable, using `__` for nesting (for example `Email__Mode=File`).

## 5. Database, migrations and seed data

- **Database:** SQL Server 2022 (the Developer edition in Docker). Database name `ClinicSpace`.
- **Migrations:** EF Core code-first migrations in `backend/src/ClinicSpace.Infrastructure/Persistence/Migrations`.
  - **Local:** applied automatically at start-up when `Seed__ApplyMigrations=true` (as set in Docker Compose).
  - **By hand:** from `backend/`:
    ```bash
    dotnet tool restore
    dotnet ef database update -p src/ClinicSpace.Infrastructure -s src/ClinicSpace.Infrastructure
    ```
  - **For a DBA or controlled release** (the approach I'd expect in production), generate an idempotent SQL script, review it and run it through change control:
    ```bash
    dotnet ef migrations script --idempotent -p src/ClinicSpace.Infrastructure -s src/ClinicSpace.Infrastructure -o migrations.sql
    ```
  - **New migration:** `dotnet ef migrations add <Name> -p src/ClinicSpace.Infrastructure -s src/ClinicSpace.Infrastructure -o Persistence/Migrations`
- **Seed data** (each step only runs if its table is empty, so restarts are safe):
  - **Locations** come from the **Buildings** sheet of the supplied workbook, `data/exercise-data.xlsx`: 11 regions, 22 sites and 44 buildings.
  - **Users** come from its **Users** sheet: 14 accounts (4 administrators, 10 requesters). "Admin" maps to the Administrator role, and passwords are hashed on import.
  - The importers accept `.xlsx` or `.csv`, match column names loosely, skip blank or invalid rows (and log why), and only ever add, so re-running is safe. To run them against a live system:
    ```bash
    docker compose run --rm api import-locations /app/data/exercise-data.xlsx
    docker compose run --rm api import-users /app/data/exercise-data.xlsx
    ```
  - **Sample requests** (6, across all three statuses) are created when `Seed__CreateDemoData=true` (Docker only), so every screen has data to show. See [`data/README.md`](data/README.md).

The data model is described, with an entity relationship diagram, in [docs/DATA-MODEL.md](docs/DATA-MODEL.md).

## 6. Email notifications: how to see them

Emails are written to an **outbox table in the same database transaction** as the decision, then sent by a background worker that retries with exponential back-off. So a decision can never be saved without its email, and an SMTP outage doesn't lose notifications.

| `Email__Mode` | What happens | Used by |
|---|---|---|
| `Smtp` | Sent over SMTP (MailKit). Host, port, TLS and credentials are all configurable. | **Docker Compose → Mailpit** |
| `File` | Each email is written as a `.eml` file to `Email__FileDirectory`; open it in any mail client. | No-server option |
| `Log` | The email is written to the application log. | Default, tests |

**To demonstrate it:**
1. `docker compose up --build`
2. Sign in as `alex.evans@example.nhs.uk` (`Password123!`) and submit a request. A "We have received your request" email appears at http://localhost:8025.
3. Sign out, sign in as `pat.jones@example.nhs.uk` (`AdminPass!23`), open the request and **Accept** or **Reject** it.
4. Within about 5 seconds the "has been accepted/rejected" email appears in Mailpit, addressed to Alex, with the reference, location, time, facilities, the administrator's notes and a link back to the request.

To use a real relay instead, set `Email__SmtpHost`, `Email__SmtpPort`, `Email__SmtpSecurity=StartTls`, `Email__SmtpUsername` and `Email__SmtpPassword` (from a secret store, never committed). In the Trust this would most likely be NHSmail SMTP or Microsoft Graph.

## 7. Running the tests

There are about 190 automated tests at four levels. All of them run in [GitHub Actions](https://github.com/Trojan9/mpft-clinic-space/actions) on every push and pull request.

| Level | Tools | What it covers | Command |
|---|---|---|---|
| Backend unit | xUnit, FluentAssertions | Request lifecycle rules, lockout, email retry back-off, every validation rule, UK time formatting (GMT/BST), email HTML encoding, CSV/XLSX import | `cd backend && dotnet test tests/ClinicSpace.UnitTests` |
| Backend integration | xUnit, WebApplicationFactory, **Testcontainers (real SQL Server 2022)** | The whole HTTP API: sign-in, lockout, sessions, CSRF, role policies, ownership (404 for other people's requests), validation, decisions, concurrency (409), search/filter/paging, outbox sending, health, security headers | `cd backend && dotnet test tests/ClinicSpace.IntegrationTests` (needs Docker) |
| Frontend | Vitest, Testing Library, **vitest-axe** | Date/time-zone logic, form rules, API client (CSRF, errors, 401), route guards, sign in, the request form journey, My Requests, administrator decisions, and automated accessibility checks | `cd frontend && npm ci && npm test` |
| End to end | **Playwright**, axe-core | Real browser against the full Docker stack: both journeys, email arriving in Mailpit, access control, mobile layout, WCAG 2.2 AA scans with colour contrast, keyboard skip link | `docker compose up -d --build --wait`, then `cd e2e && npm ci && npx playwright install chromium && npx playwright test` |

Coverage: `dotnet test --collect "XPlat Code Coverage"` and `npm run test:coverage`. CI uploads test results, the Playwright report and freshly captured screenshots as build artifacts.

More detail on the testing approach is in [docs/TESTING.md](docs/TESTING.md).

## 8. Architecture

```
Browser ──► nginx (web container :8080) ──┬─► React SPA (static files)
                                          └─► /api/* ──► ASP.NET Core API (:8080 internal) ──► SQL Server
                                                                     │
                                                                     └─► Outbox worker ──► SMTP (Mailpit)
```

- **One origin.** nginx serves the React build and proxies `/api` to the API, so the browser never makes cross-origin calls. That lets the session cookie be `SameSite=Strict` and removes any need for CORS.
- **Backend layers:** I used Clean Architecture, kept small.
  - `Domain`: entities and business rules, with no framework references.
  - `Application`: use cases, validation and interfaces.
  - `Infrastructure`: EF Core, SQL Server, email and import.
  - `Api`: HTTP, authentication and composition.
- **Frontend:** React 19 + TypeScript (strict), React Router, TanStack Query for server state, React Hook Form + Zod for forms, and Tailwind CSS with NHS design tokens.

Details, diagrams and request flows: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md). Decision records: [docs/adr](docs/adr).

## 9. Security

Security was designed in from the first sprint. The full OWASP Top 10 mapping is in [docs/SECURITY.md](docs/SECURITY.md).

- **Passwords:** never stored. They're hashed with PBKDF2-HMAC-SHA512 (100,000 iterations, random salt) through ASP.NET Core Identity's `PasswordHasher`, and old hashes are upgraded automatically.
- **Brute force:** the account locks for 15 minutes after 5 failures. Sign-in is rate limited per IP address. The error message is the same whether the username or the password is wrong, and checking an unknown user takes the same time, so attackers can't tell which accounts exist.
- **Sessions:**
  - An HttpOnly, `SameSite=Strict` cookie that scripts can't read. No tokens are kept in `localStorage`.
  - 30-minute idle timeout and 8-hour absolute limit.
  - The account is re-checked on every request, so disabling a user or changing their role takes effect at once.
- **CSRF:** anti-forgery token (double-submit) on every state-changing call, on top of `SameSite=Strict`.
- **Authorisation:**
  - Every endpoint requires sign-in unless explicitly marked otherwise.
  - Role policies decide who can create and who can decide.
  - Ownership is checked in the service layer. A requester asking for someone else's request gets **404**, not 403, so IDs can't be probed.
- **Input:**
  - Server-side validation is authoritative; the client mirrors it for speed.
  - EF Core parameterises every query.
  - Request bodies are capped at 64 KB.
  - User text is HTML-encoded in emails.
- **Output:**
  - Errors are RFC 9457 problem details with a trace ID and no stack traces.
  - Strict Content Security Policy (no inline or third-party scripts), `X-Frame-Options: DENY`, `nosniff`, `Referrer-Policy: no-referrer`.
  - API responses are `Cache-Control: no-store`.
- **Platform:**
  - Both containers run as non-root users.
  - Ports are bound to localhost only.
  - Secrets come from environment variables, and none are committed (development defaults are labelled as such).
  - Dependabot and CodeQL are set up.
  - CI fails on high or critical vulnerable packages.

In production, sign-in would move to **Microsoft Entra ID** (OpenID Connect) and the role policies would read Entra app roles. Section 14 explains why this design makes that a small change.

## 10. Accessibility and design

- The UI follows the **NHS digital service manual**: its colours, type scale, focus styles and components, including the error summary, summary list, *check your answers*, confirmation panel, tags, inset text, warning callout, details and character count. Research and references are in [docs/DESIGN.md](docs/DESIGN.md).
- It targets **WCAG 2.2 AA**:
  - skip link, landmarks and one `h1` per page, with focus moved to it on navigation
  - page titles prefixed "Error:" when a form has problems
  - labels, hints and errors linked with `aria-describedby`, and an error summary that takes focus and links to each field
  - date entry in separate day, month and year boxes, as the NHS recommends
  - status shown in words, never by colour alone
  - visible yellow focus states, reduced-motion support, and responsive tables that stack on small screens
- This is checked by axe in the component tests and, in a real browser, by the Playwright suite (which includes colour contrast). See [docs/ACCESSIBILITY.md](docs/ACCESSIBILITY.md).

## 11. Assumptions

Where the brief left things open, I made these decisions. Each is easy to change, and most are configuration.

| # | Assumption | Where to change it |
|---|---|---|
| A1 | Each user has **one role**: Requester or Administrator. Administrators process requests but don't create them. As a safeguard, the domain also stops anyone deciding their own request. | `UserRole`, policies in `AuthenticationSetup` |
| A2 | Users are created by seeding or by an administrator outside the app. Self-registration and password reset are out of scope, because in live use Microsoft Entra ID provides identity. | n/a |
| A3 | **Cost code** format: letters, numbers and single hyphens, up to 20 characters (for example `CC-4510`), stored in upper case. The Trust's real format wasn't given. | `CreateRequestValidator`, `requestSchema.ts` |
| A4 | **Budget holder email:** any valid address. An allow-list of domains (for example `nhs.net`, `mpft.nhs.uk`) can be switched on. | `Requests__AllowedBudgetHolderEmailDomains` |
| A5 | **Times are UK local time** (Europe/London). They're stored as `datetimeoffset` (UTC) and always shown in UK time, so the change to and from British Summer Time is handled correctly. | `UkDateFormatter`, `dates.ts` |
| A6 | **Booking length** is 30 minutes to 31 days. The start must be in the future and within 365 days. Times are chosen in 15-minute steps. | `Requests__*` settings |
| A7 | A request is for **a building**, not a specific room: the data has no room level. Administrators see a warning about overlapping requests rather than a hard block, because a building usually has several rooms. | `RequestService.GetAsync` |
| A8 | **Accepted and Rejected are final.** Cancelling, withdrawing or changing a request isn't in the brief, so it's not built (see *What I would do next*). | `ClinicalSpaceRequest.Decide` |
| A9 | **Rejecting needs a reason**, which is shown to the requester and included in the email. Notes are optional when accepting. | `DecideRequestValidator` |
| A10 | The requester's name and email are **copied onto the request** when it's submitted, so the record and emails stay correct if the account changes later. | `ClinicalSpaceRequest.Submit` |
| A11 | The terms and conditions are placeholder text, versioned (`2026-11-v1`). The accepted version and time are stored with each request. | `Requests__TermsVersion` |
| A12 | The supplied workbook is the source for locations and users. The **email address is the sign-in username**, because the Users sheet has no separate username. "Admin" means Administrator. | `data/README.md`, `UserFileReader` |
| A13 | The supplied passwords are test credentials for this exercise, so the workbook is committed as supplied. The app hashes them on import and never stores or logs the plain text. Live users would come from Microsoft Entra ID, with no passwords held by the app. | `UserImporter`, `docs/SECURITY.md` |

## 12. Design decisions and trade-offs

Each of these has a short record in [docs/adr](docs/adr).

| Decision | Why | Trade-off |
|---|---|---|
| **.NET 10 + SQL Server + React/TypeScript** | It's the team's stack. .NET 10 is the current LTS release, supported to November 2028. | .NET 11 is newer, but it's a short-term release. |
| **Cookie session, not JWT** (ADR 0003) | For a same-origin browser app, an HttpOnly cookie can't be stolen by injected script, and logout and disabling a user take effect at once. | It needs CSRF protection (added), and suits browsers rather than third-party API clients (which would use Entra tokens). |
| **Clean Architecture, kept light** (ADR 0002) | The business rules are testable without a database, and swapping auth or email doesn't touch them. | There are more projects than a single-project app. I didn't add MediatR or repositories over EF Core, which would be ceremony at this size (ADR 0004). |
| **Real SQL Server in integration tests** (Testcontainers) | It catches sequence, check-constraint, `rowversion` and collation behaviour that an in-memory provider would hide. | Tests need Docker and take about a minute longer. |
| **Transactional outbox for email** (ADR 0005) | A decision and its email are saved together; sending retries, and permanent failures are dead-lettered. | Emails go out a few seconds later, not instantly. One worker is assumed; scaling out needs row locking or a queue. |
| **Optimistic concurrency** (`rowversion`) | Two administrators can't overwrite each other's decision. | The second administrator has to reload, and the UI offers that. |
| **Status stored as text + check constraint** | Readable in SQL, safe to report on, and enforced by the database as well as the code. | Slightly bigger than an int; a lookup table would suit statuses that admins can configure. |
| **NHS service manual design** (ADR 0006) | Familiar to NHS staff, accessible by default, and tested with users at scale. | It's not a bespoke visual identity, which is the right call for an internal NHS tool. |
| **Check your answers step** | NHS pattern; it cuts errors before submission. | One extra page. |
| **Separate date boxes, not a date picker** | NHS and GOV.UK research shows native pickers cause accessibility problems. | More fields to fill in. |

## 13. Use of AI-assisted development tools

I used **Claude Code (Anthropic)** as an AI pair programmer, with me as the human in the loop, which is the way the job description describes. I set the direction, made the architectural decisions, and reviewed and tested what it produced.

**Where it helped:**
- Scaffolding projects and boilerplate: EF configurations, DTOs, Tailwind components.
- Drafting tests and the first versions of documentation.
- Working through framework details such as the MailKit 5 API changes, Vitest typings and Testcontainers set-up.

**How I validated and adapted its output:**
- Compiled everything with *warnings as errors* and analysers on, and ran every test suite in CI.
- Ran the whole application in Docker and walked through both journeys in a real browser. This found and fixed real bugs that compiled cleanly:
  - Clearing the form draft before navigating sent users back to an empty form after a successful submit. It now clears on the confirmation page.
  - A Zod `superRefine` silently hid date errors until every other field was valid. Field and date rules now run independently.
  - A label and selector clash broke the search box.
  - Focus moved to the heading on first load, so keyboard users' first Tab skipped the "Skip to main content" link. Found by the Playwright keyboard test; fixed.
  - CI's `npm audit` gate flagged a high-severity advisory in Zod 3, so I upgraded to Zod 4.
- Checked security-sensitive code by hand and tested it: password hashing, lockout, CSRF, cookie flags, ownership checks and HTML encoding.
- Checked accessibility with axe and by hand against the NHS service manual.

I can explain and justify every part of the code, and the decision records show the reasoning behind the main choices.

## 14. What I would do next

1. **Microsoft Entra ID single sign-on.** Swap the cookie scheme for `Microsoft.Identity.Web` (OpenID Connect) and map Entra **app roles** to the existing `Requester` and `Administrator` policies. Users would be created on first sign-in, and the password and lockout code would be deleted. Controllers and services wouldn't change.
2. **Azure deployment.** App Service or Container Apps, Azure SQL, Key Vault for secrets and data-protection keys, and Application Insights for monitoring. CI/CD with staging slots and an approval gate, and migrations run as a reviewed idempotent script.
3. **Request lifecycle.** Requester can withdraw a request; admins can reopen or amend one; a "cancelled" status; reminders for requests not reviewed within N days.
4. **Rooms and capacity.** Add a room level and room capacity, so clashes can be checked exactly instead of warned about.
5. **Admin tools.** User and location management screens, CSV export, and dashboards (for example, requests per site per month).
6. **Notifications.** Email the budget holder too, use NHSmail or Microsoft Graph, add Teams notifications, and add a dead-letter dashboard.
7. **Assurance.**
   - DCB0129 clinical safety review if the service ever carries clinical risk, and a DTAC self-assessment.
   - A DPIA, pen testing, load testing (k6), and backup and restore drills.
8. **Accessibility.** An audit with screen reader users and an accessibility statement.

## 15. Screenshots

All screenshots are produced by [`e2e/scripts/capture-screenshots.mjs`](e2e/scripts/capture-screenshots.mjs) against the running stack. Run `cd e2e && npm run screenshots` to regenerate them; CI also captures a fresh set on every build.

### Sign in
| Sign-in page | Validation |
|---|---|
| ![Sign in page](docs/screenshots/01-sign-in.png) | ![Sign in errors](docs/screenshots/02-sign-in-validation.png) |

### Creating a clinical space request
| Form with error summary | Completed form |
|---|---|
| ![Form validation](docs/screenshots/04-request-form-validation.png) | ![Completed form](docs/screenshots/05-request-form-completed.png) |

| Check your answers | Confirmation with reference |
|---|---|
| ![Check answers](docs/screenshots/06-check-answers.png) | ![Submitted](docs/screenshots/07-request-submitted.png) |

### My Requests and statuses
| My Requests | Request details and history |
|---|---|
| ![My requests](docs/screenshots/08-my-requests-with-new-request.png) | ![Detail](docs/screenshots/09-request-detail-submitted.png) |

### Administrator: view, review, accept or reject
| All requests ("To review" queue) | Search and filter |
|---|---|
| ![Admin list](docs/screenshots/10-admin-requests-to-review.png) | ![Search](docs/screenshots/11-admin-search-and-filter.png) |

| Review with clash warning | Reject needs a reason |
|---|---|
| ![Review](docs/screenshots/12-admin-review-request.png) | ![Reject validation](docs/screenshots/13-admin-reject-needs-reason.png) |

| Accepted | Accepted list |
|---|---|
| ![Accepted](docs/screenshots/14-admin-request-accepted.png) | ![Accepted list](docs/screenshots/15-admin-accepted-list.png) |

### The requester sees the decision
| My Requests updated | Detail with decision |
|---|---|
| ![Updated](docs/screenshots/16-my-requests-status-updated.png) | ![Accepted detail](docs/screenshots/17-request-detail-accepted.png) |

### Email notification (Mailpit)
| Inbox | Decision email |
|---|---|
| ![Inbox](docs/screenshots/18-email-inbox-mailpit.png) | ![Email](docs/screenshots/19-email-request-accepted.png) |

### Access control, mobile and API
| Another user's request → not found | Mobile | API reference |
|---|---|---|
| ![Access control](docs/screenshots/20-access-control-other-users-request.png) | ![Mobile](docs/screenshots/21-mobile-my-requests.png) | ![API docs](docs/screenshots/23-api-reference.png) |

## 16. Repository layout and further documentation

```
backend/                     .NET 10 solution (ClinicSpace.slnx)
  src/ClinicSpace.Domain           Entities and business rules
  src/ClinicSpace.Application      Use cases, validation, DTOs, email templates
  src/ClinicSpace.Infrastructure   EF Core + SQL Server, migrations, email, import, seeding
  src/ClinicSpace.Api              Controllers, auth, middleware, Program.cs
  tests/                           Unit and integration tests
frontend/                    React + TypeScript + Tailwind (Vite), nginx config
e2e/                         Playwright end-to-end tests and screenshot capture
data/                        Location data (sample) and import instructions
docs/                        Plan, architecture, data model, security, testing, design, ADRs, runbook
docker-compose.yml           Full local stack
.github/                     CI, CodeQL and Dependabot
```

| Document | Contents |
|---|---|
| [docs/PLAN.md](docs/PLAN.md) | Delivery plan: phases, sprints, definition of done, risks |
| [docs/REQUIREMENTS.md](docs/REQUIREMENTS.md) | Every requirement in the brief traced to code and tests |
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | Components, request flows, sequence diagrams, configuration |
| [docs/DATA-MODEL.md](docs/DATA-MODEL.md) | Entity relationship diagram, tables, constraints, indexes |
| [docs/SECURITY.md](docs/SECURITY.md) | OWASP Top 10 mapping, threat notes, production checklist |
| [docs/TESTING.md](docs/TESTING.md) | Test strategy, what each level proves, how to run |
| [docs/DESIGN.md](docs/DESIGN.md) / [docs/ACCESSIBILITY.md](docs/ACCESSIBILITY.md) | UI research, NHS patterns used, WCAG checklist |
| [docs/RUNBOOK.md](docs/RUNBOOK.md) | Operating and supporting the service: health checks, logs, failed emails, common faults |
| [docs/adr/](docs/adr) | Architecture decision records |
| [docs/DECISIONS-QA.md](docs/DECISIONS-QA.md) | Likely reviewer questions about the decisions, with the reasoning |
