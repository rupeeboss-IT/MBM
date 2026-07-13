namespace RB_Website_API.Auth;

public sealed class RecaptchaSettings
{
    public const string SectionName = "Recaptcha";

    /// <summary>Secret key from Google reCAPTCHA admin console (keep server-side only).</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Minimum score to accept (0.0 = bot, 1.0 = human). Default 0.5.</summary>
    public double MinimumScore { get; set; } = 0.5;

    /// <summary>When false, reCAPTCHA verification is skipped (useful for local dev without a key).</summary>
    public bool Enabled { get; set; } = true;
}
