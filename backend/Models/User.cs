using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Models;

[Table("Users")]
public sealed class User
{
    [Key]
    public Guid UserId { get; set; }

    [MaxLength(40)]
    public string Role { get; set; } = "";

    [MaxLength(160)]
    public string FullName { get; set; } = "";

    [MaxLength(508)]
    public string Email { get; set; } = "";

    [MaxLength(10)]
    public string Phone { get; set; } = "";

    [MaxLength(240)]
    public string? CompanyName { get; set; }

    [MaxLength(256)]
    public byte[] PasswordHash { get; set; } = Array.Empty<byte>();

    [MaxLength(64)]
    public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();

    public DateTime? EmailVerifiedAt { get; set; }

    public DateTime? PhoneVerifiedAt { get; set; }

    public bool ConsentAccepted { get; set; }

    public DateTime? ConsentAcceptedAt { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

