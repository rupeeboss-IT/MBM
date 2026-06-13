using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Models;

[Table("UserStatusHistory")]
public sealed class UserStatusHistory
{
    [Key]
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    [MaxLength(40)]
    public string UserType { get; set; } = "";

    [MaxLength(40)]
    public string ActionType { get; set; } = "";

    [MaxLength(20)]
    public string? OldStatus { get; set; }

    [MaxLength(20)]
    public string? NewStatus { get; set; }

    [MaxLength(800)]
    public string? Remarks { get; set; }

    public Guid PerformedByUserId { get; set; }

    public DateTime PerformedOn { get; set; }
}
