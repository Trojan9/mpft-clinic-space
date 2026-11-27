using System.Globalization;
using System.Security.Claims;
using ClinicSpace.Application.Auth;
using ClinicSpace.Domain.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

namespace ClinicSpace.Api.Security;

public static class AuthenticationSetup
{
    public const string SessionCookieName = "mcs_session";

    /// <summary>
    /// Cookie authentication for the same-origin SPA. Chosen over JWTs in browser storage because an
    /// HttpOnly cookie cannot be read by injected script (see ADR 0003). In production this would be
    /// replaced by Microsoft Entra ID (OpenID Connect); the role policies would stay the same.
    /// </summary>
    public static IServiceCollection AddClinicSpaceAuthentication(this IServiceCollection services, SecurityOptions security, AuthOptions auth)
    {
        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = SessionCookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = security.RequireSecureCookies ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(auth.SessionIdleTimeoutMinutes);
                options.SlidingExpiration = true;

                // An API returns status codes; it never redirects to a login page.
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };

                // Re-check the user on every request, so disabling an account or changing a role
                // takes effect at once, and enforce an absolute session lifetime.
                options.Events.OnValidatePrincipal = async context =>
                {
                    var principal = context.Principal;
                    var clock = context.HttpContext.RequestServices.GetRequiredService<TimeProvider>();
                    var authTimeClaim = principal?.FindFirstValue(ClinicSpaceClaims.AuthTime);
                    var expired = !long.TryParse(authTimeClaim, NumberStyles.Integer, CultureInfo.InvariantCulture, out var authTime)
                        || clock.GetUtcNow() - DateTimeOffset.FromUnixTimeSeconds(authTime) > TimeSpan.FromHours(security.AbsoluteSessionHours);

                    User? user = null;
                    if (!expired && Guid.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                    {
                        var authService = context.HttpContext.RequestServices.GetRequiredService<AuthService>();
                        user = await authService.FindActiveUserAsync(userId, context.HttpContext.RequestAborted);
                    }

                    if (user is null || user.Role.ToString() != principal?.FindFirstValue(ClaimTypes.Role))
                    {
                        context.RejectPrincipal();
                        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    }
                };
            });

        services.AddAuthorizationBuilder()
            // Secure by default: every endpoint needs a signed-in user unless marked [AllowAnonymous].
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(Policies.Requester, p => p.RequireAuthenticatedUser().RequireRole(nameof(UserRole.Requester)))
            .AddPolicy(Policies.Administrator, p => p.RequireAuthenticatedUser().RequireRole(nameof(UserRole.Administrator)));

        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-XSRF-TOKEN";
            options.Cookie.Name = "mcs_antiforgery";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = security.RequireSecureCookies ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
        });

        return services;
    }
}
