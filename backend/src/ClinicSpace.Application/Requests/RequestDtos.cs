using ClinicSpace.Application.Locations;
using ClinicSpace.Domain.Requests;

namespace ClinicSpace.Application.Requests;

/// <summary>Body of POST /api/requests. Requester name and email come from the account, not the form.</summary>
public sealed record CreateRequestCommand
{
    public string? CostCode { get; init; }

    public string? BudgetHolderName { get; init; }

    public string? BudgetHolderEmail { get; init; }

    public int? BuildingId { get; init; }

    public DateTimeOffset? StartsAt { get; init; }

    public DateTimeOffset? EndsAt { get; init; }

    /// <summary>Nullable so that "not answered" is different from "No".</summary>
    public bool? SinkRequired { get; init; }

    public bool? ClinicalCouchRequired { get; init; }

    public string? AdditionalRequirements { get; init; }

    public bool AcceptTerms { get; init; }
}

public enum Decision
{
    Accept = 1,
    Reject = 2,
}

/// <summary>Body of POST /api/requests/{id}/decision.</summary>
public sealed record DecideRequestCommand
{
    public Decision? Decision { get; init; }

    /// <summary>Optional when accepting; required when rejecting (shown to the requester).</summary>
    public string? Notes { get; init; }

    /// <summary>Base64 concurrency token from the detail response.</summary>
    public string? Version { get; init; }
}

public enum RequestSortField
{
    SubmittedAt = 1,
    StartsAt = 2,
    Reference = 3,
    Requester = 4,
    Status = 5,
}

/// <summary>Query string for request lists. Unknown values fall back to safe defaults.</summary>
public sealed record RequestListQuery
{
    public RequestStatus? Status { get; init; }

    public int? RegionId { get; init; }

    /// <summary>Free-text search across reference, requester, cost code, budget holder and location.</summary>
    public string? Search { get; init; }

    public RequestSortField Sort { get; init; } = RequestSortField.SubmittedAt;

    public bool Descending { get; init; } = true;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}

public sealed record RequestSummaryDto(
    Guid Id,
    string Reference,
    RequestStatus Status,
    string RequesterName,
    string Region,
    string Site,
    string Building,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? DecidedAt);

public sealed record RequestEventDto(RequestEventType Type, string ActorName, DateTimeOffset OccurredAt, string? Notes);

public sealed record OverlappingRequestDto(Guid Id, string Reference, RequestStatus Status, string RequesterName, DateTimeOffset StartsAt, DateTimeOffset EndsAt);

public sealed record RequestDetailDto(
    Guid Id,
    string Reference,
    RequestStatus Status,
    string RequesterName,
    string RequesterEmail,
    string CostCode,
    string BudgetHolderName,
    string BudgetHolderEmail,
    LocationPath Location,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    bool SinkRequired,
    bool ClinicalCouchRequired,
    string? AdditionalRequirements,
    string TermsVersion,
    DateTimeOffset TermsAcceptedAt,
    DateTimeOffset SubmittedAt,
    DateTimeOffset UpdatedAt,
    string? DecidedByName,
    DateTimeOffset? DecidedAt,
    string? DecisionNotes,
    string Version,
    IReadOnlyList<RequestEventDto> Timeline,
    IReadOnlyList<OverlappingRequestDto>? OverlappingRequests);

/// <summary>Counts per status, for the summary cards above the administrator list.</summary>
public sealed record RequestStatusCounts(int Submitted, int Accepted, int Rejected)
{
    public int Total => Submitted + Accepted + Rejected;
}
