# Decisions Q&A

Questions a reviewer is likely to ask about this solution, with the reasoning behind each answer. It complements the [decision records](adr) and the README sections on [assumptions](../README.md#11-assumptions) and [trade-offs](../README.md#12-design-decisions-and-trade-offs).

---

## Approach and scope

**How did you approach the exercise?**
I wrote a plan before any code ([PLAN.md](PLAN.md)): phases and sprints, a definition of done, and a risk list. I mapped every requirement in the brief to a design ([REQUIREMENTS.md](REQUIREMENTS.md)), then built the core workflow end to end before any extras. Each sprint went to its own branch and pull request, and CI had to be green before merging.

**How did you keep the scope proportionate?**
The brief's core is request → review → decision → email, so that came first and works fully. I added extras only where they made the core safer or more realistic: concurrency, the email outbox, a clash warning, an audit trail and accessibility. I left out things like rooms, cancellations and admin user management, and listed them under "what I would do next".

**What would you do differently with more time?**
Entra ID single sign-on, a room level with capacity so clashes can be blocked rather than warned about, withdraw/cancel, admin screens for users and locations, contract tests between the API and the TypeScript types, load testing, and a manual accessibility audit with screen reader users.

---

## Technology choices

**Why .NET 10 and not .NET 11?**
.NET 10 is the long-term support (LTS) release, supported until November 2028. .NET 11 is a standard-term release with 18 months of support. For a line-of-business NHS app, fewer forced upgrades is the safer choice.

**Why SQL Server rather than PostgreSQL?**
It's the Trust's database, so the team can support it, DBAs can review scripts, and it fits existing backup and monitoring. The design also uses SQL Server features deliberately: sequences, `rowversion` and check constraints.

**Why controllers rather than Minimal APIs?**
Controllers group related endpoints, carry attributes for authorisation, rate limiting and response types clearly, and are familiar to most .NET teams. Minimal APIs would also work; for a team codebase, I chose the more conventional option.

**Why React with TanStack Query, React Hook Form and Zod?**
React and TypeScript are the team's stack. TanStack Query handles caching, refetching and invalidation, so screens stay current without hand-written state. React Hook Form keeps large forms fast. Zod gives typed, declarative client-side rules that mirror the server's.

**Why Tailwind instead of the `nhsuk-frontend` package?**
The team uses Tailwind. I implemented the NHS design tokens and components in Tailwind and React, so there's one styling approach and no legacy global CSS or JavaScript. The trade-off is keeping in step with service manual updates by hand (ADR 0006).

---

## Architecture

**Why four backend projects? Isn't that over-engineered?**
The rules (lifecycle, reason required, no self-approval) need to be testable without a database, and the parts most likely to change (authentication, email, hosting) need to be swappable. Domain, Application, Infrastructure and Api achieve that with little ceremony. I deliberately didn't add MediatR, CQRS or generic repositories (ADRs 0002 and 0004).

**Why no repository pattern?**
EF Core's `DbContext` is already a unit of work and `DbSet` is already a repository. Wrapping them hides useful features (projections, includes, concurrency tokens) without making testing easier, especially since the integration tests use a real SQL Server.

**Where do the business rules live, and why?**
In the domain entity: `ClinicalSpaceRequest.Submit`, `Accept` and `Reject`. They're the only ways to change a request's state, and each records an audit event. Wherever a change starts, the rules can't be bypassed.

**Why does the browser talk to one origin through nginx?**
Same origin means no CORS, and lets the session cookie be `SameSite=Strict`, which blocks most CSRF on its own. It also mirrors a typical production set-up behind a gateway.

---

## Data model

**Why GUIDs for IDs and also a reference number?**
GUIDs in URLs can't be guessed and don't reveal volumes. People need something short to quote on the phone, so a database sequence produces `CSR-000123`.

**Why copy the requester's name and email onto the request?**
So the record and its emails stay accurate if the account changes later. It also means the audit trail shows who it was at the time.

**Why store status as text with a check constraint, not an int or a lookup table?**
It's readable in SQL and reports, and the database refuses invalid values even if the code has a bug. A lookup table would make sense if admins needed to configure statuses; they don't here.

**Why `datetimeoffset` and UTC?**
It records unambiguous instants. Showing times in UK time is a presentation concern, handled in one place on each side, so the change to and from British Summer Time is correct.

**Why `IsActive` on locations instead of deleting them?**
Old requests must keep pointing at real buildings. Deactivating a building hides it from new requests without breaking history.

**How did you handle the supplied data?**
The workbook has two sheets, and both are imported at start-up. **Buildings** (Region, Site, Building) fills the location hierarchy: 11 regions, 22 sites, 44 buildings. **Users** (Name, Email Address, Role, Password) creates the 14 accounts. The importers read CSV or Excel, match column names loosely, report and skip bad rows instead of guessing, merge duplicates, and only ever add, so they're safe to run again. A command-line option runs them against a live database.

**Why is the email address the username?**
The Users sheet has no username column, and staff already know their work email. It also matches how sign-in works with Microsoft Entra ID.

**The supplied file contains plain-text passwords. How did you handle that?**
They're test credentials provided for the exercise, so the workbook is committed as supplied, so reviewers can sign in straight away. On import each password is hashed with PBKDF2, and the plain text is never stored or logged. In production there'd be no password file at all: accounts and roles would come from Entra ID.

**How do roles in the file map to the app?**
"Admin" (or "Administrator") becomes Administrator, and "Requester" becomes Requester. Any other value is rejected with a row-numbered message rather than defaulting to a role, because defaulting could accidentally give someone admin rights.

---

## Security

**Why cookies and not JWTs?**
For a browser app on the same origin, an HttpOnly cookie can't be read by injected script, and revocation is immediate (the session is re-checked against the database on each request). The cost is needing CSRF protection, which I added (ADR 0003).

**How are passwords stored?**
They aren't. ASP.NET Core Identity's `PasswordHasher` stores a PBKDF2-HMAC-SHA512 hash with a random salt and 100,000 iterations, and upgrades older hashes automatically.

**How do you stop brute-force attacks and account enumeration?**
Lockout after 5 failures for 15 minutes, a per-IP rate limit on sign-in, the same message for every failure, and a dummy hash check for unknown users so response times don't reveal which accounts exist.

**Why return 404 instead of 403 for someone else's request?**
A 403 confirms the request exists, which lets someone probe IDs. A 404 reveals nothing.

**What stops a requester submitting a request as someone else?**
The server takes the requester's identity from the session. Any name or email in the request body is ignored, and an integration test proves it.

**How would this work with Microsoft Entra ID?**
Replace the cookie sign-in with OpenID Connect (`Microsoft.Identity.Web`) and map Entra app roles to the existing Requester and Administrator policies. Controllers, services and the domain don't change. Local passwords and lockout are removed.

**Why doesn't the Mailpit inbox need a sign-in?**
Mailpit isn't part of the app. It's a local stand-in for a mail server, so reviewers can see the emails without a real mailbox. Docker only exposes it on `127.0.0.1`, so nothing else on the network can reach it. In production it isn't deployed: the app sends through the Trust's relay (NHSmail) over TLS, and each person reads their own emails in Outlook. The sign-in and role rules in the brief cover the app, and the app enforces them.

**What would need to change before going live?**
TLS everywhere with secure cookies, secrets in Key Vault, a least-privilege SQL login, shared data-protection keys, a DPIA, a penetration test and a gateway-level rate limit. The production checklist is in [SECURITY.md](SECURITY.md).

---

## Reliability and support

**What happens if two administrators decide the same request at the same time?**
The request has a `rowversion`. The second save matches 0 rows, so EF Core raises a concurrency exception, the API returns 409, and the UI explains that the request changed and offers a reload. An integration test covers this.

**Why an outbox for emails instead of sending straight away?**
Sending inside the request either fails the decision when SMTP is down, or loses the email after the decision is saved. The outbox saves both in one transaction. A background worker then sends, retries with back-off, and dead-letters after 8 attempts (ADR 0005).

**Does the outbox work with several API instances?**
As written, it assumes one. To scale out, claim rows with `UPDLOCK, READPAST` or move to a message queue such as Azure Service Bus.

**How would you support this in production?**
Through the runbook ([RUNBOOK.md](RUNBOOK.md)): health endpoints for probes, structured logs, and a correlation ID on every response that users can quote. It also has queries for failed emails and locked accounts, and a release and rollback procedure.

**How are migrations released safely?**
Generate an idempotent SQL script, review it, and apply it through change control before deploying the new API. Migrations are additive, so the old version keeps working during the release.

---

## Testing

**Why use a real SQL Server in the integration tests?**
Several rules live in the database: check constraints, the reference sequence, `rowversion` and unique indexes. An in-memory provider would pass tests that production would fail.

**How did you test date and time rules reliably?**
The clock is injected (`TimeProvider`), so tests run at exact boundaries. The frontend tests include both clock-change days.

**How do you know the UI is accessible?**
ESLint's `jsx-a11y` rules, axe in the component tests, axe-core in a real browser in the end-to-end tests (including colour contrast) and a keyboard test. The keyboard test found a real bug: focus on first load skipped the skip link. Automated checks catch only part of the problem, so manual screen-reader testing is on the "next" list.

**What does CI check?**
A build with warnings as errors, unit and integration tests, frontend lint, type-check and tests, a production build, dependency vulnerability checks, the full Docker stack with Playwright, and CodeQL. The `npm audit` gate caught a real high-severity advisory, fixed by upgrading Zod.

---

## UX and accessibility

**Why follow the NHS design system?**
Staff already know it, the patterns are tested with NHS users, and it's accessible by default, which public sector bodies are legally required to be.

**Why a "check your answers" page?**
It's the NHS pattern for catching mistakes before submission, and it costs one click.

**Why separate day, month and year boxes rather than a date picker?**
NHS and GOV.UK research shows native date pickers are hard to use with assistive technology. Separate boxes are predictable for everyone.

**Why does the admin list open on "To review", oldest first?**
It's a work queue. The requests that need action should be the default view, handled first in, first out.

**Why a clash warning rather than blocking double bookings?**
The data stops at building level, and a building usually has several rooms. The administrator has the context to decide; a hard block would refuse valid requests.

---

## Use of AI

**How did you use AI, and how do you know the output is right?**
I used Claude Code as a pair programmer for scaffolding, tests, documentation drafts and framework details. I set the architecture and decisions, and validated everything:
- warnings as errors and about 190 automated tests in CI
- running the whole stack and walking through both journeys in a real browser
- reviewing the security-sensitive code by hand

That process found real bugs that compiled cleanly: a navigation race after submitting, hidden date errors, the skip-link focus issue, and a vulnerable dependency. It's the human-in-the-loop approach the job description describes.

**Could you explain any part of the code without the AI?**
Yes. The decision records, this Q&A and the architecture docs set out the reasoning, and each part maps to a familiar pattern: domain rules, use cases, EF Core configuration, middleware and React components.
