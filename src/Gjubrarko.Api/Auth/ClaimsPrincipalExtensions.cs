using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Gjubrarko.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    public static Guid? UserId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : null;
}
