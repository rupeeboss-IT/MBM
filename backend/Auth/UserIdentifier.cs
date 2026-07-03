using System.Text.RegularExpressions;

namespace RB_Website_API.Auth;

public static partial class UserIdentifier
{
    public static bool TryParse(string? identifier, out string channel, out string normalized, out string? errorMessage)
    {
        channel = "";
        normalized = "";
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(identifier))
        {
            errorMessage = "Please enter your email address or mobile number.";
            return false;
        }

        var ident = identifier.Trim();
        if (ident.Contains('@'))
        {
            var email = ident.ToLowerInvariant();
            if (!EmailPattern().IsMatch(email))
            {
                errorMessage = "Please enter a valid email address.";
                return false;
            }

            channel = "email";
            normalized = email;
            return true;
        }

        var phone = IndianPhone.Digits(ident);
        if (phone.Length != 10 || !IndianMobilePattern().IsMatch(phone))
        {
            errorMessage = "Please enter a valid mobile number.";
            return false;
        }

        channel = "sms";
        normalized = phone;
        return true;
    }

    [GeneratedRegex(@"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"^[6-9]\d{9}$")]
    private static partial Regex IndianMobilePattern();
}
