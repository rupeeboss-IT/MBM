using System.Text.RegularExpressions;

namespace RB_Website_API.Auth;

/// <summary>
/// Password rules aligned with registration (frontend passwordComplexityValidator).
/// </summary>
public static partial class PasswordPolicy
{
    public static string? Validate(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return "New password is required.";

        var value = password;
        if (value.Length < 9)
            return "Password must be 9+ characters and include uppercase, lowercase, and a number.";
        if (!LowercasePattern().IsMatch(value))
            return "Password must be 9+ characters and include uppercase, lowercase, and a number.";
        if (!UppercasePattern().IsMatch(value))
            return "Password must be 9+ characters and include uppercase, lowercase, and a number.";
        if (!DigitPattern().IsMatch(value))
            return "Password must be 9+ characters and include uppercase, lowercase, and a number.";

        return null;
    }

    [GeneratedRegex("[a-z]")]
    private static partial Regex LowercasePattern();

    [GeneratedRegex("[A-Z]")]
    private static partial Regex UppercasePattern();

    [GeneratedRegex("[0-9]")]
    private static partial Regex DigitPattern();
}
