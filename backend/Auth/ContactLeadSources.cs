namespace RB_Website_API.Auth;

/// <summary>
/// lead_data.lead_source values for MBM contact page submissions (RBMain).
/// </summary>
public static class ContactLeadSources
{
    public const string MsmeContact = "MSMECONTACT";

    public const string LeadTypeContact = "msmecontact";

    public const string LeadTypeChatbot = "msmechatbot";

    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        if (string.Equals(trimmed, MsmeContact, StringComparison.OrdinalIgnoreCase))
            return MsmeContact;
        if (string.Equals(trimmed, RegistrationLeadSources.MsmeChatbot, StringComparison.OrdinalIgnoreCase))
            return RegistrationLeadSources.MsmeChatbot;
        return null;
    }

    public static string Resolve(string? value) =>
        Normalize(value) ?? MsmeContact;

    public static string ResolveLeadType(string? leadSource) =>
        string.Equals(Resolve(leadSource), RegistrationLeadSources.MsmeChatbot, StringComparison.Ordinal)
            ? LeadTypeChatbot
            : LeadTypeContact;
}
