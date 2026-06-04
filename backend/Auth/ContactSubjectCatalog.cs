namespace RB_Website_API.Auth;

/// <summary>Contact form subject dropdown values (numeric ids).</summary>
public static class ContactSubjectCatalog
{
    public static readonly IReadOnlyDictionary<int, string> Labels = new Dictionary<int, string>
    {
        [1] = "Payment",
        [2] = "Loan Enquiry",
        [3] = "Government Scheme",
        [4] = "Company Formation",
        [5] = "Technology Services",
        [6] = "Digital Marketing",
        [7] = "Membership",
        [8] = "Partnership",
        [9] = "General Enquiry",
        [10] = "Other",
    };

    public static bool IsValid(int subjectId) => Labels.ContainsKey(subjectId);

    public static string GetLabel(int subjectId) =>
        Labels.TryGetValue(subjectId, out var label) ? label : "Enquiry";
}
