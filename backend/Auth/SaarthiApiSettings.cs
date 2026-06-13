namespace RB_Website_API.Auth;

public sealed class SaarthiApiSettings
{
    public const string SectionName = "SdrReportSettings:Saarthi";

    public string BaseUrl { get; set; } = "https://marketplace.emsme.com/uat/saarthi/v1";

    public string GetNicCodesPath { get; set; } = "get-nic-codes";

    public string GetSchemeListPath { get; set; } = "get-scheme-list";

    public string GetSchemeCountPath { get; set; } = "get-scheme-count";

    public string GeneratePath { get; set; } = "generate-sdr";

    public string ApiKey { get; set; } = "";

    public string ApiSecret { get; set; } = "";

    public int TimeoutSeconds { get; set; } = 120;
}
