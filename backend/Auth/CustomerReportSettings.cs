namespace RB_Website_API.Auth;

public sealed class CustomerReportSettings
{
    public const string SectionName = "CustomerReportSettings";

    /// <summary>Maximum upload size in megabytes.</summary>
    public int MaxUploadSizeMb { get; set; } = 50;

    /// <summary>Relative path under wwwroot for stored report ZIP files.</summary>
    public string UploadRoot { get; set; } = "uploads/reports";

    /// <summary>Frontend profile URL used in notification emails.</summary>
    public string ProfileReportsUrl { get; set; } = "http://localhost:4200/profile#reports";
}
