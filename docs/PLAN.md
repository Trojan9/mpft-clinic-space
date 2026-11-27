# Delivery plan: MPFT Clinic Space

This plan was written before any code. It breaks the exercise into phases and sprints, and says what
"done" means for each one. Each sprint was delivered on its own branch and merged through a pull
request only after CI passed, so the Git history shows the same steps.

## 1. Goal

Build a small internal web application that lets staff **request a clinical space** and lets
administrators **accept or reject** those requests. The requester gets an **email** when a decision is
made. The work should show sound application design, data modelling, UX, API design, security,
testing and documentation, at a scope that fits the exercise.

## 2. Guiding principles

| Principle | What it means here |
|---|---|
| Align with the team's stack | .NET (C#) Web API, SQL Server, React + TypeScript + Tailwind, as listed in the job advert. |
| Core workflow first | Request → review → decision → notification works end to end before any extra. |
| Secure by default | OWASP ASVS / Top 10 controls considered from Sprint 1, not added at the end. |
| Accessible by default | NHS service manual patterns, WCAG 2.2 AA, automated axe checks. |
| One-command start | `docker compose up` runs the database, API, web app and mail catcher. |
| Tests in CI | Unit, integration (real SQL Server through Testcontainers), front-end and end-to-end tests run on every push. |
| Write it down | Each decision has a short record in `docs/adr/`. Assumptions are listed in `docs/REQUIREMENTS.md`. |

## 3. Phases and sprints

Sprints are short, so each one has a single clear outcome.

### Phase 0: Discovery and planning (Sprint 0)
- Read the brief and turn every requirement into a traceability matrix (`docs/REQUIREMENTS.md`).
- Record assumptions where the brief is open (cost code format, time zone, booking length, and so on).
- Research the UI patterns in the NHS digital service manual (`docs/DESIGN.md`).
- Choose the architecture and record the key decisions (`docs/adr/`).
- **Done when:** plan, requirements matrix, assumptions and ADRs are committed.

### Phase 1: Foundations (Sprint 1)
- Solution structure: Domain, Application, Infrastructure, Api projects, plus test projects.
- Data model and EF Core migrations for users, regions, sites, buildings, requests, audit events and the email outbox.
- Location importer (CSV or XLSX) and development seed data.
- Docker Compose: SQL Server 2022, API, web (nginx), Mailpit.
- GitHub Actions CI: build, test, lint, type-check, container build.
- **Done when:** `docker compose up` starts an empty, migrated, seeded system, and CI is green.

### Phase 2: Authentication and role-based access (Sprint 2)
- Username and password sign-in, passwords hashed with PBKDF2 (ASP.NET Core Identity `PasswordHasher`).
- HttpOnly, `SameSite=Strict` cookie session; anti-forgery token on every state-changing request.
- Account lockout after repeated failures and rate limiting on the sign-in endpoint.
- `Requester` and `Administrator` roles enforced by authorisation policies on the API.
- Sign-in page, route guards and sign-out in the web app.
- **Done when:** each role can sign in and only reach the endpoints and pages it should, and tests prove it.

### Phase 3: Requester journey (Sprint 3)
- Location look-up endpoints for dependent Region → Site → Building selection.
- Create request: server-side validation (FluentValidation) mirrored on the client (Zod).
- Requester name and email shown read-only from the account; terms and conditions must be accepted.
- My Requests list with status tags, and a request detail page with a timeline.
- **Done when:** a requester can submit a valid request, sees clear errors for an invalid one, and sees it in My Requests as *Submitted*.

### Phase 4: Administrator journey and notifications (Sprint 4)
- All-requests list with search, filters (status, region), sorting and paging.
- Review page: full details, overlapping requests for the same building, accept or reject (reject needs a reason).
- Optimistic concurrency, so two administrators cannot both decide the same request.
- Transactional email outbox and background sender; SMTP to Mailpit in development, with a file-drop option.
- **Done when:** a decision updates the status, the requester's list reflects it, and an email appears in Mailpit.

### Phase 5: Hardening (Sprint 5)
- Security headers (CSP, frame-ancestors, nosniff, referrer policy), ProblemDetails errors and no stack traces in production.
- Structured logging with correlation IDs, health checks, non-root containers.
- Accessibility pass: skip link, focus management, error summary, axe checks in component and E2E tests.
- Playwright end-to-end tests covering both journeys.
- **Done when:** the security and accessibility checklists in `docs/SECURITY.md` and `docs/ACCESSIBILITY.md` are met.

### Phase 6: Documentation and evidence (Sprint 6)
- README: run locally, database and migrations, seed data, tests, email demo, assumptions, AI usage.
- Screenshots of every journey the brief asks for (`docs/screenshots/`).
- "What I would do next" section (Entra ID, Azure deployment, and so on).
- **Done when:** a reviewer can clone the repo, follow the README and reproduce every screenshot.

## 4. Definition of done (every sprint)
1. Code builds with no warnings treated as errors in CI.
2. New behaviour has automated tests at the right level.
3. CI is green before merge, through a pull request.
4. Docs updated in the same pull request (README, ADR, or the relevant guide).
5. No secrets committed; development credentials are clearly marked as development-only.

## 5. Risks and mitigations

| Risk | Mitigation |
|---|---|
| SQL Server has no native arm64 image (Apple Silicon) | Compose pins `platform: linux/amd64`; Docker Desktop runs it through Rosetta. Documented in the README. |
| Location data file format unknown | The importer accepts CSV or XLSX, matches header names loosely, and skips duplicates safely. |
| SMTP not available on reviewer machine | Mailpit is bundled in Compose. A `File` email mode writes `.eml` files with no extra services. |
| Two administrators decide the same request | SQL Server `rowversion` concurrency token; the second decision gets HTTP 409. |
| Email failure loses the notification | Transactional outbox: the email row is saved in the same transaction as the decision, then retried. |
