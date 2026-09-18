using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CampusPulse.Api.Helpers;

// Centralizes reading the current user's identity off the JWT. Controllers
// should always call these instead of trusting an id/role sent by the client
// in a request body - that's exactly the IDOR hole the security review
// flagged (client-supplied UserId on Post/Comment/Reaction/Report/Event
// join requests).
public static class ClaimsExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                    ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (value == null || !int.TryParse(value, out var id))
            throw new InvalidOperationException("No valid user id claim on the current principal.");

        return id;
    }

    public static string GetRole(this ClaimsPrincipal user)
    {
        return user.FindFirstValue("role")
               ?? user.FindFirstValue(ClaimTypes.Role)
               ?? "Student";
    }

    public static bool IsAdmin(this ClaimsPrincipal user)
        => string.Equals(user.GetRole(), "Admin", StringComparison.OrdinalIgnoreCase);
}
