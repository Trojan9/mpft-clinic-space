# Runbook: supporting MPFT Clinic Space

For whoever supports the service day to day. Commands assume the Docker Compose deployment. On Azure, the equivalents are App Service log streams, Application Insights and Azure SQL.

## Service overview

| Component | Health check | Logs |
|---|---|---|
| Web (nginx) | `GET /healthz` → `ok` | `docker compose logs web` |
| API | `GET /health/live` (process) · `GET /health/ready` (database) | `docker compose logs api` (JSON outside Development) |
| Database | `sqlcmd -Q "SELECT 1"` | `docker compose logs db` |
| Email | Mailpit UI at :8025 (dev) / SMTP relay status | API log entries from `EmailOutboxDispatcher` |

Every API response has an `X-Correlation-ID` header. Error responses include the same value as `traceId`. **Ask users for the trace ID** shown with an error, then search the logs for it.

## Common faults

### "Sorry, there is a problem with the service"
1. Get the time and, if possible, the trace ID from the user.
2. `docker compose logs api | grep <traceId>`. Unhandled exceptions are logged at Error with the full stack (the stack trace never reaches the user).
3. Check `/health/ready`. If it's unhealthy, see *Database unavailable*.

### Database unavailable
- `/health/ready` returns `Unhealthy`, and the API logs show `SqlException`.
- Check `docker compose ps db` and the free disk space. SQL Server needs at least 2 GB of RAM.
- The API retries transient SQL errors (EF Core `EnableRetryOnFailure`) and retries migrations at start-up for up to about a minute.

### A user can't sign in
- Wrong password 5 times → **locked for 15 minutes** (by design). Check:
  ```sql
  SELECT Username, IsActive, AccessFailedCount, LockoutEndUtc FROM Users WHERE NormalizedUsername = UPPER('sarah.khan');
  ```
- To unlock early (with the user's line manager's approval): `UPDATE Users SET LockoutEndUtc = NULL, AccessFailedCount = 0 WHERE ...`
- `IsActive = 0` means the account was disabled on purpose.
- HTTP 429 means the sign-in rate limit for that IP address (10 a minute) was hit. Wait a minute.

### Requester says they did not get an email
1. Find the request's emails:
   ```sql
   SELECT Subject, CreatedAtUtc, SentAtUtc, Attempts, NextAttemptAtUtc, IsDeadLettered, LastError
   FROM EmailOutbox WHERE RelatedRequestId = (SELECT Id FROM ClinicalSpaceRequests WHERE ReferenceNumber = 123);
   ```
2. `SentAtUtc` set → it left our system. Check the relay and the user's junk folder.
3. `Attempts > 0`, not sent → the relay is failing. `LastError` says why. It retries with back-off (30 s, 1 min, 2 min… up to 30 min).
4. `IsDeadLettered = 1` → it gave up after 8 attempts. Fix the cause, then re-queue:
   ```sql
   UPDATE EmailOutbox SET IsDeadLettered = 0, Attempts = 0, NextAttemptAtUtc = SYSUTCDATETIME() WHERE Id = '<id>';
   ```

### Two administrators decided the same request
That can't happen. The second one gets "This request was changed by someone else" (HTTP 409) and must reload. The audit trail (`RequestEvents`) shows who decided, and when.

### Location missing from the drop-downs
Locations come from the location file. Add the row to the file and run the importer. It only adds, never deletes:
```bash
docker compose run --rm api import-locations /app/data/locations.csv
```
To hide a building from new requests without breaking old ones: `UPDATE Buildings SET IsActive = 0 WHERE Id = ...`.

## Releases

1. A PR to `main` needs CI green (build, unit, integration, frontend, e2e) and review.
2. Database changes: generate an idempotent script (`dotnet ef migrations script --idempotent`) and review it. Apply it before deploying the new API version. Migrations are additive, so the old API version keeps working during the release.
3. Deploy the API, then the web app. Check `/health/ready` and do a smoke test: sign in, open My Requests, open one request as an administrator.
4. Rollback: redeploy the previous container images. The additive migrations don't need rolling back.

## Backups (production)

Azure SQL automated backups (point-in-time restore, 7–35 days), with a long-term retention policy agreed with Information Governance. Test a restore every quarter.
