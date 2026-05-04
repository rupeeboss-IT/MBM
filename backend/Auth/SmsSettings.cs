namespace RB_Website_API.Auth;

public sealed class SmsSettings
{
    public const string SectionName = "SmsSettings";

    /// <summary>Full URL to SendMsg.aspx (no query string).</summary>
    public string SendUrl { get; set; } = "";

    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string SenderId { get; set; } = "";
}
