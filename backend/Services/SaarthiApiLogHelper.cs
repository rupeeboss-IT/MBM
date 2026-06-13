using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace RB_Website_API.Services;

internal static class SaarthiApiLogHelper
{
    private const int MaxJsonLogChars = 8000;

    public static string MaskUdyam(string? udyam)
    {
        if (string.IsNullOrWhiteSpace(udyam) || udyam.Length < 8) return "****";
        return $"{udyam[..7]}****";
    }

    public static string BuildRequestPayloadLog(string udyamNumber) =>
        JsonSerializer.Serialize(new { udyamNumber = MaskUdyam(udyamNumber) });

    public static string BuildAuthLogSummary(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) return "ApiKey=missing";
        var trimmed = apiKey.Trim();
        var suffix = trimmed.Length >= 4 ? trimmed[^4..] : "****";
        return $"ApiKeySuffix={suffix} ApiSecret=***";
    }

    public static string FormatResponseBodyForLog(string contentType, byte[] bodyBytes, string bodyText)
    {
        if (bodyBytes.Length == 0) return "(empty)";

        if (LooksLikePdf(bodyBytes) || contentType.Contains("pdf", StringComparison.OrdinalIgnoreCase))
        {
            return JsonSerializer.Serialize(new
            {
                type = "application/pdf",
                bytes = bodyBytes.Length,
                note = "PDF binary omitted from log",
            });
        }

        if (IsLikelyBinary(bodyBytes))
        {
            return JsonSerializer.Serialize(new
            {
                type = contentType,
                bytes = bodyBytes.Length,
                note = "Binary body omitted from log",
            });
        }

        var text = string.IsNullOrWhiteSpace(bodyText)
            ? Encoding.UTF8.GetString(bodyBytes)
            : bodyText;

        return Truncate(CompactJsonIfPossible(text), MaxJsonLogChars);
    }

    private static bool LooksLikePdf(byte[] bytes) =>
        bytes.Length >= 4 && bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46;

    private static bool IsLikelyBinary(byte[] bytes)
    {
        var sample = Math.Min(bytes.Length, 512);
        var nonPrintable = 0;
        for (var i = 0; i < sample; i++)
        {
            var b = bytes[i];
            if (b is < 9 or > 126) nonPrintable++;
        }
        return sample > 0 && nonPrintable > sample / 3;
    }

    private static string CompactJsonIfPossible(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        try
        {
            using var doc = JsonDocument.Parse(text);
            return JsonSerializer.Serialize(doc.RootElement);
        }
        catch (JsonException)
        {
            return text;
        }
    }

    public static string Truncate(string? value, int max) =>
        string.IsNullOrEmpty(value) ? "" : value.Length <= max ? value : value[..max] + "…(truncated)";

    public static long ElapsedMs(Stopwatch sw) => sw.ElapsedMilliseconds;
}
