using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Models;

[Table("CustomerReports")]
public sealed class CustomerReport
{
    [Key]
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    [MaxLength(64)]
    public string MemberId { get; set; } = "";

    [MaxLength(260)]
    public string ReportFileName { get; set; } = "";

    [MaxLength(260)]
    public string OriginalFileName { get; set; } = "";

    [MaxLength(500)]
    public string FilePath { get; set; } = "";

    public long FileSize { get; set; }

    public DateTime UploadDate { get; set; }

    public Guid UploadedBy { get; set; }

    public Guid SubscriptionId { get; set; }

    public bool IsActive { get; set; } = true;

    public int DownloadCount { get; set; }

    public DateTime? LastDownloadDate { get; set; }

    [MaxLength(40)]
    public string ReportType { get; set; } = "General";

    public Guid? SchemeDiscoveryRequestId { get; set; }

    public DateTime? ExpiryDate { get; set; }
}
