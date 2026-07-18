using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Models;

[Table("EventPartners", Schema = "dbo")]
public sealed class EventPartner
{
    [Key]
    public int EventPartnerId { get; set; }

    public int EventId { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = "";

    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    [MaxLength(500)]
    public string? WebsiteUrl { get; set; }

    public int SortOrder { get; set; }

    [ForeignKey(nameof(EventId))]
    public Event? Event { get; set; }
}
