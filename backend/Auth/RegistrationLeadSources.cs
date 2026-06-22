namespace RB_Website_API.Auth;

/// <summary>
/// lead_data.lead_source values for MBM membership registration leads (RBMain).
/// </summary>
public static class RegistrationLeadSources
{
    public const string MsmeRegistration = "MSMEREGISTRATION";
    public const string MsmeChatbot = "MSMECHATBOT";

    public static bool IsKnown(string? value) =>
        string.Equals(value, MsmeRegistration, StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, MsmeChatbot, StringComparison.OrdinalIgnoreCase);

    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        if (string.Equals(trimmed, MsmeRegistration, StringComparison.OrdinalIgnoreCase))
            return MsmeRegistration;
        if (string.Equals(trimmed, MsmeChatbot, StringComparison.OrdinalIgnoreCase))
            return MsmeChatbot;
        return null;
    }
}
