namespace RB_Website_API.Auth;

public static class PlanBenefitsCatalog
{
    private static readonly Dictionary<string, string[]> Benefits = new(StringComparer.OrdinalIgnoreCase)
    {
        ["basic"] =
        [
            "Udyam Aadhaar onboarding",
            "National MSME network access",
            "Knowledge and events access",
            "Growth ecosystem access",
            "WhatsApp Business platform",
            "Basic website",
            "Business Diagnostic + 30-Min Coach Session",
            "Practo Insurance Basic",
            "Free credit report",
        ],
        ["premium"] =
        [
            "Infomerics IVerified Report",
            "Company scheme discovery report",
            "Bank statement analyzer",
            "Free credit report",
            "Basic website",
            "Business Diagnostic + 30-Min Coach Session",
            "Free relationship manager",
            "Loan audit",
            "Insurance audit",
            "MSME events access",
            "Practo Insurance (₹10 Lakh cover)",
        ],
        ["pro"] =
        [
            "Infomerics TrustScore Report",
            "Company scheme discovery report",
            "GeM portal registration and support",
            "Bank statement analyzer",
            "Free credit report",
            "Basic website",
            "Business Diagnostic + 30-Min Coach Session",
            "Free relationship manager",
            "Loan audit",
            "Insurance audit",
            "Extended MSME events access",
            "Practo Insurance (₹15 Lakh cover)",
        ],
    };

    public static IReadOnlyList<string> GetBenefits(string planCode)
    {
        if (Benefits.TryGetValue(planCode.Trim(), out var list)) return list;
        return Array.Empty<string>();
    }
}
