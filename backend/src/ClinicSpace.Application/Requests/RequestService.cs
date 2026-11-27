using ClinicSpace.Application.Abstractions;
using ClinicSpace.Application.Common;
using ClinicSpace.Application.Locations;
using ClinicSpace.Application.Notifications;
using ClinicSpace.Domain.Requests;
using ClinicSpace.Domain.Users;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClinicSpace.Application.Requests;

/// <summary>
/// Use cases for clinical space requests. Role checks live in the API's authorisation policies;
/// this service also checks ownership (a requester can only see their own requests) so the rule
/// holds whichever endpoint calls it.
/// </summary>
public sealed class RequestService(
    IAppDbContext db,
    ICurrentUser currentUser,
    IValidator<CreateRequestCommand> createValidator,
    IValidator<DecideRequestCommand> decideValidator,
    RequestEmailFactory emails,
    TimeProvider clock,
    IOptions<RequestOptions> requestOptions,
    IOptions<NotificationOptions> notificationOptions,
    ILogger<RequestService> logger)
{
    public const int MaxPageSize = 100;

    public async Task<RequestDetailDto> CreateAsync(CreateRequestCommand command, CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(command, cancellationToken);

        var requester = await db.Users.SingleOrDefaultAsync(u => u.Id == currentUser.UserId && u.IsActive, cancellationToken)
            ?? throw new NotFoundException("User");

        if (requester.Role != UserRole.Requester)
        {
            throw new ValidationException([new ValidationFailure(string.Empty, "Only requesters can create clinical space requests.")]);
        }

        var building = await db.Buildings.AsNoTracking()
            .Where(b => b.Id == command.BuildingId && b.IsActive && b.Site!.IsActive && b.Site.Region!.IsActive)
            .Select(b => new { b.Id, Path = new LocationPath(b.Site!.RegionId, b.Site.Region!.Name, b.SiteId, b.Site.Name, b.Id, b.Name) })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new ValidationException([new ValidationFailure(nameof(CreateRequestCommand.BuildingId), "Select a building")]);

        var now = clock.GetUtcNow();
        var request = ClinicalSpaceRequest.Submit(
            requester,
            building.Id,
            costCode: command.CostCode!.Trim().ToUpperInvariant(),
            budgetHolderName: command.BudgetHolderName!.Trim(),
            budgetHolderEmail: command.BudgetHolderEmail!.Trim().ToLowerInvariant(),
            startsAt: command.StartsAt!.Value,
            endsAt: command.EndsAt!.Value,
            new AdditionalRequirements(
                command.SinkRequired!.Value,
                command.ClinicalCouchRequired!.Value,
                string.IsNullOrWhiteSpace(command.AdditionalRequirements) ? null : command.AdditionalRequirements.Trim()),
            requestOptions.Value.TermsVersion,
            now);

        db.Requests.Add(request);
        await db.SaveChangesAsync(cancellationToken);

        if (notificationOptions.Value.SendSubmissionConfirmation)
        {
            // Saved separately from the request so the database-generated reference number is in the email.
            db.EmailMessages.Add(emails.Submitted(request, Describe(building.Path)));
            await db.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("Request {RequestId} ({Reference}) submitted by {UserId}", request.Id, request.Reference, requester.Id);
        return await GetAsync(request.Id, cancellationToken);
    }

    public Task<PagedResult<RequestSummaryDto>> ListMineAsync(RequestListQuery query, CancellationToken cancellationToken) =>
        ListAsync(db.Requests.AsNoTracking().Where(r => r.RequesterId == currentUser.UserId), query, cancellationToken);

    public Task<PagedResult<RequestSummaryDto>> ListAllAsync(RequestListQuery query, CancellationToken cancellationToken)
    {
        EnsureAdministrator();
        return ListAsync(db.Requests.AsNoTracking(), query, cancellationToken);
    }

    public async Task<RequestStatusCounts> GetStatusCountsAsync(CancellationToken cancellationToken)
    {
        EnsureAdministrator();
        var counts = await db.Requests.AsNoTracking()
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        int Count(RequestStatus s) => counts.SingleOrDefault(c => c.Status == s)?.Count ?? 0;
        return new RequestStatusCounts(Count(RequestStatus.Submitted), Count(RequestStatus.Accepted), Count(RequestStatus.Rejected));
    }

    public async Task<RequestDetailDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var request = await db.Requests.AsNoTracking()
            .Include(r => r.Building!).ThenInclude(b => b.Site!).ThenInclude(s => s.Region)
            .Include(r => r.DecidedBy)
            .Include(r => r.Events)
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken);

        // A requester asking for someone else's request gets 404, not 403, so ids cannot be probed.
        if (request is null || (!currentUser.IsAdministrator && request.RequesterId != currentUser.UserId))
        {
            throw new NotFoundException("Request");
        }

        IReadOnlyList<OverlappingRequestDto>? overlaps = null;
        if (currentUser.IsAdministrator)
        {
            var rows = await db.Requests.AsNoTracking()
                .Where(r => r.Id != request.Id
                    && r.BuildingId == request.BuildingId
                    && r.Status != RequestStatus.Rejected
                    && r.StartsAt < request.EndsAt
                    && r.EndsAt > request.StartsAt)
                .OrderBy(r => r.StartsAt)
                .Take(20)
                .Select(r => new { r.Id, r.ReferenceNumber, r.Status, r.RequesterName, r.StartsAt, r.EndsAt })
                .ToListAsync(cancellationToken);
            overlaps = rows
                .Select(r => new OverlappingRequestDto(r.Id, ClinicalSpaceRequest.FormatReference(r.ReferenceNumber), r.Status, r.RequesterName, r.StartsAt, r.EndsAt))
                .ToList();
        }

        return ToDetail(request, overlaps);
    }

    public async Task<RequestDetailDto> DecideAsync(Guid id, DecideRequestCommand command, CancellationToken cancellationToken)
    {
        EnsureAdministrator();
        await decideValidator.ValidateAndThrowAsync(command, cancellationToken);

        byte[] expectedVersion;
        try
        {
            expectedVersion = Convert.FromBase64String(command.Version!);
        }
        catch (FormatException)
        {
            throw new ValidationException([new ValidationFailure(nameof(DecideRequestCommand.Version), "The request version is not valid. Reload the page and try again.")]);
        }

        var administrator = await db.Users.SingleOrDefaultAsync(u => u.Id == currentUser.UserId && u.IsActive, cancellationToken)
            ?? throw new NotFoundException("User");

        var request = await db.Requests
            .Include(r => r.Building!).ThenInclude(b => b.Site!).ThenInclude(s => s.Region)
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException("Request");

        if (!request.RowVersion.AsSpan().SequenceEqual(expectedVersion))
        {
            throw new ConcurrencyConflictException();
        }

        db.SetOriginalRowVersion(request, expectedVersion);

        var now = clock.GetUtcNow();
        if (command.Decision == Decision.Accept)
        {
            request.Accept(administrator, command.Notes, now);
        }
        else
        {
            request.Reject(administrator, command.Notes!, now);
        }

        // Outbox: the email is saved in the same transaction as the decision.
        db.EmailMessages.Add(emails.Decided(request, Describe(PathOf(request))));

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrent decision on request {RequestId}", id);
            throw new ConcurrencyConflictException();
        }

        logger.LogInformation("Request {RequestId} {Status} by {UserId}", request.Id, request.Status, administrator.Id);
        return await GetAsync(request.Id, cancellationToken);
    }

    private static async Task<PagedResult<RequestSummaryDto>> ListAsync(
        IQueryable<ClinicalSpaceRequest> source,
        RequestListQuery query,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        if (query.Status is { } status)
        {
            source = source.Where(r => r.Status == status);
        }

        if (query.RegionId is { } regionId)
        {
            source = source.Where(r => r.Building!.Site!.RegionId == regionId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            if (term.Length > 100)
            {
                term = term[..100];
            }

            var hasReference = ClinicalSpaceRequest.TryParseReference(term, out var referenceNumber);
            source = source.Where(r =>
                (hasReference && r.ReferenceNumber == referenceNumber)
                || r.RequesterName.Contains(term)
                || r.CostCode.Contains(term)
                || r.BudgetHolderName.Contains(term)
                || r.Building!.Name.Contains(term)
                || r.Building.Site!.Name.Contains(term));
        }

        var total = await source.CountAsync(cancellationToken);

        source = (query.Sort, query.Descending) switch
        {
            (RequestSortField.StartsAt, false) => source.OrderBy(r => r.StartsAt).ThenBy(r => r.ReferenceNumber),
            (RequestSortField.StartsAt, true) => source.OrderByDescending(r => r.StartsAt).ThenByDescending(r => r.ReferenceNumber),
            (RequestSortField.Reference, false) => source.OrderBy(r => r.ReferenceNumber),
            (RequestSortField.Reference, true) => source.OrderByDescending(r => r.ReferenceNumber),
            (RequestSortField.Requester, false) => source.OrderBy(r => r.RequesterName).ThenByDescending(r => r.ReferenceNumber),
            (RequestSortField.Requester, true) => source.OrderByDescending(r => r.RequesterName).ThenByDescending(r => r.ReferenceNumber),
            (RequestSortField.Status, false) => source.OrderBy(r => r.Status).ThenByDescending(r => r.ReferenceNumber),
            (RequestSortField.Status, true) => source.OrderByDescending(r => r.Status).ThenByDescending(r => r.ReferenceNumber),
            (_, false) => source.OrderBy(r => r.CreatedAtUtc).ThenBy(r => r.ReferenceNumber),
            _ => source.OrderByDescending(r => r.CreatedAtUtc).ThenByDescending(r => r.ReferenceNumber),
        };

        var rows = await source
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.Id,
                r.ReferenceNumber,
                r.Status,
                r.RequesterName,
                Region = r.Building!.Site!.Region!.Name,
                Site = r.Building.Site.Name,
                Building = r.Building.Name,
                r.StartsAt,
                r.EndsAt,
                r.CreatedAtUtc,
                r.DecidedAtUtc,
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(r => new RequestSummaryDto(
                r.Id,
                ClinicalSpaceRequest.FormatReference(r.ReferenceNumber),
                r.Status,
                r.RequesterName,
                r.Region,
                r.Site,
                r.Building,
                r.StartsAt,
                r.EndsAt,
                r.CreatedAtUtc,
                r.DecidedAtUtc))
            .ToList();

        return new PagedResult<RequestSummaryDto>(items, page, pageSize, total);
    }

    private void EnsureAdministrator()
    {
        if (!currentUser.IsAdministrator)
        {
            // Defence in depth: the API policy should already have refused this call.
            throw new NotFoundException("Request");
        }
    }

    private static LocationPath PathOf(ClinicalSpaceRequest r) =>
        new(r.Building!.Site!.RegionId, r.Building.Site.Region!.Name, r.Building.SiteId, r.Building.Site.Name, r.BuildingId, r.Building.Name);

    private static string Describe(LocationPath path) => $"{path.Building}, {path.Site} ({path.Region})";

    private static RequestDetailDto ToDetail(ClinicalSpaceRequest r, IReadOnlyList<OverlappingRequestDto>? overlaps) =>
        new(
            r.Id,
            r.Reference,
            r.Status,
            r.RequesterName,
            r.RequesterEmail,
            r.CostCode,
            r.BudgetHolderName,
            r.BudgetHolderEmail,
            PathOf(r),
            r.StartsAt,
            r.EndsAt,
            r.SinkRequired,
            r.ClinicalCouchRequired,
            r.AdditionalRequirementsNotes,
            r.TermsVersion,
            r.TermsAcceptedAtUtc,
            r.CreatedAtUtc,
            r.UpdatedAtUtc,
            r.DecidedBy?.DisplayName,
            r.DecidedAtUtc,
            r.DecisionNotes,
            Convert.ToBase64String(r.RowVersion),
            r.Events
                .OrderBy(e => e.OccurredAtUtc).ThenBy(e => e.Id)
                .Select(e => new RequestEventDto(e.Type, e.ActorName, e.OccurredAtUtc, e.Notes))
                .ToList(),
            overlaps);
}
