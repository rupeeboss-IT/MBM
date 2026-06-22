using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Referrals.Models;

/// <summary>
/// DB2.lead_data (RBMAIN) — column names match production schema exactly.
/// Only fields used by MBM membership payment flow are mapped.
/// </summary>
[Table("lead_data")]
public sealed class LeadData
{
    [Key]
    [Column("Lead_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Lead_id { get; set; }

    [Column("mobile")]
    [MaxLength(50)]
    public string? mobile { get; set; }

    [Column("name")]
    [MaxLength(100)]
    public string? name { get; set; }

    [Column("email")]
    [MaxLength(300)]
    public string? email { get; set; }

    [Column("productid")]
    public int productid { get; set; }

    [Column("Pincode")]
    [MaxLength(10)]
    public string? Pincode { get; set; }

    /// <summary>Employment type id in CRM (int). Use 0 when unknown.</summary>
    [Column("profession")]
    public int profession { get; set; }

    [Column("source_id")]
    public int source_id { get; set; }

    [Column("lead_source")]
    [MaxLength(500)]
    public string? lead_source { get; set; }

    [Column("lead_type")]
    [MaxLength(50)]
    public string? lead_type { get; set; }

    [Column("campaignName")]
    [MaxLength(200)]
    public string? campaignName { get; set; }

    [Column("sysdate")]
    public DateTime sysdate { get; set; }

    [Column("emp_code")]
    [MaxLength(20)]
    public string? emp_code { get; set; }

    [Column("broker_id")]
    public int? broker_id { get; set; }

    [Column("Lead_Status_id")]
    public int Lead_Status_id { get; set; }

    [Column("lead_date")]
    public DateTime lead_date { get; set; }

    [Column("Created_Datetime")]
    public DateTime Created_Datetime { get; set; }

    [Column("CompanyName")]
    [MaxLength(550)]
    public string? CompanyName { get; set; }

    [Column("remark")]
    [MaxLength(4000)]
    public string? remark { get; set; }
}
