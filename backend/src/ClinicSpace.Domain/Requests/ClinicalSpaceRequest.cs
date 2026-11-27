using ClinicSpace.Domain.Common;
using ClinicSpace.Domain.Locations;
using ClinicSpace.Domain.Users;

namespace ClinicSpace.Domain.Requests;

/// <summary>
/// A request from a member of staff to use a clinical space in a building for a period of time.
/// State changes go through <see cref="Accept"/> and <see cref="Reject"/>, which enforce the
/// lifecycle rules and write the audit trail.
/// </summary>
public class ClinicalSpaceRequest : IAuditable
{
    public const int CostCodeMaxLength = 20;
    public const int BudgetHolderNameMaxLength = 128;
    public const int DecisionNotesMaxLength = 1000;
    public const string ReferencePrefix = "CSR-";

    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Sequential number from a database sequence, shown to people as CSR-000123.</summary>
    public int ReferenceNumber { get; set; }

    public string Reference => FormatReference(ReferenceNumber);

    public Guid RequesterId { get; set; }

    public User? Requester { get; set; }

    /// <summary>Requester's name when the request was made (snapshot for audit and email).</summary>
    public string RequesterName { get; set; } = string.Empty;

    /// <summary>Requester's email when the request was made (snapshot for audit and email).</summary>
    public string RequesterEmail { get; set; } = string.Empty;

    public string CostCode { get; set; } = string.Empty;

    public string BudgetHolderName { get; set; } = string.Empty;

    public string BudgetHolderEmail { get; set; } = string.Empty;

    public int BuildingId { get; set; }

    public Building? Building { get; set; }

    public DateTimeOffset StartsAt { get; set; }

    public DateTimeOffset EndsAt { get; set; }

    public bool SinkRequired { get; set; }

    public bool ClinicalCouchRequired { get; set; }

    public string? AdditionalRequirementsNotes { get; set; }

    public DateTimeOffset TermsAcceptedAtUtc { get; set; }

    /// <summary>Which version of the terms was accepted, so a change of terms can be traced.</summary>
    public string TermsVersion { get; set; } = string.Empty;

    public RequestStatus Status { get; private set; } = RequestStatus.Submitted;

    public Guid? DecidedById { get; private set; }

    public User? DecidedBy { get; private set; }

    public DateTimeOffset? DecidedAtUtc { get; private set; }

    public string? DecisionNotes { get; private set; }

    /// <summary>SQL Server rowversion used for optimistic concurrency between administrators.</summary>
    public byte[] RowVersion { get; set; } = [];

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public ICollection<RequestEvent> Events { get; } = new List<RequestEvent>();

    public static string FormatReference(int number) => $"{ReferencePrefix}{number:D6}";

    /// <summary>Parses "CSR-000123", "csr-123" or "123" into a reference number.</summary>
    public static bool TryParseReference(string? value, out int number)
    {
        number = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        if (trimmed.StartsWith(ReferencePrefix, StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[ReferencePrefix.Length..];
        }

        return int.TryParse(trimmed, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out number)
            && number > 0;
    }

    /// <summary>Creates a new request in the Submitted state and records the first audit event.</summary>
    public static ClinicalSpaceRequest Submit(
        User requester,
        int buildingId,
        string costCode,
        string budgetHolderName,
        string budgetHolderEmail,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        AdditionalRequirements requirements,
        string termsVersion,
        DateTimeOffset now)
    {
        if (endsAt <= startsAt)
        {
            throw new DomainException("invalid_period", "The end date and time must be after the start date and time.");
        }

        var request = new ClinicalSpaceRequest
        {
            RequesterId = requester.Id,
            RequesterName = requester.DisplayName,
            RequesterEmail = requester.Email,
            BuildingId = buildingId,
            CostCode = costCode,
            BudgetHolderName = budgetHolderName,
            BudgetHolderEmail = budgetHolderEmail,
            StartsAt = startsAt,
            EndsAt = endsAt,
            SinkRequired = requirements.SinkRequired,
            ClinicalCouchRequired = requirements.ClinicalCouchRequired,
            AdditionalRequirementsNotes = requirements.Notes,
            TermsAcceptedAtUtc = now,
            TermsVersion = termsVersion,
            Status = RequestStatus.Submitted,
        };

        request.Events.Add(new RequestEvent
        {
            RequestId = request.Id,
            Type = RequestEventType.Submitted,
            ActorUserId = requester.Id,
            ActorName = requester.DisplayName,
            OccurredAtUtc = now,
        });

        return request;
    }

    public void Accept(User administrator, string? notes, DateTimeOffset now) =>
        Decide(administrator, RequestStatus.Accepted, notes, now);

    public void Reject(User administrator, string reason, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("reason_required", "A reason is required to reject a request.");
        }

        Decide(administrator, RequestStatus.Rejected, reason, now);
    }

    private void Decide(User administrator, RequestStatus outcome, string? notes, DateTimeOffset now)
    {
        if (!administrator.IsAdministrator)
        {
            throw new DomainException("not_administrator", "Only an administrator can decide a request.");
        }

        if (Status != RequestStatus.Submitted)
        {
            throw new DomainException(
                "already_decided",
                $"This request has already been {Status.ToString().ToLowerInvariant()}.");
        }

        if (administrator.Id == RequesterId)
        {
            throw new DomainException("own_request", "You cannot decide a request you made yourself.");
        }

        Status = outcome;
        DecidedById = administrator.Id;
        DecidedAtUtc = now;
        DecisionNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

        Events.Add(new RequestEvent
        {
            RequestId = Id,
            Type = outcome == RequestStatus.Accepted ? RequestEventType.Accepted : RequestEventType.Rejected,
            ActorUserId = administrator.Id,
            ActorName = administrator.DisplayName,
            OccurredAtUtc = now,
            Notes = DecisionNotes,
        });
    }
}
