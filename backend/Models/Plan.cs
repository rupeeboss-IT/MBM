using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Models;

[Table("Plans")]
public sealed class Plan
{
    [Key]
    public int PlanId { get; set; }

    [MaxLength(40)]
    public string Code { get; set; } = "";

    [MaxLength(120)]
    public string Name { get; set; } = "";

    [MaxLength(500)]
    public string? Description { get; set; }

    public long BaseAmountPaise { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal GstPercent { get; set; } = 18m;

    public long GstPaise { get; set; }

    public long TotalAmountPaise { get; set; }

    [MaxLength(3)]
    public string Currency { get; set; } = "INR";

    public int DurationDays { get; set; } = 365;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
