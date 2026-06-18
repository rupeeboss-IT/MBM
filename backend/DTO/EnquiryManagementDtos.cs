namespace RB_Website_API.DTO;

public sealed record EnquiryManagementStatsDto(
    int TotalEnquiries,
    int NewEnquiries,
    int ReadEnquiries,
    int InProgressEnquiries,
    int ResolvedEnquiries,
    int ClosedEnquiries);

public sealed record EnquiryListItemDto(
    int Id,
    string FullName,
    string? CompanyName,
    string Phone,
    string Email,
    string Source,
    string Subject,
    DateTime CreatedAt,
    string Status,
    string? AssignedToName,
    bool IsUnread);

public sealed record EnquiryStatusHistoryDto(
    Guid Id,
    string? OldStatus,
    string NewStatus,
    Guid ChangedByUserId,
    string? ChangedByName,
    DateTime ChangedOn,
    string? Remarks);

public sealed record EnquiryDetailDto(
    int Id,
    string FullName,
    string? CompanyName,
    string Phone,
    string Email,
    string Subject,
    string Message,
    string Source,
    DateTime CreatedAt,
    string Status,
    string? AssignedToName,
    IReadOnlyList<EnquiryStatusHistoryDto> StatusHistory);

public sealed record UpdateEnquiryStatusRequest(string Status, string? Remarks);

public sealed record BulkUpdateEnquiryStatusRequest(
    IReadOnlyList<int> EnquiryIds,
    string Status,
    string? Remarks);

public sealed record EnquiryManagementStatsResponse(
    bool Success,
    string? Message = null,
    EnquiryManagementStatsDto? Stats = null);

public sealed record EnquiryManagementListResponse(
    bool Success,
    string? Message = null,
    IReadOnlyList<EnquiryListItemDto>? Enquiries = null,
    int Total = 0,
    int Page = 1,
    int PageSize = 25);

public sealed record EnquiryManagementDetailResponse(
    bool Success,
    string? Message = null,
    EnquiryDetailDto? Enquiry = null);

public sealed record EnquiryActionResponse(
    bool Success,
    string? Message = null,
    int UpdatedCount = 0);

public sealed record EnquiryFiltersResponse(
    bool Success,
    string? Message = null,
    IReadOnlyList<string>? Sources = null,
    IReadOnlyList<string>? Statuses = null);
