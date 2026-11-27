using ClinicSpace.Application.Abstractions;
using ClinicSpace.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace ClinicSpace.Application.Locations;

/// <summary>Look-ups behind the dependent Region → Site → Building selections. Only active items are offered.</summary>
public sealed class LocationService(IAppDbContext db)
{
    public async Task<IReadOnlyList<LocationOption>> GetRegionsAsync(CancellationToken cancellationToken) =>
        await db.Regions.AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .Select(r => new LocationOption(r.Id, r.Name))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<LocationOption>> GetSitesAsync(int regionId, CancellationToken cancellationToken)
    {
        if (!await db.Regions.AnyAsync(r => r.Id == regionId && r.IsActive, cancellationToken))
        {
            throw new NotFoundException("Region");
        }

        return await db.Sites.AsNoTracking()
            .Where(s => s.RegionId == regionId && s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new LocationOption(s.Id, s.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LocationOption>> GetBuildingsAsync(int siteId, CancellationToken cancellationToken)
    {
        if (!await db.Sites.AnyAsync(s => s.Id == siteId && s.IsActive, cancellationToken))
        {
            throw new NotFoundException("Site");
        }

        return await db.Buildings.AsNoTracking()
            .Where(b => b.SiteId == siteId && b.IsActive)
            .OrderBy(b => b.Name)
            .Select(b => new LocationOption(b.Id, b.Name))
            .ToListAsync(cancellationToken);
    }
}
