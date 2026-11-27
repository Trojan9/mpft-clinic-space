# Requirements traceability

Every requirement in the exercise brief, where it's implemented, and how it's proven.

| # | Requirement (from the brief) | Implementation | Evidence (tests / screenshots) |
|---|---|---|---|
| **Roles** ||||
| R1 | Requester can sign in | `AuthController.Login`, `SignInPage.tsx` | `AuthTests.Sign_in_returns_the_profile…`, `SignInPage.test.tsx`, e2e, screenshot 01 |
| R2 | Requester can create a request | `POST /api/requests`, `RequestFormPage` → `CheckAnswersPage` | `RequestWorkflowTests.Requester_submits…`, `RequestFormPage.test.tsx`, e2e, screenshots 04–07 |
| R3 | Requester can view own requests and status | `GET /api/requests/mine`, `MyRequestsPage` | `MyRequestsPage.test.tsx`, e2e, screenshots 08, 16 |
| R4 | Requester can view request details | `GET /api/requests/{id}` (ownership check), `RequestDetailPage` | `Requester_cannot_see_someone_elses_request`, screenshots 09, 17, 20 |
| R5 | Administrator can sign in | as R1 | e2e admin journey |
| R6 | Administrator can view all requests | `GET /api/requests` (Administrator policy), `AdminRequestsPage` | `Admin_list_filters…`, screenshot 10 |
| R7 | Administrator can view details | `GET /api/requests/{id}`, `AdminRequestDetailPage` | `AdminRequestDetailPage.test.tsx`, screenshot 12 |
| R8 | Administrator can accept or reject | `POST /api/requests/{id}/decision`, domain `Accept`/`Reject` | `Administrator_accepts…`, `Rejecting_needs_a_reason…`, e2e, screenshots 13–15 |
| **Authentication** ||||
| R9 | Simple username and password | Cookie authentication, `AuthService` | `AuthTests` |
| R10 | Passwords stored securely | PBKDF2 via `PasswordHasher<User>` | `AuthTests` (no hash in responses); `docs/SECURITY.md` |
| R11 | Role or administrator indicator on the user | `Users.Role` (CHECK constraint) | `DATA-MODEL.md` |
| **Data** ||||
| R12 | Users and roles | `User`, `UserRole` | |
| R13 | Regions, sites and buildings from the supplied data | `Region`/`Site`/`Building`; `LocationImporter` reads the workbook's Buildings sheet (11 regions, 22 sites, 44 buildings) | `LocationFileReaderTests`, `UserFileReaderTests.Reads_both_sheets…`, `Location_lookups_follow…` |
| R13a | Supplied user accounts | `UserImporter` reads the Users sheet (14 accounts), maps Admin → Administrator, hashes passwords | `UserFileReaderTests`, e2e sign-ins with the supplied accounts |
| R14 | Clinical space requests | `ClinicalSpaceRequest` | `ClinicalSpaceRequestTests` |
| R15 | Statuses Submitted, Accepted, Rejected | `RequestStatus` + CHECK constraint | `ClinicalSpaceRequestTests` |
| R16 | Sink, couch, optional free text | `SinkRequired`, `ClinicalCouchRequired`, `AdditionalRequirementsNotes` | validator tests |
| **Create request journey** ||||
| R17 | Name and email shown from the account | Read-only "Your details"; server ignores any name/email in the body | `Requester_name_and_email_always_come_from_the_account…`, `RequestFormPage.test.tsx` |
| R18 | Cost code, budget holder name and email | Form + `CreateRequestValidator` | `CreateRequestValidatorTests` |
| R19 | Dependent Region → Site → Building | `LocationsController`, dependent selects | `loads sites and buildings…`, `Location_lookups…` |
| R20 | Start and end date and time | NHS date input + time; UK time | `dates.test.ts`, `requestSchema.test.ts`, `UkDateFormatterTests` |
| R21 | Sink yes/no, couch yes/no, free text | Radios + textarea with character count | `RequestFormPage.test.tsx` |
| R22 | Must accept terms and conditions | Checkbox, server rule, version and time stored | `Terms_must_be_accepted`, screenshot 04 |
| R23 | Stored as Submitted | `ClinicalSpaceRequest.Submit` | `Submit_creates_a_submitted_request…` |
| R24 | Appears in My Requests | Query invalidation, `ListMineAsync` | `Requester_submits…`, e2e |
| **Admin processing** ||||
| R25 | View all, open one, accept or reject | as R6–R8 | |
| R26 | Filtering or sorting (optional) | Status tabs, search, region, six sort orders, paging | `Admin_list_filters…`, screenshot 11 |
| R27 | Status updated on decision | Domain state change in one transaction | integration tests |
| R28 | Requester gets an email | Transactional outbox → SMTP (Mailpit) / file / log | `Administrator_accepts…` (outbox + dispatch), e2e Mailpit check, screenshots 18–19 |
| R29 | Status updated in My Requests | Same data, refetched | e2e, screenshot 16 |
| **Email** ||||
| R30 | Demonstrable in development; README explains | Mailpit in Compose; `Email__Mode` | README section 6 |
| **UI** ||||
| R31 | Interfaces for sign in, create, view own, view all, review | All built | screenshots |
| **Quality** ||||
| R32 | Form validation | Zod (client) + FluentValidation (server, authoritative) | validator and schema tests |
| R33 | Role-based access control | Policies, ownership checks, route guards | `Role_policies_are_enforced…`, `RequireRole.test.tsx` |
| R34 | Sensible error handling | Problem details, error summary, 404/409/500 pages, outbox retries | `Unhandled_errors_never_leak…`, `explains a conflict…` |
| R35 | Documentation and local setup | README + `docs/` | |
| R36 | Maintainability and structure | Layered backend, feature-based frontend, ADRs | `ARCHITECTURE.md` |
| R37 | Automated tests (encouraged) | Four levels, in CI | `TESTING.md` |
| **Optional enhancements** ||||
| O1 | Search/filter/sort on the admin list | Done | R26 |
| O2 | Created and updated timestamps | `IAuditable` stamped by the DbContext, plus the `RequestEvents` history | `DATA-MODEL.md` |
| O3 | Accessibility | WCAG 2.2 AA approach, axe in CI | `ACCESSIBILITY.md` |
| O4 | Separation of concerns | Clean Architecture | `ARCHITECTURE.md` |
| **Submission** ||||
| S1 | Public repository | https://github.com/Trojan9/mpft-clinic-space | |
| S2 | README: run, database, tests, email, assumptions, AI use | README sections 1–13 | |
| S3 | Screenshots of each journey | `docs/screenshots/` (23 images) | |

Assumptions are listed in the README, section 11.
