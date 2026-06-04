using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Models;

/// <summary>
/// DB1 outbox for pushing leads to DB2 without impacting payment activation.
/// </summary>
[Table("ReferralLeadOutbox")]
public sealed class ReferralLeadOutbox
{
    [Key]
    public Guid ReferralLeadOutboxId { get; set; }

    public Guid PaymentOrderId { get; set; }
    public Guid UserId { get; set; }

    [MaxLength(40)]
    public string PlanCode { get; set; } = "";

    [MaxLength(50)]
    public string? ReferralCode { get; set; }

    public long AmountPaise { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, Processing, Done, Failed

    public int Attempts { get; set; }

    [MaxLength(1000)]
    public string? LastError { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? NextAttemptAt { get; set; }
}

