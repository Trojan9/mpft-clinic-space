namespace ClinicSpace.Application.Auth;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>Failed attempts allowed before the account is locked (OWASP ASVS 2.2.1).</summary>
    public int MaxFailedAttempts { get; set; } = 5;

    public int LockoutMinutes { get; set; } = 15;

    /// <summary>Idle time before the session cookie expires. Sliding expiration renews it on use.</summary>
    public int SessionIdleTimeoutMinutes { get; set; } = 30;
}
