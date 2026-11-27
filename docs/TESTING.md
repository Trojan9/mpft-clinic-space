# Testing

## Strategy

I followed the testing pyramid: many fast tests on rules and logic, fewer slow tests on the whole system. Each level proves something the level below can't.

| Level | Count* | Tools | Proves |
|---|---|---|---|
| Backend unit | 88 | xUnit, FluentAssertions, FluentValidation.TestHelper | Business rules in isolation: request lifecycle, self-approval ban, lockout, outbox back-off and dead-lettering, every validation rule (including boundaries and allowed characters), UK time formatting across GMT/BST, email content and **HTML encoding of hostile input**, CSV/XLSX parsing edge cases. |
| Backend integration | 32 | WebApplicationFactory, **Testcontainers SQL Server 2022** | The real HTTP pipeline + real database: migrations, sequence references, check constraints, `rowversion` 409s, cookie flags, CSRF rejection, role policies (403), ownership (404), lockout, disabled-user session revocation, validation problem details, search/filter/paging, overlaps, outbox sending, health, security headers. |
| Frontend | 62 | Vitest, Testing Library, user-event, vitest-axe | UK date conversion including clock-change days, form rules, API client (CSRF header, problem details, 401 handling, network errors), route guards, sign-in (including the open-redirect guard), the full request journey through check answers to confirmation, admin decision and conflict handling, **automated accessibility checks**. |
| End to end | 12 | Playwright, @axe-core/playwright | Real browser against the Docker stack: both journeys, the decision email arriving in Mailpit, cross-user access blocked, mobile layout with no horizontal scroll, keyboard skip link, WCAG 2.2 AA scans including colour contrast. |

\* Counts at the time of writing; CI output is the source of truth.

## Notable test design choices

- **A real SQL Server, not EF InMemory or SQLite.** Several rules live in the database (check constraints, sequence, `rowversion`, unique indexes). An in-memory provider would pass tests that production would fail.
- **The clock is injected** (`TimeProvider`), so date rules are tested at exact boundaries ("must be in the future", minimum and maximum duration) without flaky `DateTime.Now`.
- **Frontend tests drive the real router and real components** with a fake `fetch` that fails on any unexpected call, so a test can't silently depend on an endpoint it didn't set up.
- **Tests are written from the user's point of view**, using Testing Library queries by role and label, which doubles as an accessibility check.
- **Security behaviour is tested as behaviour**, for example "a request body with a forged requesterEmail is ignored", "POST without the anti-forgery header returns 400" and "a disabled account's existing session stops working".

## Running

See README, section 7. In CI (`.github/workflows/ci.yml`):

1. `backend`: build with warnings as errors, unit tests, integration tests, vulnerable package check.
2. `frontend`: ESLint (with `jsx-a11y`), type-check, tests with coverage, production build, `npm audit`.
3. `e2e`: `docker compose up --build --wait`, Playwright tests, screenshot capture, and the report uploaded as an artifact.

## What I'd add with more time

- Contract tests that check the TypeScript types against the generated OpenAPI document.
- Load test (k6) on the list and decision endpoints.
- Mutation testing (Stryker.NET) on the domain and validators.
- Manual assistive-technology testing (NVDA, VoiceOver) and UAT scripts for the estates team.
