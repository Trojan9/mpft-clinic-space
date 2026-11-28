using System.Text.Json.Serialization;
using ClinicSpace.Api.Errors;
using ClinicSpace.Api.Security;
using ClinicSpace.Application;
using ClinicSpace.Application.Abstractions;
using ClinicSpace.Application.Auth;
using ClinicSpace.Infrastructure;
using ClinicSpace.Infrastructure.Locations;
using ClinicSpace.Infrastructure.Persistence;
using ClinicSpace.Infrastructure.Seeding;
using ClinicSpace.Infrastructure.Users;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ---- Configuration --------------------------------------------------------------------------
var security = builder.Configuration.GetSection(SecurityOptions.SectionName).Get<SecurityOptions>() ?? new SecurityOptions();
var auth = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection(SecurityOptions.SectionName));

// Structured JSON logs outside development, so they can go straight into a log platform.
if (!builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddJsonConsole(o => o.IncludeScopes = true);
}

builder.WebHost.ConfigureKestrel(o =>
{
    o.AddServerHeader = false;
    o.Limits.MaxRequestBodySize = 64 * 1024; // The largest valid request is a few kilobytes.
});

// ---- Services -------------------------------------------------------------------------------
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

builder.Services.AddClinicSpaceAuthentication(security, auth);
builder.Services.AddClinicSpaceRateLimiting(security);

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
    ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddOpenApi("v1", o => o.AddDocumentTransformer((doc, _, _) =>
{
    doc.Info.Title = "MPFT Clinic Space API";
    doc.Info.Description = "Request and approve clinical spaces. Authentication uses a session cookie; state-changing calls need the X-XSRF-TOKEN header.";
    return Task.CompletedTask;
}));

// Keys that protect session and anti-forgery cookies. Persisting them means a restart or a second
// instance does not sign everyone out. Production would use a shared, protected store (for
// example Azure Blob Storage + Key Vault); a mounted volume is enough for local Docker.
var dataProtection = builder.Services.AddDataProtection().SetApplicationName("mpft-clinic-space");
if (builder.Configuration["DataProtection:KeysDirectory"] is { Length: > 0 } keysDirectory)
{
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysDirectory));
}

builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database", tags: ["ready"]);

if (security.TrustForwardedHeaders)
{
    builder.Services.Configure<ForwardedHeadersOptions>(o =>
    {
        o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        o.ForwardLimit = 1;
        // The API is only reachable through the web container on a private Docker network.
        o.KnownIPNetworks.Clear();
        o.KnownProxies.Clear();
    });
}

var app = builder.Build();

// ---- One-off commands: import data from a file, then exit -----------------------------------
// Usage: dotnet ClinicSpace.Api.dll import-locations data/exercise-data.xlsx
//        dotnet ClinicSpace.Api.dll import-users data/exercise-data.xlsx
if (args.Length >= 2 && args[0] is "import-locations" or "import-users")
{
    await using var scope = app.Services.CreateAsyncScope();
    if (args[0] == "import-locations")
    {
        var importer = scope.ServiceProvider.GetRequiredService<LocationImporter>();
        var result = await importer.ImportAsync(LocationFileReader.Read(args[1]), CancellationToken.None);
        Console.WriteLine($"Imported from {args[1]}: {result.RegionsAdded} regions, {result.SitesAdded} sites, {result.BuildingsAdded} buildings ({result.RowsRead} rows).");
    }
    else
    {
        var (users, problems) = UserFileReader.Read(args[1]);
        problems.ToList().ForEach(p => Console.WriteLine($"Skipped: {p}"));
        var result = await scope.ServiceProvider.GetRequiredService<UserImporter>().ImportAsync(users, problems.Count, CancellationToken.None);
        Console.WriteLine($"Imported from {args[1]}: {result.Added} users added, {result.AlreadyExisted} already existed, {result.Skipped} skipped.");
    }

    return;
}

// ---- Start-up: migrations and seed data (each switched on by configuration) -----------------
await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitialiseAsync(CancellationToken.None);
}

// ---- HTTP pipeline --------------------------------------------------------------------------
if (security.TrustForwardedHeaders)
{
    app.UseForwardedHeaders();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.UseMiddleware<AntiforgeryMiddleware>();

if (app.Environment.IsDevelopment())
{
    // Interactive API reference at /api/docs (development only).
    app.MapOpenApi("/api/openapi/{documentName}.json").AllowAnonymous();
    app.MapScalarApiReference("/api/docs", o => o
        .WithTitle("MPFT Clinic Space API")
        .WithOpenApiRoutePattern("/api/openapi/{documentName}.json")).AllowAnonymous();
}

app.MapControllers();
app.MapHealthChecks("/health/live", new() { Predicate = _ => false }).AllowAnonymous().DisableRateLimiting();
app.MapHealthChecks("/health/ready", new() { Predicate = c => c.Tags.Contains("ready") }).AllowAnonymous().DisableRateLimiting();

await app.RunAsync();

/// <summary>Exposed so integration tests can host the app with WebApplicationFactory.</summary>
public partial class Program
{
}
