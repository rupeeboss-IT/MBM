using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Models;

[Table("Vendors")]
public sealed class Vendor
{
    [Key]
    public Guid VendorId { get; set; }

    [MaxLength(200)]
    public string ServiceName { get; set; } = "";

    [MaxLength(200)]
    public string CompanyName { get; set; } = "";

    [MaxLength(160)]
    public string ContactPersonName { get; set; } = "";

    [MaxLength(10)]
    public string Mobile { get; set; } = "";

    [MaxLength(10)]
    public string? AlternateMobile { get; set; }

    [MaxLength(508)]
    public string Email { get; set; } = "";

    [MaxLength(508)]
    public string? AlternateEmail { get; set; }

    [MaxLength(500)]
    public string? Website { get; set; }

    [MaxLength(1000)]
    public string? Address { get; set; }

    [MaxLength(120)]
    public string? City { get; set; }

    [MaxLength(120)]
    public string? State { get; set; }

    [MaxLength(120)]
    public string? Country { get; set; }

    [MaxLength(10)]
    public string? Pincode { get; set; }

    [MaxLength(2000)]
    public string? Remarks { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public Guid? DeletedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Guid? CreatedByUserId { get; set; }
}
