# ADR 0005: Transactional outbox for notification emails

**Status:** Accepted

## Context
When a request is accepted or rejected, the requester must be emailed. Sending SMTP inside the HTTP request means either a slow or failed SMTP server fails the decision, or the decision is saved and the email is lost.

## Decision
Write the email to an `EmailOutbox` table **in the same transaction** as the decision. A hosted background service (`EmailOutboxDispatcher`) polls every 5 seconds, sends through a pluggable transport (SMTP / `.eml` file / log) and records the outcome. Failures retry with exponential back-off (30 s doubling, capped at 30 minutes). After 8 attempts the email is dead-lettered and logged at Error.

## Consequences
- No lost or phantom notifications. Decisions stay fast whatever the SMTP latency.
- Emails arrive a few seconds after the decision.
- Assumes a single API instance. Scaling out needs row claiming (`UPDLOCK, READPAST`) or a message queue (Azure Service Bus).
- The outbox doubles as an audit of what was sent to whom (`docs/RUNBOOK.md` shows how to check it).
