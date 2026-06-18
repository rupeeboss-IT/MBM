namespace RB_Website_API.Auth;

public static class EnquirySources
{
    public const string ContactUsPage = "Contact Us Page";
    public const string SchemeDiscoveryPage = "Scheme Discovery Page";
    public const string HomePage = "Home Page";
    public const string MembershipPage = "Membership Page";
    public const string ReportPage = "Report Page";
    public const string OtherPages = "Other Pages";

    public static readonly IReadOnlyList<string> All =
    [
        ContactUsPage,
        SchemeDiscoveryPage,
        HomePage,
        MembershipPage,
        ReportPage,
        OtherPages,
    ];

    public static bool IsValid(string? source) =>
        !string.IsNullOrWhiteSpace(source) && All.Contains(source.Trim(), StringComparer.OrdinalIgnoreCase);

    public static string Normalize(string? source)
    {
        if (string.IsNullOrWhiteSpace(source)) return OtherPages;
        var match = All.FirstOrDefault(s => s.Equals(source.Trim(), StringComparison.OrdinalIgnoreCase));
        return match ?? OtherPages;
    }
}
