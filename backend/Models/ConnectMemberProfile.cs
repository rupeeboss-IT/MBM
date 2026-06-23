using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Models;

[Table("ConnectMemberProfiles")]
public sealed class ConnectMemberProfile
{
    [Key]
    public Guid UserId { get; set; }

    [MaxLength(120)]
    public string? Designation { get; set; }

    [MaxLength(80)]
    public string? BusinessType { get; set; }

    [MaxLength(120)]
    public string? Sector { get; set; }

    [MaxLength(120)]
    public string? State { get; set; }

    [MaxLength(120)]
    public string? City { get; set; }

    [MaxLength(80)]
    public string? Turnover { get; set; }

    [MaxLength(40)]
    public string? Udyam { get; set; }

    [MaxLength(40)]
    public string? Employees { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? Website { get; set; }

    [MaxLength(10)]
    public string? Established { get; set; }

    [MaxLength(2000)]
    public string? SocialLinksJson { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
