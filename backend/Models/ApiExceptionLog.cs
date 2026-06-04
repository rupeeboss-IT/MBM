using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Models;

[Table("ApiExceptionLogs")]
public sealed class ApiExceptionLog
{
    [Key]
    public Guid LogId { get; set; }

    public DateTime CreatedAt { get; set; }

    [MaxLength(16)]
    public string HttpMethod { get; set; } = "";

    [MaxLength(500)]
    public string RequestPath { get; set; } = "";

    public int StatusCode { get; set; }

    public Guid? UserId { get; set; }

    [MaxLength(64)]
    public string? IpAddress { get; set; }

    [MaxLength(80)]
    public string? OperationKey { get; set; }

    [MaxLength(500)]
    public string UserMessage { get; set; } = "";

    [MaxLength(200)]
    public string ExceptionType { get; set; } = "";

    public string ExceptionMessage { get; set; } = "";

    public string? InnerExceptionMessage { get; set; }

    public string StackTrace { get; set; } = "";
}
