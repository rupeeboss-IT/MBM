namespace RB_Website_API.Auth;

/// <summary>
/// Stores registration lead source on PaymentOrders.Notes without schema changes.
/// </summary>
public static class PaymentOrderRegistrationSource
{
    private const string Prefix = "mbm_reg_source=";

    public static string? EncodeNotes(string? registrationSource)
    {
        var normalized = RegistrationLeadSources.Normalize(registrationSource);
        return normalized is null ? null : Prefix + normalized;
    }

    public static string? ReadFromNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes)) return null;
        var trimmed = notes.Trim();
        if (!trimmed.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)) return null;
        return RegistrationLeadSources.Normalize(trimmed[Prefix.Length..]);
    }
}
