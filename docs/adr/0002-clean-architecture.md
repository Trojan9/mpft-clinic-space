# ADR 0002: Light Clean Architecture for the backend

**Status:** Accepted

## Context
The core of the app is a small workflow, but it has real rules (lifecycle, reason required, no self-approval, concurrency) and parts that will change (authentication → Entra ID, SMTP → NHSmail/Graph, database hosting).

## Decision
Four projects: `Domain` (entities and rules, no dependencies), `Application` (use cases, validation, interfaces), `Infrastructure` (EF Core, email, import), and `Api` (HTTP, authentication, composition). Dependencies point inwards.

## Consequences
- Rules are unit-tested without a database or HTTP.
- Infrastructure changes don't touch business logic.
- More projects than a single-project app. I kept it light: no MediatR, no CQRS split, no generic repositories (see ADR 0004).
