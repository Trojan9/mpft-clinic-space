namespace ClinicSpace.IntegrationTests.Infrastructure;

/// <summary>Builders for request bodies used across tests.</summary>
internal static class Requests
{
    private static DateTimeOffset Today => new(DateTime.UtcNow.Date, TimeSpan.Zero);

    public static async Task<int> AnyBuildingIdAsync(ClinicSpaceAppFixture app) =>
        await app.WithDbAsync(async db =>
        {
            var building = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstAsync(db.Buildings);
            return building.Id;
        });

    public static object Valid(int buildingId, int daysAhead = 5, int hours = 3, string costCode = "cc-1234") => new
    {
        costCode,
        budgetHolderName = "Priya Patel",
        budgetHolderEmail = "priya.patel@clinicspace.test",
        buildingId,
        startsAt = Today.AddDays(daysAhead).AddHours(9),
        endsAt = Today.AddDays(daysAhead).AddHours(9 + hours),
        sinkRequired = true,
        clinicalCouchRequired = false,
        additionalRequirements = "Hearing loop",
        acceptTerms = true,
    };
}
