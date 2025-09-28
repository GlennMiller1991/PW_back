using System.Security.Claims;

namespace webapi.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var success = int.TryParse(claim, out var userId);
        if (!success) throw new Exception("Not authorized");
        return userId;
    }

    public static int GetSessionVersion(this ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst("session_version")?.Value;
        var success = int.TryParse(claim, out var sessionVersion);
        if (!success) throw new Exception("Not authorized");
        return sessionVersion;
    }
}