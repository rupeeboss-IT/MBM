using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Models;

[Table("CreditRepair_Lead", Schema = "dbo")]
public sealed class CreditRepairLead
{
    [Key]
    public long Id { get; set; }

    [Required]
    [MaxLength(160)]
    public string FullName { get; set; } = "";

    [Required]
    [MaxLength(10)]
    public string Phone { get; set; } = "";

    [MaxLength(508)]
    public string? Email { get; set; }

    [Required]
    public bool ConsentAccepted { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    [MaxLength(80)]
    public string Source { get; set; } = "mbmwebsite";

    [MaxLength(200)]
    public string CampaignName { get; set; } = "creditrepair";

    /// <summary>RBMAIN.dbo.lead_data.Lead_id after CRM push.</summary>
    [Column("lead_id")]
    public int? LeadId { get; set; }
}

