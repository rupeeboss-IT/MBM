using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Models;

[Table("ConnectContactUnlocks")]
public sealed class ConnectContactUnlock
{
    public Guid ViewerUserId { get; set; }

    public Guid TargetUserId { get; set; }

    public DateTime UnlockedAt { get; set; }
}
