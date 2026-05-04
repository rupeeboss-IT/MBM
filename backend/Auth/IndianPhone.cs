namespace RB_Website_API.Auth;

public static class IndianPhone
{
    /// <summary>
    /// Strips non-digits and normalizes common Indian formats (91 prefix, leading 0) for consistent OTP keys and SMS dest.
    /// </summary>
    public static string Digits(string? phone)
    {
        var digits = string.Concat((phone ?? "").Where(char.IsDigit));
        if (digits.Length == 12 && digits.StartsWith("91", StringComparison.Ordinal))
            return digits[2..];
        if (digits.Length == 11 && digits.StartsWith('0'))
            return digits[1..];
        return digits;
    }
}
