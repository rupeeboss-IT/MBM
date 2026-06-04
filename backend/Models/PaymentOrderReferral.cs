using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Models;

/// <summary>
/// DB1 table to store buyer-entered referral codes per payment order.
/// Kept separate from PaymentOrders to avoid altering existing schema/logic.
/// </summary>
[Table("PaymentOrderReferrals")]
public sealed class PaymentOrderReferral
{
    [Key]
    public Guid PaymentOrderId { get; set; }

    [MaxLength(50)]
    public string? ReferralCode { get; set; }

    /// <summary>Set when lead_data row was inserted (idempotency for verify + webhook).</summary>
    public DateTime? LeadPushedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

