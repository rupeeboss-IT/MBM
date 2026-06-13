using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Referrals.Models;

/// <summary>
/// RBMAIN.broker_master (existing table).
/// </summary>
[Table("broker_master")]
public sealed class BrokerMaster
{
    [Key]
    [Column("Broker_id")]
    public int Broker_id { get; set; }

    [Column("Broker_Name")]
    [MaxLength(200)]
    public string Broker_Name { get; set; } = "";

    [Column("PAN_No")]
    [MaxLength(20)]
    public string PAN_No { get; set; } = "";

    [Column("Emp_Code")]
    [MaxLength(50)]
    public string Emp_Code { get; set; } = "";

    /// <summary>1 = active, 0 = inactive.</summary>
    [Column("Is_Active")]
    public int Is_Active { get; set; }
}
