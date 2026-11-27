using System.Globalization;
using System.Security.Claims;
using ClinicSpace.Domain.Users;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ClinicSpace.Api.Security;

public static class ClaimsFactory
{
    public static ClaimsPrincipal Create(User user, DateTimeOffset now)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(ClinicSpaceClaims.AuthTime, now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)),
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }
}
