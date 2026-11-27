using ClinicSpace.Domain.Common;

namespace ClinicSpace.Domain.Users;

/// <summary>
/// A member of staff who can sign in. Passwords are never stored, only a salted PBKDF2 hash.
/// </summary>
public class User : IAuditable
{
    public const int UsernameMaxLength = 64;
    public const int DisplayNameMaxLength = 128;
    public const int EmailMaxLength = 254;

    public Guid Id { get; set; } = Guid.NewGuid();

    public string Username { get; set; } = string.Empty;

    /// <summary>Upper-case invariant username, used for case-insensitive unique look-ups.</summary>
    public string NormalizedUsername { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Requester;

    /// <summary>Inactive users cannot sign in, and their existing sessions stop working.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Consecutive failed sign-in attempts since the last success or lockout.</summary>
    public int AccessFailedCount { get; set; }

    /// <summary>When set and in the future, sign-in is refused.</summary>
    public DateTimeOffset? LockoutEndUtc { get; set; }

    public DateTimeOffset? LastSignInAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public bool IsAdministrator => Role == UserRole.Administrator;

    public static string Normalize(string username) => username.Trim().ToUpperInvariant();

    public bool IsLockedOut(DateTimeOffset now) => LockoutEndUtc is { } end && end > now;

    /// <summary>
    /// Records a failed sign-in and locks the account once the threshold is reached.
    /// </summary>
    /// <returns><c>true</c> if this failure caused the account to be locked.</returns>
    public bool RegisterFailedSignIn(DateTimeOffset now, int maxAttempts, TimeSpan lockoutDuration)
    {
        AccessFailedCount++;
        if (AccessFailedCount < maxAttempts)
        {
            return false;
        }

        LockoutEndUtc = now.Add(lockoutDuration);
        AccessFailedCount = 0;
        return true;
    }

    public void RegisterSuccessfulSignIn(DateTimeOffset now)
    {
        AccessFailedCount = 0;
        LockoutEndUtc = null;
        LastSignInAtUtc = now;
    }
}
