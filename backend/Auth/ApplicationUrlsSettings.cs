namespace RB_Website_API.Auth;

/// <summary>Public site and API origins per environment (ApplicationUrls in appsettings).</summary>
public sealed class ApplicationUrlsSettings
{
    public const string SectionName = "ApplicationUrls";

    /// <summary>Public frontend origin, no trailing slash (e.g. http://localhost:4200).</summary>
    public string Frontend { get; set; } = "";

    /// <summary>API origin, no trailing slash. Same as Frontend when SPA is hosted in wwwroot.</summary>
    public string Api { get; set; } = "";

    /// <summary>Browser origins for CORS (local dev: http://localhost:4200). Empty when same-origin.</summary>
    public string[] CorsOrigins { get; set; } = [];

    public string FrontendBase => Frontend.TrimEnd('/');

    public string ApiBase => Api.TrimEnd('/');

    public string ProfileReportsUrl =>
        string.IsNullOrWhiteSpace(FrontendBase) ? "" : $"{FrontendBase}/profile#reports";

    public string MembershipUrl =>
        string.IsNullOrWhiteSpace(FrontendBase) ? "" : $"{FrontendBase}/membership";

    public string MyPlanUrl =>
        string.IsNullOrWhiteSpace(FrontendBase) ? "" : $"{FrontendBase}/my-plan";

    public string ProfileUrl =>
        string.IsNullOrWhiteSpace(FrontendBase) ? "" : $"{FrontendBase}/profile";

    public string PaymentWebhookUrl =>
        string.IsNullOrWhiteSpace(ApiBase) ? "" : $"{ApiBase}/api/payment/razorpay/webhook";
}
