using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Models;

/// <summary>City master for event filters and venue city dropdowns (future-ready).</summary>
[Table("EventCities", Schema = "dbo")]
public sealed class EventCity
{
    [Key]
    public int EventCityId { get; set; }

    [Required, MaxLength(50)]
    public string Slug { get; set; } = "";

    [Required, MaxLength(100)]
    public string Name { get; set; } = "";

    [MaxLength(50)]
    public string? BadgeClass { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
