namespace RB_Website_API.Auth;

public static class PlanEmojiDefaults
{
    private static readonly Dictionary<string, string> ByCode = new(StringComparer.OrdinalIgnoreCase)
    {
        ["basic"] = "\U0001F331",
        ["premium"] = "\U0001F3C6",
        ["pro"] = "\U0001F48E",
        ["standard"] = "\U0001F310",
    };

    public static string Get(string? planCode) =>
        ByCode.TryGetValue(planCode?.Trim() ?? "", out var emoji) ? emoji : "\U0001F331";

    /// <summary>Fixes mojibake from sqlcmd seeds run without UTF-8 (e.g. ðŸŒ± instead of 🌱).</summary>
    public static string Normalize(string? planCode, string? iconEmoji)
    {
        var value = iconEmoji?.Trim() ?? "";
        if (string.IsNullOrEmpty(value) || LooksCorrupted(value))
            return Get(planCode);

        return value;
    }

    private static bool LooksCorrupted(string value) =>
        value.Contains('\u00F0') // ð
        || value.Contains('\u0152') // Œ
        || value.Contains('\u0178') // Ÿ
        || value.Contains('?') && value.Length <= 6;
}
