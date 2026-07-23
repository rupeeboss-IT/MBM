using System.Text;

namespace RB_Website_API.Auth;

/// <summary>
/// Repairs text that was UTF-8 originally but stored/read as Windows-1252
/// (e.g. ₹ shown as â‚¹ on plan feature lines).
/// </summary>
public static class Utf8Mojibake
{
    /// <summary>₹ (U+20B9) as UTF-8 mis-decoded via Windows-1252.</summary>
    private const string RupeeMojibake = "\u00E2\u201A\u00B9"; // â‚¹

    public static string Fix(string? value)
    {
        if (string.IsNullOrEmpty(value)) return value ?? "";

        var s = value;
        if (s.Contains(RupeeMojibake, StringComparison.Ordinal))
            s = s.Replace(RupeeMojibake, "₹", StringComparison.Ordinal);

        // Broader repair when the whole field looks like UTF-8-as-1252.
        if (LooksLikeMojibake(s))
        {
            try
            {
                var bytes = Encoding.GetEncoding(1252).GetBytes(s);
                var repaired = Encoding.UTF8.GetString(bytes);
                if (!string.IsNullOrEmpty(repaired) && !repaired.Contains('\uFFFD'))
                    s = repaired;
            }
            catch (EncoderFallbackException)
            {
                // Keep the partially fixed string.
            }
        }

        return s;
    }

    private static bool LooksLikeMojibake(string s) =>
        s.Contains('\u00E2') // â — common lead byte for UTF-8 multi-byte sequences
        || s.Contains('\u00C3') // Ã — another common UTF-8/1252 lead
        || s.Contains('\u00C2'); // Â
}
