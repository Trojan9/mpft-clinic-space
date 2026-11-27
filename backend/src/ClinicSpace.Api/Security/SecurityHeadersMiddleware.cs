namespace ClinicSpace.Api.Security;

/// <summary>
/// Adds OWASP-recommended response headers to API responses. The web container (nginx) sets the
/// equivalent headers, including the Content Security Policy, for the single-page app.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";

            // The interactive API docs need scripts; everything else is JSON and needs nothing.
            if (!path.StartsWithSegments("/api/docs"))
            {
                headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
            }

            // Responses contain personal data, so browsers and proxies must not cache them.
            if (path.StartsWithSegments("/api"))
            {
                headers.CacheControl = "no-store";
                headers.Pragma = "no-cache";
            }

            headers["X-Correlation-ID"] = context.TraceIdentifier;
            return Task.CompletedTask;
        });
        return next(context);
    }
}
