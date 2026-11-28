# Security

This service would hold staff names, email addresses, cost codes and booking details. That's personal and operational data, but not patient data. The controls below are designed for that level, with notes on what changes in production.

## OWASP Top 10 (2021) mapping

| Risk | Controls in this application | Where |
|---|---|---|
| **A01 Broken access control** | Fallback policy: every endpoint requires an authenticated user unless explicitly `[AllowAnonymous]`. `Requester`/`Administrator` policies on each action. Ownership is checked in the service layer, and other users' requests return **404**, not 403. Administrators can't decide their own requests (domain rule). The UI route guards are convenience only. | `AuthenticationSetup.cs`, `RequestsController.cs`, `RequestService.GetAsync`, `ClinicalSpaceRequest.Decide` |
| **A02 Cryptographic failures** | PBKDF2-HMAC-SHA512, 100k iterations, 128-bit salt (Identity `PasswordHasher` v3), with automatic rehash. Session and anti-forgery cookies are encrypted by ASP.NET Core Data Protection. `Secure` cookies and HSTS outside local HTTP. SQL connection uses `Encrypt=True`. | `AuthService.cs`, `Program.cs` |
| **A03 Injection** | EF Core parameterises all SQL. No raw SQL built from strings. Input is validated with allow-list patterns (cost code, names). Regexes have timeouts. Email HTML encodes every user value. React escapes output by default; no `dangerouslySetInnerHTML`. | `CreateRequestValidator.cs`, `RequestEmailFactory.cs` |
| **A04 Insecure design** | Business rules live in the domain (state machine, reason required, no self-approval). Optimistic concurrency prevents lost updates. The transactional outbox prevents a decision being saved without its notification. Requester identity comes from the session, never from the request body. | `ClinicalSpaceRequest.cs`, `RequestService.cs` |
| **A05 Security misconfiguration** | Strict CSP (`default-src 'self'`, no inline script), `X-Frame-Options: DENY`, `nosniff`, `Referrer-Policy: no-referrer`, `Permissions-Policy`, COOP. No `Server` header. API docs only in Development. Non-root containers. Ports bound to `127.0.0.1`. 64 KB request body limit. | `SecurityHeadersMiddleware.cs`, `frontend/nginx/*`, `Dockerfile`s |
| **A06 Vulnerable components** | Pinned versions (Central Package Management, `package-lock.json`). CI fails on High/Critical NuGet advisories and `npm audit` High. Dependabot weekly. CodeQL for C# and TypeScript. | `.github/` |
| **A07 Identification and authentication failures** | Lockout after 5 failures for 15 minutes. Per-IP rate limit on sign-in. Generic failure message. Constant-work path for unknown users (dummy hash). Password length capped at 128 to prevent hashing DoS. Session: HttpOnly, `SameSite=Strict`, 30 min sliding idle, 8 h absolute, revalidated against the database on every request (disabled user or role change applies at once). Logout clears the session. | `AuthService.cs`, `AuthController.cs`, `AuthenticationSetup.cs`, `RateLimitPolicies.cs` |
| **A08 Software and data integrity failures** | Anti-forgery (double-submit token bound to the user) on every POST. Concurrency tokens on decisions. Reproducible builds from lock files; CI builds the containers. | `AntiforgeryMiddleware.cs` |
| **A09 Logging and monitoring failures** | Structured JSON logs. Sign-in success and failure, lockouts, submissions, decisions, concurrency conflicts and email failures are logged with user IDs (no passwords or personal free text). Correlation ID on every response. Dead-lettered emails logged at Error. Health endpoints for monitoring. | throughout; `docs/RUNBOOK.md` |
| **A10 SSRF** | The server makes no outbound requests based on user input. SMTP host is configuration only. | n/a |

## Data protection (UK GDPR) notes

- **Minimisation**: only the fields the brief asks for are stored. Free text is capped at 1,000 characters, and the hint discourages entering patient information.
- **No browser storage of personal data**: the form draft is held in memory only, not in `localStorage`, so nothing is left on a shared clinical workstation.
- **No caching**: API responses carry `Cache-Control: no-store`.
- **Retention**: not defined by the brief. In production, agree a retention period with Information Governance and add a scheduled purge or anonymisation of old requests and sent emails.
- **Emails** contain booking details only, nothing clinical. Use NHSmail (encrypted in transit) in production.

## Supplied test credentials

The exercise workbook (`data/exercise-data.xlsx`) contains plain-text passwords for its test accounts. It's committed unchanged so reviewers can run the app as supplied. The importer hashes each password with PBKDF2 before saving and never logs it. A real deployment would have no password file: identities and roles would come from Microsoft Entra ID.

## Known limitations and production checklist

| Item | Status here | Production action |
|---|---|---|
| Identity provider | Local username/password (as the brief asks) | Microsoft Entra ID (OIDC) with MFA and conditional access; map app roles to the existing policies; remove local passwords. |
| HTTPS | Local HTTP on localhost; `Secure` cookies switched off in Compose only | TLS at the front door / App Service; `Security__RequireSecureCookies=true` (the default); HSTS is already on outside Development. |
| Secrets | Development defaults in Compose, clearly labelled | Azure Key Vault + managed identity. No SQL `sa` account; a least-privilege SQL user (db_datareader/db_datawriter plus a separate migration identity). |
| Data protection keys | File volume | Azure Blob Storage + Key Vault key encryption, shared across instances. |
| Rate limiting | In-memory, per instance | Gateway or WAF level (Front Door / API Management) for multi-instance deployments. |
| Penetration test and DPIA | Not done | Before go-live, with Information Governance and the security team. |
| Audit log retention | In the database | Ship logs to the SIEM; protect audit tables from modification. |
