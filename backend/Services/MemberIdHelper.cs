using System.Text.RegularExpressions;
using RB_Website_API.Models;

namespace RB_Website_API.Services;

/// <summary>
/// Member ID formatting and parsing (new MBM format + legacy GUID hex for existing customers).
/// </summary>
public static partial class MemberIdHelper
{
    public const string Prefix = MemberIdGeneratorService.Prefix;

    /// <summary>Legacy display format for accounts created before MBM member IDs.</summary>
    public static string FormatLegacy(Guid userId) => userId.ToString("N").ToUpperInvariant();

    public static string GetDisplayMemberId(User user)
    {
        if (!string.IsNullOrWhiteSpace(user.MemberId))
            return user.MemberId.Trim().ToUpperInvariant();
        return FormatLegacy(user.UserId);
    }

    public static bool IsNewFormat(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        return NewFormatPattern().IsMatch(value.Trim());
    }

    /// <summary>Maps MBM260001 to canonical uppercase form, or null if invalid.</summary>
    public static string? NormalizeNewFormat(string? value)
    {
        if (!IsNewFormat(value)) return null;
        return value!.Trim().ToUpperInvariant();
    }

    [Obsolete("Use GetDisplayMemberId(User) or IMemberIdGeneratorService.GetDisplayMemberId.")]
    public static string Format(Guid userId) => FormatLegacy(userId);

    public static bool TryParse(string? value, out Guid userId) => TryParseLegacyGuid(value, out userId);

    public static bool TryParseLegacyGuid(string? value, out Guid userId)
    {
        userId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var trimmed = value.Trim();
        if (Guid.TryParse(trimmed, out userId) && userId != Guid.Empty)
            return true;

        var compact = trimmed.Replace("-", "", StringComparison.Ordinal);
        if (compact.Length == 32 && Guid.TryParseExact(compact, "N", out userId) && userId != Guid.Empty)
            return true;

        return false;
    }

    [GeneratedRegex(@"^MBM\d{6}$", RegexOptions.IgnoreCase)]
    private static partial Regex NewFormatPattern();
}
