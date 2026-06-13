namespace RB_Website_API.DTO;

public sealed record SchemeDiscoveryProfileDto(
    string FullName,
    string Email,
    string Phone,
    string MemberId);

public sealed record SchemeDiscoveryMembershipDto(
    Guid UserPlanId,
    string PlanCode,
    string PlanName,
    DateTime ActiveFrom,
    DateTime? ActiveTo);

public sealed record SchemeDiscoveryExistingReportDto(
    Guid ReportId,
    string ReportName,
    DateTime? UploadDate);

public sealed record SchemeDiscoveryStatusDto(
    bool Success,
    string Phase,
    string? Message,
    SchemeDiscoveryProfileDto? Profile,
    SchemeDiscoveryMembershipDto? Membership,
    bool IsPremiumOrPro,
    bool HasOneTimeEntitlement,
    SchemeDiscoveryExistingReportDto? ExistingReport,
    string? PendingRequestStatus,
    Guid? DraftRequestId = null,
    string? SavedUdyam = null,
    Guid? PendingRequestId = null);

public sealed record SchemeDiscoverySubmitResponse(
    bool Success,
    string? Message,
    Guid? RequestId,
    string? Status,
    string? Outcome = null,
    Guid? ReportId = null,
    DateTime? ExpiryDate = null);
