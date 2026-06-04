namespace RB_Website_API.Auth;

/// <summary>
/// Contact form content and addresses. SMTP delivery uses <see cref="EmailSettings"/>
/// unless <see cref="SmtpHost"/> / <see cref="SmtpUsername"/> are set below.
/// </summary>
public sealed class ContactSettings
{
    public const string SectionName = "ContactSettings";

    public string FromEmail { get; set; } = "support@msmebharatmanch.com";

    /// <summary>Inbox that receives new contact form enquiries.</summary>
    public string SupportNotifyEmail { get; set; } = "support@msmebharatmanch.com";

    public string FromDisplayName { get; set; } = "MSME Bharat Manch Support";
    public string SiteUrl { get; set; } = "http://localhost:4200";
    public string SupportPhone { get; set; } = "+91 90059 00921";

    /// <summary>Optional SMTP host for contact mail only. Empty = use EmailSettings:Host.</summary>
    public string? SmtpHost { get; set; }

    /// <summary>Optional SMTP port. Null = use EmailSettings:Port.</summary>
    public int? SmtpPort { get; set; }

    /// <summary>Optional SMTP login (e.g. support@msmebharatmanch.com). Empty = EmailSettings:Username.</summary>
    public string? SmtpUsername { get; set; }

    /// <summary>Optional SMTP password for SmtpUsername. Empty = EmailSettings:Password.</summary>
    public string? SmtpPassword { get; set; }
}
