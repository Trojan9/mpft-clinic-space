using System.Security.Claims;
using ClinicSpace.Application.Abstractions;
using ClinicSpace.Domain.Users;

namespace ClinicSpace.Api.Security;

/// <summary>Reads the signed-in user from the authenticated cookie principal.</summary>
public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal Principal =>
        accessor.HttpContext?.User ?? throw new InvalidOperationException("No HTTP context.");

    public Guid UserId =>
        Guid.TryParse(Principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new UnauthorizedAccessException("The user is not signed in.");

    public UserRole Role =>
        Enum.TryParse<UserRole>(Principal.FindFirstValue(ClaimTypes.Role), out var role)
            ? role
            : throw new UnauthorizedAccessException("The user has no role.");
}
