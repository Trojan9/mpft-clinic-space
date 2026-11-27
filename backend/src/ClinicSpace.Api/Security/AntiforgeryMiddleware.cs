using Microsoft.AspNetCore.Antiforgery;

namespace ClinicSpace.Api.Security;

/// <summary>
/// Cross-site request forgery protection (double-submit token). Every state-changing API call
/// must send the X-XSRF-TOKEN header that matches the anti-forgery cookie. This sits on top of the
/// SameSite=Strict session cookie as a second, independent defence.
/// </summary>
public sealed class AntiforgeryMiddleware(RequestDelegate next)
{
    public const string RequestTokenCookie = "XSRF-TOKEN";

    public async Task InvokeAsync(HttpContext context, IAntiforgery antiforgery)
    {
        if (context.Request.Path.StartsWithSegments("/api") && !HttpMethods.IsGet(context.Request.Method)
            && !HttpMethods.IsHead(context.Request.Method) && !HttpMethods.IsOptions(context.Request.Method))
        {
            // Throws AntiforgeryValidationException, which the exception handler turns into a 400.
            await antiforgery.ValidateRequestAsync(context);
        }

        await next(context);
    }

    /// <summary>Issues a fresh token pair for the current user and exposes the request token to the SPA.</summary>
    public static void IssueTokens(HttpContext context, IAntiforgery antiforgery, bool secure)
    {
        var tokens = antiforgery.GetAndStoreTokens(context);
        context.Response.Cookies.Append(RequestTokenCookie, tokens.RequestToken!, new CookieOptions
        {
            HttpOnly = false, // The SPA must read it to copy it into the header.
            SameSite = SameSiteMode.Strict,
            Secure = secure,
            Path = "/",
        });
    }
}
