using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Models;

[Table("UserPlans")]
public sealed class UserPlan
{
    [Key]
    public Guid UserPlanId { get; set; }

    public Guid UserId { get; set; }

    public int PlanId { get; set; }

    [MaxLength(40)]
    public string PlanCode { get; set; } = "";

    public Guid? PaymentOrderId { get; set; }

    public DateTime ActiveFrom { get; set; }

    public DateTime? ActiveTo { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Active";

    /// <summary>Member opted out of renewal; access continues until ActiveTo.</summary>
    public bool CancelAtPeriodEnd { get; set; }

    public DateTime? CancelledAt { get; set; }

    /// <summary>Phase 2: Razorpay auto-renewal enabled.</summary>
    public bool AutoRenewEnabled { get; set; }

    [MaxLength(64)]
    public string? RazorpaySubscriptionId { get; set; }

    [MaxLength(64)]
    public string? RazorpayCustomerId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
