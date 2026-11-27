namespace ClinicSpace.Api.Security;

public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    /// <summary>
    /// Send cookies only over HTTPS. True in production. The local Docker set-up serves plain HTTP on
    /// localhost, so it is switched off there (see docs/SECURITY.md).
    /// </summary>
    public bool RequireSecureCookies { get; set; } = true;

    /// <summary>Longest a session can last, however active, before signing in again.</summary>
    public int AbsoluteSessionHours { get; set; } = 8;

    /// <summary>Sign-in attempts allowed per client IP address per minute.</summary>
    public int SignInRequestsPerMinute { get; set; } = 10;

    /// <summary>API calls allowed per signed-in user (or IP address) per minute.</summary>
    public int ApiRequestsPerMinute { get; set; } = 300;

    /// <summary>Trust X-Forwarded-For from the reverse proxy. Only safe when the API is not exposed directly.</summary>
    public bool TrustForwardedHeaders { get; set; }
}
