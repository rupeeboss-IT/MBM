namespace RB_Website_API.Auth;

public static class PlanBenefitsCatalog
{
    private static readonly Dictionary<string, string[]> Benefits = new(StringComparer.OrdinalIgnoreCase)
    {
        ["basic"] =
        [
            "Udyam Aadhaar onboarding",
            "Credibility profile",
            "National MSME network access",
            "Knowledge and events access",
            "Growth ecosystem access",
            "WhatsApp platform (basic)",
            "Basic website",
            "Practo Insurance Basic",
            "Free credit report",
        ],
        ["standard"] =
        [
            "Website development (Business / Service / Portfolio / E-commerce)",
            "Hosting + server + database included",
            "Free subdomain on qobo.dev",
            "Domain mapping FREE",
            "Mobile-friendly site with WhatsApp and call button",
            "FAQ automation and basic routing",
            "Setup + yearly hosting/maintenance",
            "Knowledge and events access",
            "Practo Insurance Basic",
            "Free credit report",
        ],
        ["premium"] =
        [
            "Infomerics verified trust score report",
            "Company scheme discovery report",
            "Bank statement analyzer",
            "Free credit report",
            "Website development",
            "WhatsApp Business platform",
            "Free relationship manager",
            "Loan audit",
            "Insurance audit",
            "MSME events access",
            "Practo Insurance (₹15 Lakh cover)",
        ],
        ["pro"] =
        [
            "Infomerics business assessment report",
            "Company scheme discovery report",
            "GeM portal registration and support",
            "Bank statement analyzer",
            "Free credit report",
            "Website development",
            "WhatsApp Business platform",
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
