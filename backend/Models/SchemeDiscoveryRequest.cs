using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Models;

[Table("SchemeDiscoveryRequests")]
public sealed class SchemeDiscoveryRequest
{
    [Key]
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    [MaxLength(64)]
    public string MemberId { get; set; } = "";

    public Guid UserPlanId { get; set; }

    [MaxLength(32)]
    public string UdyamNumber { get; set; } = "";

    public Guid? PaymentId { get; set; }

    [MaxLength(20)]
    public string EntitlementType { get; set; } = "";

    [MaxLength(20)]
    public string Status { get; set; } = "";

    [MaxLength(128)]
    public string? ExternalReference { get; set; }

    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    public Guid? CustomerReportId { get; set; }

    public DateTime RequestedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
