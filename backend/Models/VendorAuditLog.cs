using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Models;

[Table("VendorAuditLogs")]
public sealed class VendorAuditLog
{
    [Key]
    public Guid Id { get; set; }

    public Guid VendorId { get; set; }

    [MaxLength(40)]
    public string Action { get; set; } = "";

    public Guid PerformedByUserId { get; set; }

    public DateTime PerformedOn { get; set; }

    public string? PreviousValues { get; set; }

    public string? NewValues { get; set; }

    [MaxLength(800)]
    public string? Remarks { get; set; }
}
