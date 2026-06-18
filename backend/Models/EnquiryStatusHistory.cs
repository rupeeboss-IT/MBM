using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Models;

[Table("EnquiryStatusHistory", Schema = "dbo")]
public sealed class EnquiryStatusHistory
{
    [Key]
    public Guid Id { get; set; }

    public int ContactSubmissionId { get; set; }

    [MaxLength(20)]
    public string? OldStatus { get; set; }

    [MaxLength(20)]
    public string NewStatus { get; set; } = "";

    public Guid ChangedByUserId { get; set; }

    public DateTime ChangedOn { get; set; }

    [MaxLength(800)]
    public string? Remarks { get; set; }
}
