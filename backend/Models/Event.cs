using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Models;

[Table("Events", Schema = "dbo")]
public sealed class Event
{
    [Key]
    public int EventId { get; set; }

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

    /// <summary>Rich HTML for "About the Event". Empty = section hidden on public page.</summary>
    public string AboutHtml { get; set; } = "";

    /// <summary>Optional rich HTML alternative for highlights. Prefer EventHighlights rows when present.</summary>
    public string HighlightsHtml { get; set; } = "";

    /// <summary>Optional rich HTML for "In Association With". Prefer EventPartners rows when present.</summary>
    public string AssociationHtml { get; set; } = "";

    [Required, MaxLength(50)]
    public string CategorySlug { get; set; } = "";

    [MaxLength(50)]
    public string? CitySlug { get; set; }

    [MaxLength(500)]
    public string? FeaturedImageUrl { get; set; }

    [MaxLength(500)]
    public string? BannerImageUrl { get; set; }

    [MaxLength(500)]
    public string? ThumbnailUrl { get; set; }

    /// <summary>Flexible date label shown on cards/detail (e.g. "25–27 January 2027", "Every Saturday", "Coming Soon").</summary>
    [MaxLength(300)]
    public string DateDisplayText { get; set; } = "";

    /// <summary>Flexible time label (e.g. "9 AM – 6 PM", "Time To Be Announced", "Starts 10:00 AM").</summary>
    [MaxLength(300)]
    public string TimeDisplayText { get; set; } = "";

    /// <summary>Optional structured start for upcoming/past/ongoing filters. Null when schedule is free-text only.</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>Optional structured end for multi-day / ongoing range filtering.</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>ISO date string for schema.org when known (e.g. 2026-06-27).</summary>
    [MaxLength(30)]
    public string? DateISO { get; set; }

    /// <summary>Online | Offline | Hybrid | empty.</summary>
    [MaxLength(20)]
    public string AttendanceMode { get; set; } = "";

    /// <summary>Combined location line for cards (e.g. venue + city). Falls back to venue fields when empty.</summary>
    [MaxLength(500)]
    public string LocationDisplayText { get; set; } = "";

    [MaxLength(300)]
    public string VenueName { get; set; } = "";

    [MaxLength(500)]
    public string VenueAddress { get; set; } = "";

    [MaxLength(200)]
    public string Landmark { get; set; } = "";

    [MaxLength(100)]
    public string CityName { get; set; } = "";

    [MaxLength(100)]
    public string State { get; set; } = "";

    [MaxLength(100)]
    public string Country { get; set; } = "India";

    [MaxLength(1000)]
    public string? MapsUrl { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    [MaxLength(100)]
    public string PriceDisplay { get; set; } = "";

    [MaxLength(1000)]
    public string RegNote { get; set; } = "";

    [MaxLength(500)]
    public string? SeoTitle { get; set; }

    [MaxLength(1000)]
    public string? MetaDescription { get; set; }

    public bool IsPublished { get; set; }

    public bool IsFeatured { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public Guid? CreatedByUserId { get; set; }

    public ICollection<EventHighlight> Highlights { get; set; } = new List<EventHighlight>();

    public ICollection<EventPartner> Partners { get; set; } = new List<EventPartner>();
}
