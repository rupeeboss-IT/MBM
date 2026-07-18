using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Models;

[Table("EventHighlights", Schema = "dbo")]
public sealed class EventHighlight
{
    [Key]
    public int EventHighlightId { get; set; }

    public int EventId { get; set; }

    [Required, MaxLength(1000)]
    public string Text { get; set; } = "";

    public int SortOrder { get; set; }

    [ForeignKey(nameof(EventId))]
    public Event? Event { get; set; }
}
