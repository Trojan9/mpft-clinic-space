using ClinicSpace.Domain.Locations;
using ClinicSpace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicSpace.Infrastructure.Locations;

public sealed record LocationImportResult(int RegionsAdded, int SitesAdded, int BuildingsAdded, int RowsRead);

/// <summary>
/// Adds regions, sites and buildings from the location file. Safe to run more than once: existing
/// items (matched by name, ignoring case) are left alone and only new ones are added. Nothing is
/// deleted, because existing requests may point at a building.
/// </summary>
public sealed class LocationImporter(AppDbContext db, ILogger<LocationImporter> logger)
{
    public async Task<LocationImportResult> ImportAsync(IReadOnlyList<LocationRow> rows, CancellationToken cancellationToken)
    {
        var regions = await db.Regions.Include(r => r.Sites).ThenInclude(s => s.Buildings).AsSplitQuery().ToListAsync(cancellationToken);
        int regionsAdded = 0, sitesAdded = 0, buildingsAdded = 0;

        foreach (var row in rows)
        {
            var region = regions.FirstOrDefault(r => string.Equals(r.Name, row.Region, StringComparison.OrdinalIgnoreCase));
            if (region is null)
            {
                region = new Region { Name = Truncate(row.Region, Region.NameMaxLength) };
                regions.Add(region);
                db.Regions.Add(region);
                regionsAdded++;
            }

            var site = region.Sites.FirstOrDefault(s => string.Equals(s.Name, row.Site, StringComparison.OrdinalIgnoreCase));
            if (site is null)
            {
                site = new Site { Name = Truncate(row.Site, Site.NameMaxLength), Region = region };
                region.Sites.Add(site);
                sitesAdded++;
            }

            if (!site.Buildings.Any(b => string.Equals(b.Name, row.Building, StringComparison.OrdinalIgnoreCase)))
            {
                site.Buildings.Add(new Building { Name = Truncate(row.Building, Building.NameMaxLength), Site = site });
                buildingsAdded++;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        var result = new LocationImportResult(regionsAdded, sitesAdded, buildingsAdded, rows.Count);
        logger.LogInformation(
            "Location import: {Rows} rows read; added {Regions} regions, {Sites} sites, {Buildings} buildings",
            result.RowsRead, result.RegionsAdded, result.SitesAdded, result.BuildingsAdded);
        return result;
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
}
