using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Models;

[Table("PlanFeatures", Schema = "dbo")]
public sealed class PlanFeature
{
    [Key]
    public int PlanFeatureId { get; set; }

    public int PlanId { get; set; }

    [MaxLength(1000)]
    public string Text { get; set; } = "";

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string? OfferingSlug { get; set; }

    public bool IsIncludesLine { get; set; }

    public int SortOrder { get; set; }

    public Plan Plan { get; set; } = null!;
}
