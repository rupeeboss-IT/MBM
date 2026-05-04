namespace RB_Website_API.Auth;

public sealed class EmailSettings
{
    public const string SectionName = "EmailSettings";

    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string From { get; set; } = "";
    public string? FromDisplayName { get; set; }
}
