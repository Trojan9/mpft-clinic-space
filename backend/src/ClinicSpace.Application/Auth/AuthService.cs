using ClinicSpace.Application.Abstractions;
using ClinicSpace.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClinicSpace.Application.Auth;

/// <summary>
/// Checks a username and password. Uses ASP.NET Core Identity's PasswordHasher
/// (PBKDF2 with HMAC-SHA512, 100,000 iterations, random 128-bit salt).
/// </summary>
public sealed class AuthService(
    IAppDbContext db,
    IPasswordHasher<User> passwordHasher,
    TimeProvider clock,
    IOptions<AuthOptions> options,
    ILogger<AuthService> logger)
{
    // Hash of a random value. Verifying against it when the username does not exist keeps the
    // response time similar, so timing cannot reveal which usernames exist.
    private static readonly string DummyHash = new PasswordHasher<User>().HashPassword(new User(), Guid.NewGuid().ToString());

    private readonly AuthOptions _options = options.Value;

    public async Task<SignInResult> SignInAsync(string username, string password, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var normalized = User.Normalize(username ?? string.Empty);
        var user = await db.Users.SingleOrDefaultAsync(u => u.NormalizedUsername == normalized, cancellationToken);

        if (user is null || !user.IsActive)
        {
            passwordHasher.VerifyHashedPassword(new User(), DummyHash, password ?? string.Empty);
            logger.LogWarning("Sign-in failed: unknown or inactive account");
            return SignInResult.Failed(SignInFailure.InvalidCredentials);
        }

        if (user.IsLockedOut(now))
        {
            logger.LogWarning("Sign-in refused: account {UserId} is locked until {LockoutEnd}", user.Id, user.LockoutEndUtc);
            return SignInResult.Failed(SignInFailure.LockedOut);
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password ?? string.Empty);
        if (verification == PasswordVerificationResult.Failed)
        {
            var locked = user.RegisterFailedSignIn(now, _options.MaxFailedAttempts, TimeSpan.FromMinutes(_options.LockoutMinutes));
            await db.SaveChangesAsync(cancellationToken);
            if (locked)
            {
                logger.LogWarning("Account {UserId} locked after repeated failed sign-ins", user.Id);
                return SignInResult.Failed(SignInFailure.LockedOut);
            }

            logger.LogWarning("Sign-in failed for account {UserId}", user.Id);
            return SignInResult.Failed(SignInFailure.InvalidCredentials);
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            // Upgrades old hashes when the hashing parameters are strengthened.
            user.PasswordHash = passwordHasher.HashPassword(user, password!);
        }

        user.RegisterSuccessfulSignIn(now);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("User {UserId} signed in", user.Id);
        return SignInResult.Success(user);
    }

    /// <summary>Loads an active user for session validation. Returns null if disabled or deleted.</summary>
    public Task<User?> FindActiveUserAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken);
}
