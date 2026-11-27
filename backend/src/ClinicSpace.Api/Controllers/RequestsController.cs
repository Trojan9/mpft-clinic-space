using ClinicSpace.Api.Security;
using ClinicSpace.Application.Common;
using ClinicSpace.Application.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicSpace.Api.Controllers;

/// <summary>Clinical space requests. Requesters create and view their own; administrators view all and decide.</summary>
[ApiController]
[Route("api/requests")]
[Produces("application/json")]
public sealed class RequestsController(RequestService requests) : ControllerBase
{
    /// <summary>Submit a new request (requesters only). Requester name and email come from the session.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.Requester)]
    [ProducesResponseType<RequestDetailDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateRequestCommand command, CancellationToken cancellationToken)
    {
        var created = await requests.CreateAsync(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>The signed-in requester's own requests ("My Requests").</summary>
    [HttpGet("mine")]
    [Authorize(Policy = Policies.Requester)]
    public Task<PagedResult<RequestSummaryDto>> Mine([FromQuery] RequestListQuery query, CancellationToken cancellationToken) =>
        requests.ListMineAsync(query, cancellationToken);

    /// <summary>All requests, with search, filters, sorting and paging (administrators only).</summary>
    [HttpGet]
    [Authorize(Policy = Policies.Administrator)]
    public Task<PagedResult<RequestSummaryDto>> All([FromQuery] RequestListQuery query, CancellationToken cancellationToken) =>
        requests.ListAllAsync(query, cancellationToken);

    /// <summary>Number of requests in each status (administrators only).</summary>
    [HttpGet("summary")]
    [Authorize(Policy = Policies.Administrator)]
    public Task<RequestStatusCounts> Summary(CancellationToken cancellationToken) =>
        requests.GetStatusCountsAsync(cancellationToken);

    /// <summary>One request. Requesters get 404 for requests that are not theirs.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<RequestDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<RequestDetailDto> Get(Guid id, CancellationToken cancellationToken) =>
        requests.GetAsync(id, cancellationToken);

    /// <summary>Accept or reject a submitted request (administrators only). Sends the requester an email.</summary>
    [HttpPost("{id:guid}/decision")]
    [Authorize(Policy = Policies.Administrator)]
    [ProducesResponseType<RequestDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<RequestDetailDto> Decide(Guid id, DecideRequestCommand command, CancellationToken cancellationToken) =>
        requests.DecideAsync(id, command, cancellationToken);
}
