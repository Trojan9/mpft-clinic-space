using ClinicSpace.Application.Locations;
using Microsoft.AspNetCore.Mvc;

namespace ClinicSpace.Api.Controllers;

/// <summary>Region → Site → Building look-ups for the dependent drop-downs. Any signed-in user.</summary>
[ApiController]
[Route("api/locations")]
[Produces("application/json")]
public sealed class LocationsController(LocationService locations) : ControllerBase
{
    [HttpGet("regions")]
    public Task<IReadOnlyList<LocationOption>> Regions(CancellationToken cancellationToken) =>
        locations.GetRegionsAsync(cancellationToken);

    [HttpGet("regions/{regionId:int}/sites")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IReadOnlyList<LocationOption>> Sites(int regionId, CancellationToken cancellationToken) =>
        locations.GetSitesAsync(regionId, cancellationToken);

    [HttpGet("sites/{siteId:int}/buildings")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IReadOnlyList<LocationOption>> Buildings(int siteId, CancellationToken cancellationToken) =>
        locations.GetBuildingsAsync(siteId, cancellationToken);
}
