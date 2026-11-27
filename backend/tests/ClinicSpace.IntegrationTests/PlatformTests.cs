using System.Net;
using ClinicSpace.Application.Locations;
using ClinicSpace.IntegrationTests.Infrastructure;

namespace ClinicSpace.IntegrationTests;

[Collection(ApiCollection.Name)]
public class PlatformTests(ClinicSpaceAppFixture app)
{
    [Fact]
    public async Task Readiness_check_reports_the_database_healthy()
    {
        using var session = app.Anonymous();

        var response = await session.GetAsync("/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("Healthy");
    }

    [Fact]
    public async Task Api_responses_carry_security_headers_and_are_not_cached()
    {
        using var session = await app.SignInAsync("sarah.khan");

        var response = await session.GetAsync("/api/auth/me");

        response.Headers.GetValues("X-Content-Type-Options").Should().Equal("nosniff");
        response.Headers.GetValues("X-Frame-Options").Should().Equal("DENY");
        response.Headers.GetValues("Referrer-Policy").Should().Equal("no-referrer");
        response.Headers.GetValues("Content-Security-Policy").Single().Should().Contain("frame-ancestors 'none'");
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        response.Headers.Contains("Server").Should().BeFalse();
    }

    [Fact]
    public async Task Location_lookups_follow_the_region_site_building_hierarchy()
    {
        using var session = await app.SignInAsync("sarah.khan");

        var regions = await session.GetJsonAsync<List<LocationOption>>("/api/locations/regions");
        var north = regions.Single(r => r.Name == "Test Region North");
        var sites = await session.GetJsonAsync<List<LocationOption>>($"/api/locations/regions/{north.Id}/sites");
        var harplands = sites.Single(s => s.Name == "Harplands Hospital");
        var buildings = await session.GetJsonAsync<List<LocationOption>>($"/api/locations/sites/{harplands.Id}/buildings");

        regions.Select(r => r.Name).Should().BeInAscendingOrder();
        buildings.Select(b => b.Name).Should().Contain(["Main Building", "Therapies Wing"]);
        (await session.GetAsync("/api/locations/regions/999999/sites")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Unhandled_errors_never_leak_stack_traces()
    {
        using var session = await app.SignInAsync("olivia.bennett");

        // A malformed concurrency token is handled as a validation error, not a crash.
        var response = await session.PostAsync($"/api/requests/{Guid.NewGuid()}/decision", new { decision = "Accept", version = "not base64!!" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("   at ").And.NotContain("Exception");
    }
}
