namespace RB_Website_API.DTO;

public sealed record CustomerSearchResultDto(
    Guid UserId,
    string MemberId,
    string FullName,
    string Email,
    string Phone,
    bool HasActiveSubscription,
    string? PlanName,
    DateTime? ActiveTo);

public sealed record CustomerReportListItemDto(
    Guid Id,
    string ReportName,
    DateTime UploadDate,
    string PlanName,
    long FileSize,
    string MemberId,
    string ReportType);

public sealed record AdminReportHistoryItemDto(
    Guid Id,
    string CustomerName,
    string MemberId,
    string Email,
    DateTime UploadDate,
    int DownloadCount,
    DateTime? LastDownloadDate,
    string OriginalFileName,
    long FileSize,
    string? PlanName,
    bool HasPendingRequest = false,
    Guid? PendingRequestId = null,
    string? PendingRequestType = null,
    string? LatestRequestStatus = null);

public sealed record AdminSdrGenerateResponse(
    bool Success,
    string? Message,
    Guid? ReportId,
    Guid? RequestId,
    DateTime? ExpiryDate,
    string? Outcome);

public sealed record PagedResultDto<T>(
    bool Success,
    string? Message,
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize);
