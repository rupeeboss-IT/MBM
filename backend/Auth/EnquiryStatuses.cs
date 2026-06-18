namespace RB_Website_API.Auth;

public static class EnquiryStatuses
{
    public const string New = "New";
    public const string Read = "Read";
    public const string InProgress = "In Progress";
    public const string Resolved = "Resolved";
    public const string Closed = "Closed";

    public static readonly IReadOnlyList<string> All =
    [
        New,
        Read,
        InProgress,
        Resolved,
        Closed,
    ];

    public static bool IsValid(string? status) =>
        !string.IsNullOrWhiteSpace(status) && All.Contains(status.Trim(), StringComparer.OrdinalIgnoreCase);

    public static string Normalize(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return New;
        var match = All.FirstOrDefault(s => s.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase));
        return match ?? New;
    }

    public static string NormalizeFilter(string? status)
    {
        var normalized = Normalize(status);
        return normalized;
    }
}
