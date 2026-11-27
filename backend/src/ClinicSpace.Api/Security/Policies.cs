namespace ClinicSpace.Api.Security;

/// <summary>Names of the authorisation policies. Endpoints use these, never raw role strings.</summary>
public static class Policies
{
    public const string Requester = "Requester";
    public const string Administrator = "Administrator";
}

/// <summary>Custom claim types issued in the session cookie.</summary>
public static class ClinicSpaceClaims
{
    /// <summary>When the user signed in (Unix seconds). Used to enforce an absolute session lifetime.</summary>
    public const string AuthTime = "auth_time";
}
