using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Models;

[Table("SchemeCategories", Schema = "dbo")]
public sealed class SchemeCategory
{
    [Key]
    public int SchemeCategoryId { get; set; }

    [Required, MaxLength(50)]
    public string Slug { get; set; } = "";

    [Required, MaxLength(100)]
    public string Name { get; set; } = "";

    [MaxLength(500)]
    public string? ShortDescription { get; set; }

    [MaxLength(500)]
    public string? IconUrl { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public bool ShowInFilter { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
