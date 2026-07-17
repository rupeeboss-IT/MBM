using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Models;

[Table("BlogBadges", Schema = "dbo")]
public sealed class BlogBadge
{
    [Key]
    public int BlogBadgeId { get; set; }

    [Required, MaxLength(50)]
    public string Slug { get; set; } = "";

    [Required, MaxLength(100)]
    public string Label { get; set; } = "";

    [Required, MaxLength(100)]
    public string BadgeText { get; set; } = "";

    [Required, MaxLength(100)]
    public string BadgeClass { get; set; } = "";

    [Required, MaxLength(50)]
    public string CardIcon { get; set; } = "";

    [Required, MaxLength(100)]
    public string CardClass { get; set; } = "";

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
