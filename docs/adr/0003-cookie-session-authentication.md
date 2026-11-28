# ADR 0003: Cookie sessions, not JWTs in the browser

**Status:** Accepted

## Context
The brief asks for simple username/password sign-in. The client is a browser SPA served from the same origin as the API.

## Options
1. JWT access token stored in `localStorage`, sent as a bearer token.
2. JWT in memory with a refresh-token cookie.
3. **ASP.NET Core cookie authentication** (HttpOnly, `SameSite=Strict`) plus an anti-forgery token.

## Decision
Option 3.

## Reasons
- Script can't read the cookie, so an XSS bug can't steal the session (unlike option 1).
- Sign-out and account disabling take effect at once (the principal is revalidated on each request). Self-contained JWTs stay valid until they expire.
- Same origin, through the nginx proxy, means no CORS and lets `SameSite=Strict` work.
- It's the pattern Microsoft recommends for SPAs hosted with their API (the "BFF" approach). It also carries over to Entra ID: OpenID Connect sign-in issues the same kind of cookie.

## Consequences
- CSRF protection is needed. Added: a double-submit anti-forgery token, on top of SameSite.
- Not suitable for third-party API clients. Those would use Entra-issued bearer tokens on a separate scheme.
