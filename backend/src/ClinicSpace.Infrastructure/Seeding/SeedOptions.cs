namespace ClinicSpace.Infrastructure.Seeding;

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    /// <summary>Apply EF Core migrations when the API starts. Convenient locally; off in production (see README).</summary>
    public bool ApplyMigrations { get; set; }

    /// <summary>Import locations from <see cref="LocationsFile"/> when the Regions table is empty.</summary>
    public bool ImportLocations { get; set; }

    public string LocationsFile { get; set; } = "data/locations.csv";

    /// <summary>Create demo users and sample requests. Development and demo only.</summary>
    public bool CreateDemoData { get; set; }

    /// <summary>Password for every demo user. Must be set when CreateDemoData is true.</summary>
    public string? DemoUserPassword { get; set; }
}
