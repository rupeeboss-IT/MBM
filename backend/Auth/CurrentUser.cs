using System.Security.Claims;

namespace RB_Website_API.Auth;

public static class CurrentUser
{
    public static Guid? GetUserId(ClaimsPrincipal? user)
    {
        var raw = user?.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? user?.FindFirstValue(ClaimTypes.Name)
                  ?? user?.FindFirst("sub")?.Value;
        return Guid.TryParse(raw, out var id) && id != Guid.Empty ? id : null;
    }

    public static Guid RequireUserId(ClaimsPrincipal user)
    {
        var id = GetUserId(user);
        if (id is null) throw new UnauthorizedAccessException("Invalid session.");
        return id.Value;
    }

    public static string? GetRole(ClaimsPrincipal? user) =>
        user?.FindFirstValue(ClaimTypes.Role)?.Trim().ToLowerInvariant();

    public static bool IsSuperAdmin(ClaimsPrincipal user) =>
        string.Equals(GetRole(user), "superadmin", StringComparison.OrdinalIgnoreCase);
}
