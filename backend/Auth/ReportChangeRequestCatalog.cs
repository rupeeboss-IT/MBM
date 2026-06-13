namespace RB_Website_API.Auth;

public static class ReportChangeRequestCatalog
{
    public const string TypeDelete = "Delete";
    public const string TypeReplace = "Replace";
    public const string TypeEdit = "Edit";

    public const string StatusPending = "Pending";
    public const string StatusApproved = "Approved";
    public const string StatusRejected = "Rejected";

    public static readonly IReadOnlySet<string> ValidTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        TypeDelete, TypeReplace, TypeEdit,
    };

    public static readonly IReadOnlySet<string> ValidStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        StatusPending, StatusApproved, StatusRejected,
    };

    public static string NormalizeType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var t = value.Trim();
        if (string.Equals(t, TypeDelete, StringComparison.OrdinalIgnoreCase)) return TypeDelete;
        if (string.Equals(t, TypeReplace, StringComparison.OrdinalIgnoreCase)) return TypeReplace;
        if (string.Equals(t, TypeEdit, StringComparison.OrdinalIgnoreCase)) return TypeEdit;
        return t;
    }

    public static string NormalizeStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var s = value.Trim();
        if (string.Equals(s, StatusPending, StringComparison.OrdinalIgnoreCase)) return StatusPending;
        if (string.Equals(s, StatusApproved, StringComparison.OrdinalIgnoreCase)) return StatusApproved;
        if (string.Equals(s, StatusRejected, StringComparison.OrdinalIgnoreCase)) return StatusRejected;
        return s;
    }
}
