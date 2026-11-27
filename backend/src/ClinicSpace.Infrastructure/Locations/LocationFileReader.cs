namespace ClinicSpace.Infrastructure.Locations;

/// <summary>One row of the location hierarchy: a building on a site in a region.</summary>
public sealed record LocationRow(string Region, string Site, string Building);

/// <summary>
/// Reads the location data supplied with the exercise. Accepts CSV or Excel (.xlsx) with a header row.
/// Column names are matched loosely ("Region", "Region Name", "Site", "Site Name", "Building",
/// "Building Name", ...), so small differences in the supplied file do not need a code change.
/// </summary>
public static class LocationFileReader
{
    private static readonly string[] RegionHeaders = ["region", "regionname", "area", "locality"];
    private static readonly string[] SiteHeaders = ["site", "sitename", "location", "locationname"];
    private static readonly string[] BuildingHeaders = ["building", "buildingname", "premises", "block"];

    /// <summary>Worksheet names tried first in an Excel file; otherwise the first worksheet is used.</summary>
    public static readonly string[] SheetNames = ["Buildings", "Locations", "Sites"];

    public static IReadOnlyList<LocationRow> Read(string path) => ToRows(TableFileReader.Read(path, SheetNames));

    public static IReadOnlyList<LocationRow> ReadCsv(string content) =>
        ToRows(TableFileReader.ParseCsv(content.TrimStart('\uFEFF')).ToList());

    private static List<LocationRow> ToRows(List<IReadOnlyList<string>> records)
    {
        if (records.Count == 0)
        {
            return [];
        }

        var header = records[0].Select(TableFileReader.NormalizeHeader).ToList();
        var regionIndex = TableFileReader.FindColumn(header, RegionHeaders, "Region");
        var siteIndex = TableFileReader.FindColumn(header, SiteHeaders, "Site");
        var buildingIndex = TableFileReader.FindColumn(header, BuildingHeaders, "Building");

        var rows = new List<LocationRow>();
        foreach (var record in records.Skip(1))
        {
            string Cell(int index) => index < record.Count ? TableFileReader.Clean(record[index]) : string.Empty;
            var region = Cell(regionIndex);
            var site = Cell(siteIndex);
            var building = Cell(buildingIndex);
            if (region.Length == 0 || site.Length == 0 || building.Length == 0)
            {
                continue; // Skip blank or incomplete rows rather than failing the whole import.
            }

            rows.Add(new LocationRow(region, site, building));
        }

        return rows
            .DistinctBy(r => (r.Region.ToUpperInvariant(), r.Site.ToUpperInvariant(), r.Building.ToUpperInvariant()))
            .ToList();
    }
}
