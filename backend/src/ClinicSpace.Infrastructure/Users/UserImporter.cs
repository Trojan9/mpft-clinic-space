using ClinicSpace.Domain.Users;
using ClinicSpace.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicSpace.Infrastructure.Users;

public sealed record UserImportResult(int Added, int AlreadyExisted, int Skipped);

/// <summary>
/// Creates accounts from the supplied users file. The email address is the sign-in username.
/// Passwords are hashed (PBKDF2) before they are stored; the plain-text value is never saved or logged.
/// Existing accounts are left alone, so running the import again changes nothing.
/// </summary>
public sealed class UserImporter(AppDbContext db, IPasswordHasher<User> passwordHasher, ILogger<UserImporter> logger)
{
    public async Task<UserImportResult> ImportAsync(IReadOnlyList<UserRow> rows, int skipped, CancellationToken cancellationToken)
    {
        var existing = (await db.Users.Select(u => u.NormalizedUsername).ToListAsync(cancellationToken)).ToHashSet();
        var added = 0;
        var alreadyExisted = 0;

        foreach (var row in rows)
        {
            var normalized = User.Normalize(row.Email);
            if (!existing.Add(normalized))
            {
                alreadyExisted++;
                continue;
            }

            var user = new User
            {
                Username = row.Email,
                NormalizedUsername = normalized,
                DisplayName = row.Name.Length > User.DisplayNameMaxLength ? row.Name[..User.DisplayNameMaxLength] : row.Name,
                Email = row.Email,
                Role = row.Role,
            };
            user.PasswordHash = passwordHasher.HashPassword(user, row.Password);
            db.Users.Add(user);
            added++;
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "User import: added {Added}, already existed {Existing}, skipped {Skipped}",
            added, alreadyExisted, skipped);
        return new UserImportResult(added, alreadyExisted, skipped);
    }
}
