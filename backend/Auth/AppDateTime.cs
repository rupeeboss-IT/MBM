using System.Globalization;

namespace RB_Website_API.Auth;

/// <summary>
/// Application clock and formatting — always server local time (GETDATE() / DateTime.Now).
/// Do not convert to/from UTC; SQL Server datetime2 values are stored as local clock time.
/// </summary>
public static class AppDateTime
{
    public static DateTime Now => DateTime.Now;

    public static string FormatDate(DateTime? value)
    {
        if (value is null) return "—";
        return Normalize(value.Value).ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
    }

    public static string FormatDateTime(DateTime? value)
    {
        if (value is null) return "—";
        return Normalize(value.Value).ToString("dd MMM yyyy, hh:mm tt", CultureInfo.InvariantCulture);
    }

    /// <summary>Normalize DB/API values to local clock time without timezone shifting.</summary>
    public static DateTime Normalize(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value.ToLocalTime(),
            DateTimeKind.Local => value,
            _ => value,
        };
    }
}
