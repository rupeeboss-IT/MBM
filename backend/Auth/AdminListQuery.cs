using System.Globalization;

namespace RB_Website_API.Auth;

/// <summary>Shared paging, search, date-range, and sort normalization for admin list APIs.</summary>
public static class AdminListQuery
{
    public const int DefaultPageSize = 100;
    public const int MaxPageSize = 100;
    public const int MaxExportSize = 10_000;

    public static (int Page, int PageSize) Normalize(int page, int pageSize, bool export)
    {
        if (export)
            return (1, MaxExportSize);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        return (page, pageSize);
    }

    public static string? NormalizeSearch(string? search)
    {
        var trimmed = (search ?? "").Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    public static DateTime? ParseDateFrom(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var text = value.Trim();
        if (DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date.Date;

        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out date))
            return date.Date;

        return null;
    }

    public static DateTime? ParseDateToExclusive(string? value)
    {
        var from = ParseDateFrom(value);
        return from?.AddDays(1);
    }

    public static (string SortField, bool SortAsc) NormalizeSort(string? sortBy, string? sortDir)
    {
        var key = (sortBy ?? "").Trim().ToLowerInvariant();
        var dir = (sortDir ?? "").Trim().ToLowerInvariant();

        switch (key)
        {
            case "latest":
                return ("created", false);
            case "oldest":
                return ("created", true);
            case "name_asc":
                return ("name", true);
            case "name_desc":
                return ("name", false);
            case "":
                return ("created", false);
        }

        var field = NormalizeSortField(key);
        var asc = dir == "asc";
        return (field, asc);
    }

    public static string? NormalizeStatus(string? status)
    {
        var value = (status ?? "").Trim().ToLowerInvariant();
        return value switch
        {
            "active" => "active",
            "inactive" => "inactive",
            _ => null,
        };
    }

    private static string NormalizeSortField(string field) =>
        field switch
        {
            "fullname" or "member" or "customer" => "name",
            "joined" or "date" or "upload" or "uploaded" => "created",
            "paidat" => "paid",
            "activefrom" => "started",
            "activeto" => "ends",
            "plancode" => "plan",
            "downloadcount" => "downloads",
            "lastdownloaddate" => "lastdownload",
            "originalfilename" or "filename" => "file",
            "isactive" => "status",
            _ => field,
        };
}
