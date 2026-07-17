using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Models;

[Table("Blogs", Schema = "dbo")]
public sealed class Blog
{
    [Key]
    public int BlogId { get; set; }

    [Required, MaxLength(200)]
    public string Slug { get; set; } = "";

    [Required, MaxLength(500)]
    public string Title { get; set; } = "";

    [MaxLength(200)]
    public string Crumb { get; set; } = "";

    [MaxLength(500)]
    public string Meta { get; set; } = "";

    public string Content { get; set; } = "";

    [MaxLength(50)]
    public string Category { get; set; } = "blog";

    [MaxLength(50)]
    public string BadgeSlug { get; set; } = "msme-green";

    [MaxLength(100)]
    public string DateLabel { get; set; } = "";

    [MaxLength(2000)]
    public string Summary { get; set; } = "";

    [MaxLength(100)]
    public string BadgeText { get; set; } = "";

    [MaxLength(100)]
    public string BadgeClass { get; set; } = "";

    [MaxLength(50)]
    public string CardIcon { get; set; } = "";

    [MaxLength(100)]
    public string CardClass { get; set; } = "";

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    [MaxLength(500)]
    public string? SeoTitle { get; set; }

    [MaxLength(1000)]
    public string? MetaDescription { get; set; }

    public bool IsPublished { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public Guid? CreatedByUserId { get; set; }
}
