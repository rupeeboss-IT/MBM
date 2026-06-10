using RB_Website_API.Models;
using RB_Website_API.Services.IRepository;

namespace RB_Website_API.Services;

public sealed class ReportAuditService : IReportAuditService
{
    public const string ActionUpload = "ReportUpload";
    public const string ActionDownload = "ReportDownload";
    public const string ActionEmailSent = "EmailSent";

    private readonly IReportAuditRepository _audit;

    public ReportAuditService(IReportAuditRepository audit) => _audit = audit;

    public Task LogAsync(
        string action,
        Guid? actorUserId,
        Guid? reportId,
        Guid? customerId,
        string? ipAddress,
        CancellationToken ct)
    {
        var entry = new ReportAuditLog
        {
            AuditId = Guid.NewGuid(),
            UserId = actorUserId,
            Action = action,
            ReportId = reportId,
            CustomerId = customerId,
            CreatedAt = DateTime.Now,
            IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim(),
        };
        return _audit.AddAsync(entry, ct);
    }
}
