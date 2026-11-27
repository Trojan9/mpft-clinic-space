using System.Text;
using ClosedXML.Excel;

namespace ClinicSpace.Infrastructure.Locations;

/// <summary>
/// Reads a table (header row + data rows) from a CSV file or from a worksheet of an Excel (.xlsx)
/// file. Shared by the location and user imports.
/// </summary>
public static class TableFileReader
{
    /// <summary>
    /// Reads every row. For .xlsx, uses the first worksheet whose name matches one of
    /// <paramref name="preferredSheets"/> (ignoring case), or the first worksheet if none match.
    /// </summary>
    public static List<IReadOnlyList<string>> Read(string path, IReadOnlyCollection<string> preferredSheets, bool requirePreferredSheet = false)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".csv" => ParseCsv(File.ReadAllText(path, Encoding.UTF8).TrimStart('﻿')).ToList(),
            ".xlsx" => ReadXlsx(path, preferredSheets, requirePreferredSheet),
            _ => throw new NotSupportedException($"File type '{extension}' is not supported. Use .csv or .xlsx."),
        };
    }

    private static List<IReadOnlyList<string>> ReadXlsx(string path, IReadOnlyCollection<string> preferredSheets, bool requirePreferredSheet)
    {
        using var workbook = new XLWorkbook(path);
        var sheet = workbook.Worksheets.FirstOrDefault(w => preferredSheets.Any(n => string.Equals(n, w.Name.Trim(), StringComparison.OrdinalIgnoreCase)));
        if (sheet is null)
        {
            if (requirePreferredSheet)
            {
                return [];
            }

            sheet = workbook.Worksheets.First();
        }

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

        return records;
    }

    /// <summary>"Email Address", "email_address" and "EmailAddress" all become "emailaddress".</summary>
    public static string NormalizeHeader(string header) =>
        new(header.Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

    public static int FindColumn(List<string> header, string[] candidates, string label, bool required = true)
    {
        var index = header.FindIndex(candidates.Contains);
        return index >= 0 || !required
            ? index
            : throw new InvalidDataException($"The file has no '{label}' column. Columns found: {string.Join(", ", header)}.");
    }

    /// <summary>Trims and collapses internal whitespace, so "St  George's " and "St George's" match.</summary>
    public static string Clean(string value) =>
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
