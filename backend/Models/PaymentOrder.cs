using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Models;

[Table("PaymentOrders")]
public sealed class PaymentOrder
{
    [Key]
    public Guid PaymentOrderId { get; set; }

    public Guid UserId { get; set; }

    public int PlanId { get; set; }

    [MaxLength(40)]
    public string PlanCode { get; set; } = "";

    public long BaseAmountPaise { get; set; }
    public long GstPaise { get; set; }
    public long TotalAmountPaise { get; set; }

    [MaxLength(3)]
    public string Currency { get; set; } = "INR";

    [MaxLength(20)]
    public string Provider { get; set; } = "Razorpay";

    [MaxLength(64)]
    public string? RazorpayOrderId { get; set; }

    [MaxLength(64)]
    public string? Receipt { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Created";

    [MaxLength(500)]
    public string? FailureReason { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
