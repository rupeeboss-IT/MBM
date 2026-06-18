using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Models;

[Table("VendorPlanMappings")]
public sealed class VendorPlanMapping
{
    [Key]
    public Guid Id { get; set; }

    public Guid VendorId { get; set; }

    public int PlanId { get; set; }

    public DateTime AssignedAt { get; set; }

    public Guid AssignedByUserId { get; set; }
}
