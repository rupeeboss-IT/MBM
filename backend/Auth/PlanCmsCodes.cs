namespace RB_Website_API.Auth;

public static class PlanCmsCodes
{
    public static readonly HashSet<string> Managed = new(StringComparer.OrdinalIgnoreCase)
    {
        "basic",
        "standard",
        "pro",
        "premium",
    };

    public static bool IsManaged(string? code)
        => !string.IsNullOrWhiteSpace(code) && Managed.Contains(code.Trim());
}
