# Data model

```mermaid
erDiagram
  Users ||--o{ ClinicalSpaceRequests : "submits (RequesterId)"
  Users ||--o{ ClinicalSpaceRequests : "decides (DecidedById)"
  Users ||--o{ RequestEvents : "acts in (ActorUserId)"
  Regions ||--o{ Sites : contains
  Sites ||--o{ Buildings : contains
  Buildings ||--o{ ClinicalSpaceRequests : "is requested in"
  ClinicalSpaceRequests ||--o{ RequestEvents : "has history"
  ClinicalSpaceRequests ||--o{ EmailOutbox : "triggers"

  Users {
    uniqueidentifier Id PK
    nvarchar64 Username
    nvarchar64 NormalizedUsername UK
    nvarchar128 DisplayName
    nvarchar254 Email
    nvarchar512 PasswordHash "PBKDF2, never plain text"
    nvarchar20 Role "Requester | Administrator (CHECK)"
    bit IsActive
    int AccessFailedCount
    datetimeoffset LockoutEndUtc
    datetimeoffset LastSignInAtUtc
    datetimeoffset CreatedAtUtc
    datetimeoffset UpdatedAtUtc
  }
  Regions {
    int Id PK
    nvarchar128 Name UK
    bit IsActive
  }
  Sites {
    int Id PK
    int RegionId FK
    nvarchar160 Name "UK with RegionId"
    bit IsActive
  }
  Buildings {
    int Id PK
    int SiteId FK
    nvarchar160 Name "UK with SiteId"
    bit IsActive
  }
  ClinicalSpaceRequests {
    uniqueidentifier Id PK
    int ReferenceNumber UK "from SEQUENCE, shown as CSR-000123"
    uniqueidentifier RequesterId FK
    nvarchar128 RequesterName "snapshot"
    nvarchar254 RequesterEmail "snapshot"
    nvarchar20 CostCode
    nvarchar128 BudgetHolderName
    nvarchar254 BudgetHolderEmail
    int BuildingId FK
    datetimeoffset StartsAt
    datetimeoffset EndsAt "CHECK EndsAt > StartsAt"
    bit SinkRequired
    bit ClinicalCouchRequired
    nvarchar1000 AdditionalRequirementsNotes
    datetimeoffset TermsAcceptedAtUtc
    nvarchar32 TermsVersion
    nvarchar20 Status "Submitted | Accepted | Rejected (CHECK)"
    uniqueidentifier DecidedById FK
    datetimeoffset DecidedAtUtc
    nvarchar1000 DecisionNotes
    rowversion RowVersion "optimistic concurrency"
    datetimeoffset CreatedAtUtc
    datetimeoffset UpdatedAtUtc
  }
  RequestEvents {
    bigint Id PK
    uniqueidentifier RequestId FK
    nvarchar20 Type "Submitted | Accepted | Rejected"
    uniqueidentifier ActorUserId FK
    nvarchar128 ActorName "snapshot"
    datetimeoffset OccurredAtUtc
    nvarchar1000 Notes
  }
  EmailOutbox {
    uniqueidentifier Id PK
    nvarchar254 ToAddress
    nvarchar128 ToName
    nvarchar200 Subject
    nvarcharmax TextBody
    nvarcharmax HtmlBody
    uniqueidentifier RelatedRequestId
    datetimeoffset CreatedAtUtc
    datetimeoffset NextAttemptAtUtc
    datetimeoffset SentAtUtc
    int Attempts
    nvarchar2000 LastError
    bit IsDeadLettered
  }
```

## Design notes

| Choice | Reason |
|---|---|
| **Location hierarchy as three tables** (Region → Site → Building) with `IsActive` flags | Matches the dependent drop-downs and the supplied data. Deactivating a building hides it from new requests while keeping old requests valid. Rows are never deleted. |
| **Unique names within the parent** (`Sites(RegionId, Name)`, `Buildings(SiteId, Name)`) | Stops duplicate imports, and lets the same building name exist on different sites. |
| **GUID primary key + sequential reference number** | GUIDs don't reveal how many requests exist and can't be guessed in URLs. The `RequestReferenceNumbers` sequence gives people a short, readable reference (`CSR-000123`) for phone calls and emails. |
| **Requester name/email snapshot** on the request | The record and its emails stay accurate after a name or email change. The audit trail shows who it was *at the time*. |
| **Status stored as text with a CHECK constraint** | Readable in SQL and reports. The database rejects invalid values even if the application has a bug. |
| **`CK_Requests_Decision`** | Enforces the rule that a submitted request has no decider and a decided request always has one. |
| **`RowVersion`** | SQL Server updates it automatically on every change. EF Core adds it to the `WHERE` clause, so a stale update affects 0 rows and becomes a 409. |
| **`RequestEvents` append-only history** | A simple, queryable audit trail ("who accepted this, when, and why?"). Shown to users as a timeline. |
| **`EmailOutbox`** | Transactional outbox: the email is committed atomically with the change that caused it. A filtered index (`WHERE SentAtUtc IS NULL AND IsDeadLettered = 0`) keeps the worker's polling query cheap. |
| **`datetimeoffset`** everywhere | Unambiguous instants. Display in Europe/London is a presentation concern. |
| **`ON DELETE RESTRICT`** on all foreign keys except request → events | Nothing can be removed in a way that orphans history. |

## Indexes

| Index | Serves |
|---|---|
| `Users(NormalizedUsername)` unique | Sign-in look-up |
| `ClinicalSpaceRequests(RequesterId, CreatedAtUtc)` | My Requests |
| `ClinicalSpaceRequests(Status, CreatedAtUtc)` | Admin queue and filters |
| `ClinicalSpaceRequests(BuildingId, StartsAt, EndsAt)` | Overlap (clash) check |
| `ClinicalSpaceRequests(ReferenceNumber)` unique | Search by reference |
| `RequestEvents(RequestId, OccurredAtUtc)` | Timeline |
| `EmailOutbox(NextAttemptAtUtc)` filtered | Outbox polling |

## Migrations

EF Core migrations are in `backend/src/ClinicSpace.Infrastructure/Persistence/Migrations`. See the README, section 5, for applying them, scripting them for DBA review, and adding new ones.
