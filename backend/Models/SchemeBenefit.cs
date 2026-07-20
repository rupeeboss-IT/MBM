using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Models;

[Table("SchemeBenefits", Schema = "dbo")]
public sealed class SchemeBenefit
{
    [Key]
    public int SchemeBenefitId { get; set; }

    public int SchemeId { get; set; }

    [Required, MaxLength(1000)]
    public string Text { get; set; } = "";

    public int SortOrder { get; set; }

    [ForeignKey(nameof(SchemeId))]
    public Scheme? Scheme { get; set; }
}
