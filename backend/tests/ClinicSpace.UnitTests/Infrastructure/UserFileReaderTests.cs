using ClinicSpace.Domain.Users;
using ClinicSpace.Infrastructure.Locations;
using ClinicSpace.Infrastructure.Users;
using ClosedXML.Excel;

namespace ClinicSpace.UnitTests.Infrastructure;

public class UserFileReaderTests
{
    [Fact]
    public void Reads_the_supplied_column_layout_and_maps_roles()
    {
        const string csv = "Name,Email Address,Role,Password\nPat Jones,Pat.Jones@example.nhs.uk,Admin,AdminPass!23\nRiley Green,riley.green@example.nhs.uk,Requester,Password123!\n";

        var (users, problems) = UserFileReader.ReadCsv(csv);

        problems.Should().BeEmpty();
        users.Should().Equal(
            new UserRow("Pat Jones", "pat.jones@example.nhs.uk", UserRole.Administrator, "AdminPass!23"),
            new UserRow("Riley Green", "riley.green@example.nhs.uk", UserRole.Requester, "Password123!"));
    }

    [Theory]
    [InlineData("Admin", UserRole.Administrator)]
    [InlineData("administrator", UserRole.Administrator)]
    [InlineData(" REQUESTER ", UserRole.Requester)]
    public void Role_names_are_matched_loosely(string text, UserRole expected) =>
        UserFileReader.ParseRole(text).Should().Be(expected);

    [Fact]
    public void Bad_rows_are_reported_and_skipped_not_guessed()
    {
        const string csv = "Name,Email Address,Role,Password\nNo Role,a@x.test,Superuser,pw\nNo Password,b@x.test,Requester,\n,,,\nNot Email,not-an-email,Requester,pw\nGood One,c@x.test,Requester,pw\n";

        var (users, problems) = UserFileReader.ReadCsv(csv);

        users.Should().ContainSingle().Which.Email.Should().Be("c@x.test");
        problems.Should().HaveCount(3);
        problems[0].Should().Contain("Row 2").And.Contain("Superuser");
    }

    [Fact]
    public void Reads_both_sheets_of_a_workbook_shaped_like_the_supplied_data()
    {
        var path = Path.Combine(Path.GetTempPath(), $"exercise-{Guid.NewGuid():N}.xlsx");
        try
        {
            using (var workbook = new XLWorkbook())
            {
                var buildings = workbook.AddWorksheet("Buildings");
                buildings.Cell(1, 1).Value = "Region";
                buildings.Cell(1, 2).Value = "Site";
                buildings.Cell(1, 3).Value = "Building";
                buildings.Cell(2, 1).Value = "Staffordshire";
                buildings.Cell(2, 2).Value = "Stafford Community Health Hub";
                buildings.Cell(2, 3).Value = "Alder House";
                var users = workbook.AddWorksheet("Users");
                users.Cell(1, 1).Value = "Name";
                users.Cell(1, 2).Value = "Email Address";
                users.Cell(1, 3).Value = "Role";
                users.Cell(1, 4).Value = "Password";
                users.Cell(2, 1).Value = "Pat Jones";
                users.Cell(2, 2).Value = "pat.jones@example.nhs.uk";
                users.Cell(2, 3).Value = "Admin";
                users.Cell(2, 4).Value = "AdminPass!23";
                workbook.SaveAs(path);
            }

            LocationFileReader.Read(path).Should().ContainSingle()
                .Which.Should().Be(new LocationRow("Staffordshire", "Stafford Community Health Hub", "Alder House"));
            UserFileReader.Read(path).Users.Should().ContainSingle()
                .Which.Role.Should().Be(UserRole.Administrator);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void A_workbook_without_a_users_sheet_gives_no_users()
    {
        var path = Path.Combine(Path.GetTempPath(), $"locations-{Guid.NewGuid():N}.xlsx");
        try
        {
            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.AddWorksheet("Buildings");
                sheet.Cell(1, 1).Value = "Region";
                workbook.SaveAs(path);
            }

            UserFileReader.Read(path).Users.Should().BeEmpty();
        }
        finally
        {
            File.Delete(path);
        }
    }
}
