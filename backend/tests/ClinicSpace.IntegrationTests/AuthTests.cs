using System.Net;
using ClinicSpace.Application.Auth;
using ClinicSpace.Domain.Users;
using ClinicSpace.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicSpace.IntegrationTests;

[Collection(ApiCollection.Name)]
public class AuthTests(ClinicSpaceAppFixture app)
{
    [Fact]
    public async Task Sign_in_returns_the_profile_without_secrets_and_sets_an_HttpOnly_session_cookie()
    {
        using var session = app.Anonymous();
        await session.PrimeCsrfAsync();

        var response = await session.PostAsync("/api/auth/login", new { username = "SARAH.KHAN", password = ClinicSpaceAppFixture.DemoPassword });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"role\":\"Requester\"").And.NotContainAny("password", "Password", "hash");
        response.Headers.GetValues("Set-Cookie").Should().Contain(c =>
            c.StartsWith("mcs_session=", StringComparison.Ordinal)
            && c.Contains("httponly", StringComparison.OrdinalIgnoreCase)
            && c.Contains("samesite=strict", StringComparison.OrdinalIgnoreCase));

        var me = await session.GetJsonAsync<UserDto>("/api/auth/me");
        me.Username.Should().Be("sarah.khan");
        me.Role.Should().Be(UserRole.Requester);
    }

    [Theory]
    [InlineData("sarah.khan", "wrong-password")]
    [InlineData("no.such.user", "whatever-password")]
    public async Task Failed_sign_in_gives_the_same_generic_message(string username, string password)
    {
        using var session = app.Anonymous();
        await session.PrimeCsrfAsync();

        var response = await session.PostAsync("/api/auth/login", new { username, password });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Incorrect username or password");
    }

    [Fact]
    public async Task Account_locks_after_five_failures_even_with_the_right_password()
    {
        const string username = "lockout.test";
        await CreateUserAsync(username, UserRole.Requester);
        using var session = app.Anonymous();
        await session.PrimeCsrfAsync();

        for (var i = 0; i < 5; i++)
        {
            (await session.PostAsync("/api/auth/login", new { username, password = "wrong" })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        var correct = await session.PostAsync("/api/auth/login", new { username, password = ClinicSpaceAppFixture.DemoPassword });
        correct.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Inactive_users_cannot_sign_in()
    {
        const string username = "inactive.test";
        await CreateUserAsync(username, UserRole.Requester, isActive: false);
        using var session = app.Anonymous();
        await session.PrimeCsrfAsync();

        var response = await session.PostAsync("/api/auth/login", new { username, password = ClinicSpaceAppFixture.DemoPassword });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Disabling_an_account_ends_its_existing_session()
    {
        const string username = "disable.me";
        await CreateUserAsync(username, UserRole.Requester);
        using var session = await app.SignInAsync(username);
        (await session.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.OK);

        await app.WithDbAsync(async db =>
        {
            var user = db.Users.Single(u => u.Username == username);
            user.IsActive = false;
            await db.SaveChangesAsync();
        });

        (await session.GetAsync("/api/requests/mine")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Sign_out_ends_the_session()
    {
        using var session = await app.SignInAsync("james.wright");

        (await session.PostAsync("/api/auth/logout", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await session.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_without_the_anti_forgery_header_is_refused()
    {
        using var session = await app.SignInAsync("sarah.khan");
        var buildingId = await Requests.AnyBuildingIdAsync(app);

        var response = await session.PostAsync("/api/requests", Requests.Valid(buildingId), includeCsrfHeader: false);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("csrf_invalid");
    }

    [Theory]
    [InlineData("/api/requests/mine")]
    [InlineData("/api/requests")]
    [InlineData("/api/locations/regions")]
    [InlineData("/api/auth/me")]
    public async Task Protected_endpoints_return_401_when_not_signed_in(string url)
    {
        using var session = app.Anonymous();

        (await session.GetAsync(url)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Overlong_passwords_are_rejected_before_hashing()
    {
        using var session = app.Anonymous();
        await session.PrimeCsrfAsync();

        var response = await session.PostAsync("/api/auth/login", new { username = "sarah.khan", password = new string('x', 129) });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private Task CreateUserAsync(string username, UserRole role, bool isActive = true) =>
        app.WithDbAsync(async db =>
        {
            if (db.Users.Any(u => u.Username == username))
            {
                return;
            }

            await using var scope = app.Factory.Services.CreateAsyncScope();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
            var user = new User
            {
                Username = username,
                NormalizedUsername = User.Normalize(username),
                DisplayName = "Test " + username,
                Email = username + "@clinicspace.test",
                Role = role,
                IsActive = isActive,
            };
            user.PasswordHash = hasher.HashPassword(user, ClinicSpaceAppFixture.DemoPassword);
            db.Users.Add(user);
            await db.SaveChangesAsync();
        });
}
