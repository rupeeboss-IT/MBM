using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Models;

[Table("BlogCategories", Schema = "dbo")]
public sealed class BlogCategory
{
    [Key]
    public int BlogCategoryId { get; set; }

    [Required, MaxLength(50)]
    public string Slug { get; set; } = "";

    [Required, MaxLength(100)]
    public string Label { get; set; } = "";

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public bool ShowInFilter { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
