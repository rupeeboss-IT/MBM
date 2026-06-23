using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Models;

[Table("ConnectRequests")]
public sealed class ConnectRequest
{
    [Key]
    public Guid RequestId { get; set; }

    public Guid FromUserId { get; set; }

    public Guid ToUserId { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = ConnectRequestStatuses.Pending;

    [MaxLength(500)]
    public string? Message { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? RespondedAt { get; set; }
}

public static class ConnectRequestStatuses
{
    public const string Pending = "Pending";
    public const string Connected = "Connected";
    public const string Rejected = "Rejected";

    public static bool IsValid(string? status) =>
        status is Pending or Connected or Rejected;
}
