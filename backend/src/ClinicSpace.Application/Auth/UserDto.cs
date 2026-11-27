using ClinicSpace.Domain.Users;

namespace ClinicSpace.Application.Auth;

/// <summary>The signed-in user's profile as the web app sees it. Never includes the password hash.</summary>
public sealed record UserDto(Guid Id, string Username, string DisplayName, string Email, UserRole Role)
{
    public static UserDto From(User user) => new(user.Id, user.Username, user.DisplayName, user.Email, user.Role);
}
