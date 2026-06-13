namespace RB_Website_API.Auth;

public sealed class SdrReportSettings
{
    public const string SectionName = "SdrReportSettings";

    public string StorageRoot { get; set; } = "reports/sdr";

    public int ValidityDays { get; set; } = SdrReportCatalog.ValidityDays;

    public SaarthiApiSettings Saarthi { get; set; } = new();
}
