using System.Text.RegularExpressions;
using Serilog.Core;
using Serilog.Events;

namespace RB_Website_API.Logging;

/// <summary>
/// Redacts passwords, tokens, OTP values, and other sensitive data from log messages.
/// </summary>
public sealed partial class SensitiveDataLogFilter : ILogEventFilter
{
    private static readonly string[] SensitivePropertyNames =
    [
        "password",
        "otp",
        "token",
        "accesstoken",
        "access_token",
        "refreshtoken",
        "refresh_token",
        "authorization",
        "jwt",
        "apikey",
        "apisecret",
        "secret",
        "creditcard",
        "cardnumber",
        "cvv",
        "pan",
        "aadhaar",
        "ssn",
    ];

    public bool IsEnabled(LogEvent logEvent)
    {
        RedactSensitiveProperties(logEvent);
        return true;
    }

    private static void RedactSensitiveProperties(LogEvent logEvent)
    {
        foreach (var key in logEvent.Properties.Keys.ToList())
        {
            if (IsSensitiveName(key))
                logEvent.AddOrUpdateProperty(new LogEventProperty(key, new ScalarValue("[REDACTED]")));
        }
    }

    internal static string RedactMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        var result = message;

        result = BearerTokenPattern().Replace(result, "$1[REDACTED]");
        result = JwtPattern().Replace(result, "[REDACTED]");
        result = KeyValueSensitivePattern().Replace(result, "$1[REDACTED]");

        return result;
    }

    private static bool IsSensitiveName(string name)
    {
        var normalized = name.Replace("_", "", StringComparison.Ordinal)
            .Replace("-", "", StringComparison.Ordinal)
            .ToLowerInvariant();

        return SensitivePropertyNames.Any(s => normalized.Contains(s, StringComparison.Ordinal));
    }

    [GeneratedRegex(@"(?i)(authorization\s*[:=]\s*bearer\s+)[^\s,;""']+", RegexOptions.Compiled)]
    private static partial Regex BearerTokenPattern();

    [GeneratedRegex(@"\beyJ[A-Za-z0-9_-]*\.[A-Za-z0-9_-]*\.[A-Za-z0-9_-]*\b", RegexOptions.Compiled)]
    private static partial Regex JwtPattern();

    [GeneratedRegex(
        @"(?i)\b(password|otp|token|access[_-]?token|refresh[_-]?token|api[_-]?key|api[_-]?secret|secret)\b\s*[:=]\s*(""[^""]*""|'[^']*'|\S+)",
        RegexOptions.Compiled)]
    private static partial Regex KeyValueSensitivePattern();
}
