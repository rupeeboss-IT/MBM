namespace RB_Website_API.Auth;

public sealed class ZohoFlowSettings
{
    public const string SectionName = "ZohoFlow";

    /// <summary>
    /// Full incoming webhook URL. Keep secrets (zapikey) only in configuration.
    /// </summary>
    public string WebhookUrl { get; set; } = "";

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 15;
}

