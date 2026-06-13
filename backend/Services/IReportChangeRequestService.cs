using RB_Website_API.DTO;

namespace RB_Website_API.Services;

public interface IReportChangeRequestService
{
    Task<(bool Success, string? Message, Guid? RequestId)> CreateRequestAsync(
        Guid actorUserId,
        bool isSuperAdmin,
        Guid reportId,
        string requestType,
        string reason,
        string? newOriginalFileName,
        IFormFile? replacementFile,
        string? ipAddress,
        CancellationToken ct);

    Task<PagedResultDto<ReportChangeRequestListItemDto>> ListRequestsAsync(
        Guid actorUserId,
        bool isSuperAdmin,
        string? status,
        int page,
        int pageSize,
        CancellationToken ct);

    Task<int> GetPendingCountAsync(bool isSuperAdmin, CancellationToken ct);

    Task<(bool Success, string? Message, ReportChangeRequestDetailDto? Detail)> GetRequestDetailAsync(
        Guid actorUserId,
        bool isSuperAdmin,
        Guid requestId,
        CancellationToken ct);

    Task<(bool Success, string? Message)> ApproveRequestAsync(
        Guid superAdminUserId,
        Guid requestId,
        string? remarks,
        string? ipAddress,
        CancellationToken ct);

    Task<(bool Success, string? Message)> RejectRequestAsync(
        Guid superAdminUserId,
        Guid requestId,
        string? remarks,
        string? ipAddress,
        CancellationToken ct);

    Task<(bool Success, string? Message)> DirectSoftDeleteAsync(
        Guid superAdminUserId,
        Guid reportId,
        string reason,
        string? ipAddress,
        CancellationToken ct);

    Task<(bool Success, string? Message)> DirectEditAsync(
        Guid superAdminUserId,
        Guid reportId,
        string? originalFileName,
        string reason,
        string? ipAddress,
        CancellationToken ct);

    Task<(bool Success, string? Message)> DirectReplaceAsync(
        Guid superAdminUserId,
        Guid reportId,
        IFormFile file,
        string reason,
        string? ipAddress,
        CancellationToken ct);

    Task<IReadOnlyList<ReportAuditLogItemDto>> ListAuditHistoryAsync(
        Guid actorUserId,
        bool isSuperAdmin,
        Guid reportId,
        CancellationToken ct);
}
