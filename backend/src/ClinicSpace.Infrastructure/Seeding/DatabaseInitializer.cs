using ClinicSpace.Domain.Requests;
using ClinicSpace.Domain.Users;
using ClinicSpace.Infrastructure.Locations;
using ClinicSpace.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClinicSpace.Infrastructure.Seeding;

/// <summary>
/// Runs at start-up: applies migrations, imports locations and creates demo data, each only if
/// switched on in configuration. Every step checks what already exists, so restarts are safe.
/// </summary>
public sealed class DatabaseInitializer(
    AppDbContext db,
    LocationImporter importer,
    IPasswordHasher<User> passwordHasher,
    TimeProvider clock,
    IOptions<SeedOptions> options,
    ILogger<DatabaseInitializer> logger)
{
    public static readonly (string Username, string Name, string Email, UserRole Role)[] DemoUsers =
    [
        ("sarah.khan", "Sarah Khan", "sarah.khan@clinicspace.test", UserRole.Requester),
        ("james.wright", "James Wright", "james.wright@clinicspace.test", UserRole.Requester),
        ("olivia.bennett", "Olivia Bennett", "olivia.bennett@clinicspace.test", UserRole.Administrator),
        ("daniel.okafor", "Daniel Okafor", "daniel.okafor@clinicspace.test", UserRole.Administrator),
    ];

    public async Task InitialiseAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;

        if (settings.ApplyMigrations)
        {
            await MigrateWithRetryAsync(cancellationToken);
        }

        if (settings.ImportLocations && !await db.Regions.AnyAsync(cancellationToken))
        {
            if (File.Exists(settings.LocationsFile))
            {
                await importer.ImportAsync(LocationFileReader.Read(settings.LocationsFile), cancellationToken);
            }
            else
            {
                logger.LogWarning("Location file {File} not found; no locations imported", settings.LocationsFile);
            }
        }

        if (settings.CreateDemoData)
        {
            await SeedDemoUsersAsync(settings, cancellationToken);
            await SeedSampleRequestsAsync(cancellationToken);
        }
    }

    // SQL Server can take a few seconds to accept connections after its container starts.
    private async Task MigrateWithRetryAsync(CancellationToken cancellationToken)
    {
        const int maxAttempts = 10;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await db.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("Database migrations applied");
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts && ex is not OperationCanceledException)
            {
                logger.LogWarning("Database not ready (attempt {Attempt}/{Max}): {Message}", attempt, maxAttempts, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(Math.Min(2 * attempt, 10)), cancellationToken);
            }
        }
    }

    private async Task SeedDemoUsersAsync(SeedOptions settings, CancellationToken cancellationToken)
    {
        if (await db.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(settings.DemoUserPassword) || settings.DemoUserPassword.Length < 12)
        {
            throw new InvalidOperationException("Seed:DemoUserPassword must be set (at least 12 characters) to create demo users.");
        }

        foreach (var (username, name, email, role) in DemoUsers)
        {
            var user = new User
            {
                Username = username,
                NormalizedUsername = User.Normalize(username),
                DisplayName = name,
                Email = email,
                Role = role,
            };
            user.PasswordHash = passwordHasher.HashPassword(user, settings.DemoUserPassword);
            db.Users.Add(user);
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Created {Count} demo users", DemoUsers.Length);
    }

    /// <summary>A handful of requests in each status so every screen has something to show.</summary>
    private async Task SeedSampleRequestsAsync(CancellationToken cancellationToken)
    {
        if (await db.Requests.AnyAsync(cancellationToken))
        {
            return;
        }

        var buildings = await db.Buildings.OrderBy(b => b.Id).Take(6).ToListAsync(cancellationToken);
        if (buildings.Count == 0)
        {
            return;
        }

        var users = await db.Users.ToDictionaryAsync(u => u.Username, cancellationToken);
        var sarah = users["sarah.khan"];
        var james = users["james.wright"];
        var olivia = users["olivia.bennett"];

        var now = clock.GetUtcNow();
        var morning = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero).AddHours(9);

        ClinicalSpaceRequest Make(User who, int buildingIndex, int daysAhead, int hours, string costCode, string budgetHolder, bool sink, bool couch, string? notes) =>
            ClinicalSpaceRequest.Submit(
                who,
                buildings[buildingIndex % buildings.Count].Id,
                costCode,
                budgetHolder,
                budgetHolder.ToLowerInvariant().Replace(' ', '.') + "@clinicspace.test",
                morning.AddDays(daysAhead),
                morning.AddDays(daysAhead).AddHours(hours),
                new AdditionalRequirements(sink, couch, notes),
                "2026-11-v1",
                now.AddDays(-Math.Max(1, 14 - daysAhead)));

        var samples = new List<ClinicalSpaceRequest>
        {
            Make(sarah, 0, 7, 8, "CC-4510", "Priya Patel", true, true, "Wheelchair access needed for patients."),
            Make(sarah, 1, 14, 4, "CC-4510", "Priya Patel", false, true, null),
            Make(james, 2, 10, 3, "MH-2207", "Tom Hughes", true, false, "Space for a family therapy session (4 people)."),
            Make(james, 0, 7, 2, "MH-2207", "Tom Hughes", false, false, null),
            Make(sarah, 3, 21, 6, "CC-4512", "Priya Patel", true, true, null),
            Make(james, 4, 5, 8, "PH-0931", "Grace Adeyemi", true, true, "Phlebotomy clinic; sharps bin required."),
        };

        for (var i = 0; i < samples.Count; i++)
        {
            samples[i].CreatedAtUtc = now.AddDays(-(samples.Count - i)).AddHours(-i);
        }

        samples[2].Accept(olivia, "Room 2 is booked for you. Collect the key from reception.", now.AddDays(-2));
        samples[3].Reject(olivia, "The building is closed for maintenance on that day. Please choose another date.", now.AddDays(-1));
        samples[4].Accept(olivia, null, now.AddHours(-5));

        db.Requests.AddRange(samples);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Created {Count} sample requests", samples.Count);
    }
}
