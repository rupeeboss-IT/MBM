using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Models;

[Table("CookieConsentLogs")]
public sealed class CookieConsentLog
{
    [Key]
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    [MaxLength(100)]
    public string? SessionToken { get; set; }

    [MaxLength(64)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    public DateTime AcceptedAt { get; set; }
}
