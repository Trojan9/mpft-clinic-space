using ClinicSpace.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace ClinicSpace.IntegrationTests.Infrastructure;

/// <summary>
/// Starts a real SQL Server 2022 in Docker (Testcontainers) and hosts the API in memory against it.
/// Migrations, the reference sequence, check constraints and rowversion concurrency all run for real.
/// One container is shared by every test in the collection; tests create their own data.
/// </summary>
public sealed class ClinicSpaceAppFixture : IAsyncLifetime
{
    public const string DemoPassword = "Integration-Test-Pass-1";

    private readonly MsSqlContainer _sql = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _sql.StartAsync();

        // Environment variables are read by WebApplication.CreateBuilder, so they apply before
        // Program.cs reads any configuration.
        var settings = new Dictionary<string, string>
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Testing",
            ["ConnectionStrings__ClinicSpace"] = _sql.GetConnectionString().Replace("Database=master", "Database=ClinicSpaceTests", StringComparison.Ordinal),
            ["Security__RequireSecureCookies"] = "false",
            ["Security__SignInRequestsPerMinute"] = "100000",
            ["Security__ApiRequestsPerMinute"] = "100000",
            ["Email__Mode"] = "Log",
            ["Email__DispatcherEnabled"] = "false",
            ["Seed__ApplyMigrations"] = "true",
            ["Seed__ImportLocations"] = "true",
            ["Seed__LocationsFile"] = Path.Combine(AppContext.BaseDirectory, "TestData", "locations.csv"),
            ["Seed__CreateDemoData"] = "true",
            ["Seed__DemoUserPassword"] = DemoPassword,
        };
        foreach (var (key, value) in settings)
        {
            Environment.SetEnvironmentVariable(key, value);
        }

        Factory = new WebApplicationFactory<Program>();
        _ = Factory.Server; // Start the host now: runs migrations and seeding once.
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _sql.DisposeAsync();
    }

    /// <summary>A new browser-like client (own cookie jar) that is not signed in.</summary>
    public ApiSession Anonymous() =>
        new(Factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true, AllowAutoRedirect = false }));

    /// <summary>A client signed in as the given user, with a valid anti-forgery token.</summary>
    public async Task<ApiSession> SignInAsync(string username, string? password = null)
    {
        var session = Anonymous();
        await session.PrimeCsrfAsync();
        var response = await session.PostAsync("/api/auth/login", new { username, password = password ?? DemoPassword });
        response.EnsureSuccessStatusCode();
        return session;
    }

    public async Task WithDbAsync(Func<AppDbContext, Task> action)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    public async Task<T> WithDbAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ClinicSpaceAppFixture>
{
    public const string Name = "API";
}
