namespace RB_Website_API.Auth;

public static class UserRoles
{
    public const string SuperAdmin = "superadmin";
    public const string Admin = "admin";
    public const string Partner = "partner";
    public const string Member = "member";

    public static bool IsAdminRole(string? role)
    {
        var r = Normalize(role);
        return r is Admin or SuperAdmin;
    }

    public static bool IsMemberRole(string? role)
    {
        var r = Normalize(role);
        return r is Member or Partner;
    }

    public static bool IsSuperAdmin(string? role) =>
        string.Equals(Normalize(role), SuperAdmin, StringComparison.Ordinal);

    public static string? Normalize(string? role)
    {
        var trimmed = (role ?? "").Trim().ToLowerInvariant();
        return trimmed.Length == 0 ? null : trimmed;
    }

    public static string DisplayName(string? role) => Normalize(role) switch
    {
        SuperAdmin => "Super Admin",
        Admin => "Admin",
        Partner => "Partner",
        Member => "Member",
        _ => role ?? "",
    };
}
