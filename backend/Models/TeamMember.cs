using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Models;

[Table("TeamMembers", Schema = "dbo")]
public sealed class TeamMember
{
    [Key]
    public int TeamMemberId { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = "";

    /// <summary>Role / bio shown under the name. May include simple HTML (e.g. line breaks).</summary>
    [MaxLength(2000)]
    public string DesignationHtml { get; set; } = "";

    [Required, MaxLength(500)]
    public string PhotoUrl { get; set; } = "";

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public Guid? CreatedByUserId { get; set; }
}
