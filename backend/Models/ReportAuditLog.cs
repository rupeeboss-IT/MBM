using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Models;

[Table("ReportAuditLogs")]
public sealed class ReportAuditLog
{
    [Key]
    public Guid AuditId { get; set; }

    public Guid? UserId { get; set; }

    [MaxLength(40)]
    public string Action { get; set; } = "";

    public Guid? ReportId { get; set; }

    public Guid? CustomerId { get; set; }

    public DateTime CreatedAt { get; set; }

    [MaxLength(64)]
    public string? IpAddress { get; set; }
}
