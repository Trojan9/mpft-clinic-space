namespace ClinicSpace.Infrastructure.Seeding;

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    /// <summary>Apply EF Core migrations when the API starts. Convenient locally; off in production (see README).</summary>
    public bool ApplyMigrations { get; set; }

    /// <summary>Import locations from <see cref="LocationsFile"/> when the Regions table is empty.</summary>
    public bool ImportLocations { get; set; }

    public string LocationsFile { get; set; } = "data/exercise-data.xlsx";

    /// <summary>Create accounts from <see cref="UsersFile"/> (the "Users" sheet of the supplied workbook).</summary>
    public bool ImportUsers { get; set; }

    public string UsersFile { get; set; } = "data/exercise-data.xlsx";

    /// <summary>
    /// Create sample requests so every screen has data. If no users exist (no users file), also
    /// create four demo users with <see cref="DemoUserPassword"/>. Development and demo only.
    /// </summary>
    public bool CreateDemoData { get; set; }

    /// <summary>Password for the fallback demo users. Needed only when no users file is imported.</summary>
    public string? DemoUserPassword { get; set; }
}
