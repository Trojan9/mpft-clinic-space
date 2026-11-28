# ADR 0004: Use EF Core directly in the application layer

**Status:** Accepted

## Context
A common pattern wraps EF Core in generic repositories and a unit of work.

## Decision
Use cases depend on `IAppDbContext` (the DbSets plus `SaveChangesAsync`). No repository layer.

## Reasons
`DbContext` is already a unit of work and `DbSet` is already a repository. Wrapping them hides useful features (projections, `Include`, split queries, concurrency tokens) behind a lowest-common-denominator interface, without making the code easier to test. The integration tests use a real SQL Server instead.

## Consequences
The Application project references EF Core. That's acceptable: the dependency is on an abstraction-friendly ORM, not on SQL Server.
