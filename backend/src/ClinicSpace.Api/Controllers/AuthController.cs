using System.ComponentModel.DataAnnotations;
using ClinicSpace.Api.Security;
using ClinicSpace.Application.Abstractions;
using ClinicSpace.Application.Auth;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ClinicSpace.Api.Controllers;

public sealed record SignInRequest
{
    [Required(ErrorMessage = "Enter your username")]
    [MaxLength(64, ErrorMessage = "Username must be 64 characters or fewer")]
    public string Username { get; init; } = string.Empty;

    // Upper limit stops very long passwords being used to tie up the CPU in hashing.
    [Required(ErrorMessage = "Enter your password")]
    [MaxLength(128, ErrorMessage = "Password must be 128 characters or fewer")]
    public string Password { get; init; } = string.Empty;
}

/// <summary>Sign in, sign out and "who am I". Sessions use an HttpOnly cookie; no tokens are exposed to JavaScript.</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController(
    AuthService authService,
    IAntiforgery antiforgery,
    TimeProvider clock,
    IOptions<SecurityOptions> securityOptions) : ControllerBase
{
    private const string GenericFailure =
        "Incorrect username or password. After 5 failed attempts your account will be locked for 15 minutes.";

    /// <summary>Issues an anti-forgery token. The SPA calls this before its first POST.</summary>
    [HttpGet("csrf")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Csrf()
    {
        AntiforgeryMiddleware.IssueTokens(HttpContext, antiforgery, securityOptions.Value.RequireSecureCookies);
        return NoContent();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.SignIn)]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(SignInRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.SignInAsync(request.Username, request.Password, cancellationToken);
        if (!result.Succeeded)
        {
            // Same message for wrong password, unknown user and locked account (no account enumeration).
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Sign in failed", detail: GenericFailure);
        }

        var principal = ClaimsFactory.Create(result.User!, clock.GetUtcNow());
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        // Anti-forgery tokens are bound to the user, so issue new ones for the new identity.
        HttpContext.User = principal;
        AntiforgeryMiddleware.IssueTokens(HttpContext, antiforgery, securityOptions.Value.RequireSecureCookies);

        return Ok(UserDto.From(result.User!));
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Response.Cookies.Delete(AntiforgeryMiddleware.RequestTokenCookie);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me([FromServices] ICurrentUser currentUser, CancellationToken cancellationToken)
    {
        var user = await authService.FindActiveUserAsync(currentUser.UserId, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        AntiforgeryMiddleware.IssueTokens(HttpContext, antiforgery, securityOptions.Value.RequireSecureCookies);
        return Ok(UserDto.From(user));
    }
}
