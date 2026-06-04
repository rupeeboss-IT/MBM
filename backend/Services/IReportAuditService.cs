namespace RB_Website_API.Services;

public interface IReportAuditService
{
    Task LogAsync(
        string action,
        Guid? actorUserId,
        Guid? reportId,
        Guid? customerId,
        string? ipAddress,
        CancellationToken ct);
}
