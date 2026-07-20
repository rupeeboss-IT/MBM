using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Models;

[Table("Schemes", Schema = "dbo")]
public sealed class Scheme
{
    [Key]
    public int SchemeId { get; set; }

    [Required, MaxLength(200)]
    public string Slug { get; set; } = "";

    [Required, MaxLength(300)]
    public string Name { get; set; } = "";

    [MaxLength(200)]
    public string Crumb { get; set; } = "";

    [MaxLength(500)]
    public string Tagline { get; set; } = "";

    [MaxLength(2000)]
    public string ShortDescription { get; set; } = "";

    /// <summary>Rich HTML for the scheme detail main body.</summary>
    public string ContentHtml { get; set; } = "";

    [Required, MaxLength(50)]
    public string CategorySlug { get; set; } = "";

    [MaxLength(100)]
    public string PrimaryBadgeText { get; set; } = "";

    [MaxLength(50)]
    public string PrimaryBadgeClass { get; set; } = "badge-green";

    [MaxLength(100)]
    public string SecondaryBadgeText { get; set; } = "";

    [MaxLength(50)]
    public string SecondaryBadgeClass { get; set; } = "badge-orange";

    /// <summary>Optional shorter title for the home page card. Falls back to Crumb.</summary>
    [MaxLength(200)]
    public string HomeTitle { get; set; } = "";

    /// <summary>Optional badge label for the home page card. Falls back to category name.</summary>
    [MaxLength(100)]
    public string HomeBadgeText { get; set; } = "";

    [MaxLength(50)]
    public string HomeBadgeClass { get; set; } = "";

    /// <summary>Paragraph copy for the homepage scheme card. Separate from ShortDescription (listing page).</summary>
    [MaxLength(2000)]
    public string HomeDescription { get; set; } = "";

    [MaxLength(500)]
    public string? SeoTitle { get; set; }

    [MaxLength(1000)]
    public string? MetaDescription { get; set; }

    public bool IsPublished { get; set; }

    /// <summary>When true, eligible for the home page schemes grid.</summary>
    public bool IsFeatured { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public Guid? CreatedByUserId { get; set; }

    public ICollection<SchemeBenefit> Benefits { get; set; } = new List<SchemeBenefit>();

    public ICollection<SchemeCardHighlight> CardHighlights { get; set; } = new List<SchemeCardHighlight>();
}
