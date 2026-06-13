using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Models;

[Table("ReportChangeRequests")]
public sealed class ReportChangeRequest
{
    [Key]
    public Guid Id { get; set; }

    public Guid ReportId { get; set; }

    [MaxLength(20)]
    public string RequestType { get; set; } = "";

    public Guid RequestedBy { get; set; }

    public DateTime RequestedOn { get; set; }

    public Guid? ApprovedBy { get; set; }

    public DateTime? ApprovedOn { get; set; }

    public Guid? RejectedBy { get; set; }

    public DateTime? RejectedOn { get; set; }

    [MaxLength(2000)]
    public string Reason { get; set; } = "";

    [MaxLength(2000)]
    public string? Remarks { get; set; }

    [MaxLength(500)]
    public string? PreviousReportPath { get; set; }

    [MaxLength(500)]
    public string? NewReportPath { get; set; }

    public string? PreviousValues { get; set; }

    public string? NewValues { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Pending";

    [MaxLength(260)]
    public string? PendingFileName { get; set; }

    [MaxLength(260)]
    public string? PendingOriginalFileName { get; set; }

    public long? PendingFileSize { get; set; }
}
