using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Models;

[Table("UserStatusAudit")]
public sealed class UserStatusAudit
{
    [Key]
    public Guid AuditId { get; set; }

    public Guid TargetUserId { get; set; }

    public Guid ActorUserId { get; set; }

    public bool NewIsActive { get; set; }

    [MaxLength(800)]
    public string Reason { get; set; } = "";

    public DateTime CreatedAt { get; set; }
}

