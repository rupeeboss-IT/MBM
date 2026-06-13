namespace RB_Website_API.DTO;

public sealed record UserManagementStatsDto(
    int TotalAdmins,
    int ActiveAdmins,
    int InactiveAdmins,
    int TotalPartners,
    int ActivePartners,
    int InactivePartners,
    int TotalMembers,
    int ActiveMembers,
    int InactiveMembers);

public sealed record ManagedUserDto(
    Guid UserId,
    string Role,
    string FullName,
    string Email,
    string Phone,
    string? CompanyName,
    string? MemberId,
    bool IsActive,
    bool IsDeleted,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    Guid? CreatedByUserId,
    string? CreatedByName);

public sealed record ManagedUserDetailDto(
    Guid UserId,
    string Role,
    string FullName,
    string Email,
    string Phone,
    string? CompanyName,
    string? MemberId,
    bool IsActive,
    bool IsDeleted,
    DateTime? DeletedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    Guid? CreatedByUserId,
    string? CreatedByName,
    string? PlanCode,
    string? PlanName);

public sealed record UserAuditLogDto(
    Guid Id,
    Guid UserId,
    string UserType,
    string Action,
    Guid PerformedByUserId,
    string? PerformedByName,
    DateTime PerformedOn,
    string? PreviousValues,
    string? NewValues,
    string? Remarks);

public sealed record UserStatusHistoryDto(
    Guid Id,
    Guid UserId,
    string UserType,
    string ActionType,
    string? OldStatus,
    string? NewStatus,
    string? Remarks,
    Guid PerformedByUserId,
    string? PerformedByName,
    DateTime PerformedOn);

public sealed record CreateManagedUserRequest(
    string FullName,
    string Email,
    string Phone,
    string Password,
    string? CompanyName);

public sealed record UpdateManagedUserRequest(
    string FullName,
    string Email,
    string Phone,
    string? CompanyName,
    string? Password);

public sealed record SetManagedUserActiveRequest(bool IsActive, string? Remarks);

public sealed record DeleteManagedUserRequest(string? Remarks);

public sealed record UserManagementListResponse(
    bool Success,
    string? Message = null,
    IReadOnlyList<ManagedUserDto>? Users = null,
    int Total = 0,
    int Page = 1,
    int PageSize = 10);

public sealed record UserManagementDetailResponse(
    bool Success,
    string? Message = null,
    ManagedUserDetailDto? User = null);

public sealed record UserManagementStatsResponse(
    bool Success,
    string? Message = null,
    UserManagementStatsDto? Stats = null);

public sealed record UserAuditListResponse(
    bool Success,
    string? Message = null,
    IReadOnlyList<UserAuditLogDto>? Items = null,
    int Total = 0,
    int Page = 1,
    int PageSize = 10);

public sealed record UserStatusHistoryListResponse(
    bool Success,
    string? Message = null,
    IReadOnlyList<UserStatusHistoryDto>? Items = null,
    int Total = 0,
    int Page = 1,
    int PageSize = 10);

public sealed record UserActionResponse(bool Success, string? Message, Guid? UserId = null);

public static class UserAuditActions
{
    public const string Created = "Created";
    public const string Updated = "Updated";
    public const string Activated = "Activated";
    public const string Deactivated = "Deactivated";
    public const string Deleted = "Deleted";
}

public static class UserStatusLabels
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";

    public static string FromBool(bool isActive) => isActive ? Active : Inactive;
}
