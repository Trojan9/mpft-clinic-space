using System.Text;
using ClosedXML.Excel;

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

    public static IReadOnlyList<LocationRow> Read(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".csv" => ReadCsv(File.ReadAllText(path, Encoding.UTF8)),
            ".xlsx" => ReadXlsx(path),
            _ => throw new NotSupportedException($"Location file type '{extension}' is not supported. Use .csv or .xlsx."),
        };
    }

    public static IReadOnlyList<LocationRow> ReadCsv(string content)
    {
        var records = ParseCsv(content.TrimStart('﻿')).ToList();
        return ToRows(records);
    }

    private static IReadOnlyList<LocationRow> ReadXlsx(string path)
    {
        using var workbook = new XLWorkbook(path);
        var sheet = workbook.Worksheets.First();
        var records = new List<IReadOnlyList<string>>();
        foreach (var row in sheet.RowsUsed())
        {
            var lastColumn = row.LastCellUsed()?.Address.ColumnNumber ?? 0;
            var values = new List<string>(lastColumn);
            for (var column = 1; column <= lastColumn; column++)
            {
                values.Add(row.Cell(column).GetFormattedString());
            }

            records.Add(values);
        }

        return ToRows(records);
    }

    private static List<LocationRow> ToRows(List<IReadOnlyList<string>> records)
    {
        if (records.Count == 0)
        {
            return [];
        }

        var header = records[0].Select(Normalize).ToList();
        var regionIndex = FindColumn(header, RegionHeaders, "Region");
        var siteIndex = FindColumn(header, SiteHeaders, "Site");
        var buildingIndex = FindColumn(header, BuildingHeaders, "Building");

        var rows = new List<LocationRow>();
        foreach (var record in records.Skip(1))
        {
            string Cell(int index) => index < record.Count ? Clean(record[index]) : string.Empty;
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

    private static int FindColumn(List<string> header, string[] candidates, string label)
    {
        var index = header.FindIndex(h => candidates.Contains(h));
        return index >= 0
            ? index
            : throw new InvalidDataException($"The location file has no '{label}' column. Columns found: {string.Join(", ", header)}.");
    }

    private static string Normalize(string header) =>
        new(header.Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

    // Collapses internal whitespace and trims, so "St  George's " and "St George's" are the same.
    private static string Clean(string value) =>
        string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    /// <summary>Minimal RFC 4180 parser: quoted fields, escaped quotes, commas and new lines in quotes.</summary>
    internal static IEnumerable<IReadOnlyList<string>> ParseCsv(string content)
    {
        var fields = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < content.Length; i++)
        {
            var c = content[i];
            if (inQuotes)
            {
                if (c == '"' && i + 1 < content.Length && content[i + 1] == '"')
                {
                    field.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    inQuotes = false;
                }
                else
                {
                    field.Append(c);
                }

                continue;
            }

            switch (c)
            {
                case '"':
                    inQuotes = true;
                    break;
                case ',':
                    fields.Add(field.ToString());
                    field.Clear();
                    break;
                case '\r':
                    break;
                case '\n':
                    fields.Add(field.ToString());
                    field.Clear();
                    if (fields.Any(f => f.Length > 0))
                    {
                        yield return fields;
                    }

                    fields = [];
                    break;
                default:
                    field.Append(c);
                    break;
            }
        }

        fields.Add(field.ToString());
        if (fields.Any(f => f.Length > 0))
        {
            yield return fields;
        }
    }
}
