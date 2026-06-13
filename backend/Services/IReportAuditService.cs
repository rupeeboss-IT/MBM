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

    Task StageChangeLogAsync(
        string action,
        Guid? actorUserId,
        Guid? reportId,
        Guid? customerId,
        Guid? requestId,
        string? remarks,
        string? previousReportPath,
        string? newReportPath,
        string? previousValues,
        string? newValues,
        string? ipAddress,
        CancellationToken ct);
}
