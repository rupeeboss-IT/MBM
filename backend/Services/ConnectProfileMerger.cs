using RB_Website_API.Models;

namespace RB_Website_API.Services;

public static class ConnectProfileMerger
{
    public static string DisplayOrDash(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();

    public static string? MergeField(string? memberValue, string? adminValue) =>
        !string.IsNullOrWhiteSpace(memberValue) ? memberValue.Trim() : Normalize(adminValue);

    public static string MergeFieldDash(string? memberValue, string? adminValue) =>
        DisplayOrDash(MergeField(memberValue, adminValue));

    public static string BuildInitials(string? fullName, string? companyName)
    {
        var source = !string.IsNullOrWhiteSpace(companyName) ? companyName : fullName;
        if (string.IsNullOrWhiteSpace(source)) return "?";
        var parts = source.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
            return $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant();
        return parts[0].Length >= 2
            ? parts[0][..2].ToUpperInvariant()
            : parts[0].ToUpperInvariant();
    }

    public static IReadOnlyList<string> GetCustomerLockedFields(ConnectMemberProfile? member)
    {
        if (member is null) return Array.Empty<string>();
        var locked = new List<string>();
        if (HasValue(member.Designation)) locked.Add(nameof(ConnectMemberProfile.Designation));
        if (HasValue(member.BusinessType)) locked.Add(nameof(ConnectMemberProfile.BusinessType));
        if (HasValue(member.Sector)) locked.Add(nameof(ConnectMemberProfile.Sector));
        if (HasValue(member.State)) locked.Add(nameof(ConnectMemberProfile.State));
        if (HasValue(member.City)) locked.Add(nameof(ConnectMemberProfile.City));
        if (HasValue(member.Turnover)) locked.Add(nameof(ConnectMemberProfile.Turnover));
        if (HasValue(member.Udyam)) locked.Add(nameof(ConnectMemberProfile.Udyam));
        if (HasValue(member.Employees)) locked.Add(nameof(ConnectMemberProfile.Employees));
        if (HasValue(member.Description)) locked.Add(nameof(ConnectMemberProfile.Description));
        if (HasValue(member.Website)) locked.Add(nameof(ConnectMemberProfile.Website));
        if (HasValue(member.Established)) locked.Add(nameof(ConnectMemberProfile.Established));
        if (HasValue(member.SocialLinksJson)) locked.Add(nameof(ConnectMemberProfile.SocialLinksJson));
        return locked;
    }

    public static bool HasCustomerValue(ConnectMemberProfile? member, string fieldName) =>
        member is not null && fieldName switch
        {
            nameof(ConnectMemberProfile.Designation) => HasValue(member.Designation),
            nameof(ConnectMemberProfile.BusinessType) => HasValue(member.BusinessType),
            nameof(ConnectMemberProfile.Sector) => HasValue(member.Sector),
            nameof(ConnectMemberProfile.State) => HasValue(member.State),
            nameof(ConnectMemberProfile.City) => HasValue(member.City),
            nameof(ConnectMemberProfile.Turnover) => HasValue(member.Turnover),
            nameof(ConnectMemberProfile.Udyam) => HasValue(member.Udyam),
            nameof(ConnectMemberProfile.Employees) => HasValue(member.Employees),
            nameof(ConnectMemberProfile.Description) => HasValue(member.Description),
            nameof(ConnectMemberProfile.Website) => HasValue(member.Website),
            nameof(ConnectMemberProfile.Established) => HasValue(member.Established),
            nameof(ConnectMemberProfile.SocialLinksJson) => HasValue(member.SocialLinksJson),
            _ => false,
        };

    private static bool HasValue(string? v) => !string.IsNullOrWhiteSpace(v);

    private static string? Normalize(string? v) =>
        string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}
