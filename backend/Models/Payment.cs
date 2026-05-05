using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Models;

[Table("Payments")]
public sealed class Payment
{
    [Key]
    public Guid PaymentId { get; set; }

    public Guid PaymentOrderId { get; set; }

    [MaxLength(64)]
    public string RazorpayOrderId { get; set; } = "";

    [MaxLength(64)]
    public string RazorpayPaymentId { get; set; } = "";

    [MaxLength(256)]
    public string? RazorpaySignature { get; set; }

    [MaxLength(40)]
    public string? Method { get; set; }

    public long AmountPaise { get; set; }

    [MaxLength(3)]
    public string Currency { get; set; } = "INR";

    [MaxLength(20)]
    public string Status { get; set; } = "Captured";

    public string? RawPayload { get; set; }

    public DateTime PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
