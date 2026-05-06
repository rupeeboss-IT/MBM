namespace RB_Website_API.Auth;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// Symmetric signing key. Keep it secret.
    /// </summary>
    public string Key { get; set; } = "";

    public string? Issuer { get; set; }

    public string? Audience { get; set; }

    public int ExpMinutes { get; set; } = 60 * 24; // 24h
}

