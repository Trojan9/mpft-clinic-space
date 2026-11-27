using ClinicSpace.Infrastructure.Locations;
using ClosedXML.Excel;

namespace ClinicSpace.UnitTests.Infrastructure;

public class LocationFileReaderTests
{
    [Fact]
    public void Reads_a_simple_csv()
    {
        const string csv = "Region,Site,Building\nNorth,Harplands Hospital,Main Building\nNorth,Haywood Hospital,Outpatients\n";

        var rows = LocationFileReader.ReadCsv(csv);

        rows.Should().Equal(
            new LocationRow("North", "Harplands Hospital", "Main Building"),
            new LocationRow("North", "Haywood Hospital", "Outpatients"));
    }

    [Fact]
    public void Handles_quotes_commas_bom_crlf_and_extra_columns()
    {
        const string csv = "﻿Region Name,Site Name,Building Name,Postcode\r\n\"South\",\"St George's, Stafford\",\"Block \"\"A\"\"\",ST16 3SR\r\n";

        var rows = LocationFileReader.ReadCsv(csv);

        rows.Should().ContainSingle().Which.Should().Be(new LocationRow("South", "St George's, Stafford", "Block \"A\""));
    }

    [Fact]
    public void Skips_blank_and_incomplete_rows_and_merges_duplicates()
    {
        const string csv = "region,site,building\nNorth,Site A,B1\n,,\nNorth,Site A,\nnorth,  site a ,b1\nNorth,Site A,B2\n";

        var rows = LocationFileReader.ReadCsv(csv);

        rows.Select(r => r.Building).Should().Equal("B1", "B2");
    }

    [Fact]
    public void Collapses_extra_whitespace_in_names()
    {
        var rows = LocationFileReader.ReadCsv("Region,Site,Building\n  North   Staffs ,St  George's,Main\n");

        rows.Single().Should().Be(new LocationRow("North Staffs", "St George's", "Main"));
    }

    [Fact]
    public void Explains_which_column_is_missing()
    {
        var act = () => LocationFileReader.ReadCsv("Region,Site\nNorth,Harplands\n");

        act.Should().Throw<InvalidDataException>().WithMessage("*'Building'*");
    }

    [Fact]
    public void Empty_file_gives_no_rows() =>
        LocationFileReader.ReadCsv(string.Empty).Should().BeEmpty();

    [Fact]
    public void Reads_the_first_worksheet_of_an_xlsx_file()
    {
        var path = Path.Combine(Path.GetTempPath(), $"locations-{Guid.NewGuid():N}.xlsx");
        try
        {
            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.AddWorksheet("Locations");
                sheet.Cell(1, 1).Value = "Region";
                sheet.Cell(1, 2).Value = "Site";
                sheet.Cell(1, 3).Value = "Building";
                sheet.Cell(2, 1).Value = "Shropshire";
                sheet.Cell(2, 2).Value = "Redwoods Centre";
                sheet.Cell(2, 3).Value = "Main Building";
                workbook.SaveAs(path);
            }

            var rows = LocationFileReader.Read(path);

            rows.Should().ContainSingle().Which.Should().Be(new LocationRow("Shropshire", "Redwoods Centre", "Main Building"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Refuses_unsupported_file_types()
    {
        var act = () => LocationFileReader.Read("locations.json");

        act.Should().Throw<NotSupportedException>();
    }
}
