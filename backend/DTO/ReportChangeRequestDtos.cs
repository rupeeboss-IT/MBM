namespace RB_Website_API.DTO;

public sealed record ReportChangeRequestListItemDto(
    Guid Id,
    Guid ReportId,
    string RequestType,
    string Status,
    string Reason,
    DateTime RequestedOn,
    string RequestedByName,
    string? CustomerName,
    string? MemberId,
    string? OriginalFileName,
    DateTime? ReportUploadDate);

public sealed record ReportChangeRequestDetailDto(
    Guid Id,
    Guid ReportId,
    string RequestType,
    string Status,
    string Reason,
    string? Remarks,
    DateTime RequestedOn,
    Guid RequestedBy,
    string RequestedByName,
    DateTime? ApprovedOn,
    string? ApprovedByName,
    DateTime? RejectedOn,
    string? RejectedByName,
    string? PreviousReportPath,
    string? NewReportPath,
    string? PreviousValues,
    string? NewValues,
    string? PendingOriginalFileName,
    long? PendingFileSize,
    ReportSummaryDto? Report);

public sealed record ReportSummaryDto(
    Guid Id,
    string CustomerName,
    string MemberId,
    string Email,
    string OriginalFileName,
    long FileSize,
    DateTime UploadDate,
    int DownloadCount,
    DateTime? LastDownloadDate,
    string? PlanName,
    string ReportType);

public sealed record ReportAuditLogItemDto(
    Guid AuditId,
    string Action,
    DateTime CreatedAt,
    string? ActorName,
    Guid? RequestId,
    string? Remarks,
    string? PreviousReportPath,
    string? NewReportPath,
    string? PreviousValues,
    string? NewValues);

public sealed record PendingRequestsCountDto(int Count);

public sealed record ReportRequestStatusDto(
    bool HasPendingRequest,
    Guid? PendingRequestId,
    string? PendingRequestType,
    string? LatestRequestStatus);
