namespace RB_Website_API.Auth;

public sealed class RazorpaySettings
{
    public const string SectionName = "RazorpaySettings";

    public string KeyId { get; set; } = "";
    public string KeySecret { get; set; } = "";
    public string? WebhookSecret { get; set; }

    public string BaseUrl { get; set; } = "https://api.razorpay.com/v1";
}
