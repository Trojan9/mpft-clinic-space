using ClinicSpace.Domain.Users;

namespace ClinicSpace.Application.Abstractions;

/// <summary>The signed-in user for the current request, resolved from the authentication cookie.</summary>
public interface ICurrentUser
{
    Guid UserId { get; }

    UserRole Role { get; }

    bool IsAdministrator => Role == UserRole.Administrator;
}
