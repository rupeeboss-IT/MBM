namespace RB_Website_API.DTO;

public static class ConnectConnectionStates
{
    public const string None = "none";
    public const string Pending = "pending";
    public const string Connected = "connected";
    public const string Rejected = "rejected";
}

public sealed record ConnectAccessSummaryDto(
    string Tier,
    int UnlocksUsed,
    int? UnlocksLimit,
    bool CanUnlockMore,
    string? PlanCode,
    string? PlanName);

public sealed record ConnectStatsDto(
    int ListedCount,
    int SectorCount,
    int StateCount);

public sealed record ConnectProfileCardDto(
    Guid UserId,
    bool IsLocked,
    string? DisplayName,
    string? CompanyName,
    string? Initials,
    string? BusinessType,
    string? Sector,
    string? State,
    string? City,
    string? Turnover,
    string? Description,
    bool Verified,
    string ConnectionStatus,
    bool CanConnect,
    bool ContactVisible);

public sealed record ConnectProfileDetailDto(
    Guid UserId,
    bool IsLocked,
    string? DisplayName,
    string? CompanyName,
    string? Designation,
    string? Initials,
    string? BusinessType,
    string? Sector,
    string? State,
    string? City,
    string? Turnover,
    string? Udyam,
    string? Employees,
    string? Description,
    string? Website,
    string? Established,
    string? SocialLinksJson,
    string? Phone,
    string? Email,
    bool Verified,
    string ConnectionStatus,
    bool CanConnect,
    bool ContactVisible,
    string? ConnectionMessage);

public sealed record ConnectSearchResponse(
    bool Success,
    string? Message = null,
    IReadOnlyList<ConnectProfileCardDto>? Profiles = null,
    int Total = 0,
    int Page = 1,
    int PageSize = 20,
    ConnectStatsDto? Stats = null,
    ConnectAccessSummaryDto? Access = null);

public sealed record ConnectProfileDetailResponse(
    bool Success,
    string? Message = null,
    ConnectProfileDetailDto? Profile = null,
    ConnectAccessSummaryDto? Access = null);

public sealed record ConnectAccessSummaryResponse(
    bool Success,
    string? Message = null,
    ConnectAccessSummaryDto? Access = null);

public sealed record SendConnectRequestBody(string? Message);

public sealed record ConnectRequestActionResponse(
    bool Success,
    string? Message = null,
    string? Status = null);

public sealed record ConnectIncomingRequestDto(
    Guid RequestId,
    Guid FromUserId,
    string? FromDisplayName,
    string? FromCompanyName,
    string? Message,
    DateTime CreatedAt);

public sealed record ConnectIncomingListResponse(
    bool Success,
    string? Message = null,
    IReadOnlyList<ConnectIncomingRequestDto>? Items = null);

public sealed record UpdateConnectMemberProfileRequest(
    string? Designation,
    string? BusinessType,
    string? Sector,
    string? State,
    string? City,
    string? Turnover,
    string? Udyam,
    string? Employees,
    string? Description,
    string? Website,
    string? Established,
    string? SocialLinksJson);

public sealed record ConnectMemberProfileResponse(
    bool Success,
    string? Message = null,
    ConnectProfileDetailDto? Profile = null);

// --- Admin ---

public sealed record ConnectAdminListingListItemDto(
    Guid ListingId,
    Guid UserId,
    string Role,
    string FullName,
    string? CompanyName,
    string Phone,
    string Email,
    bool IsListed,
    bool IsVerified,
    bool IsActive,
    string? Sector,
    string? City,
    string? State,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record ConnectAdminListingDetailDto(
    Guid ListingId,
    Guid UserId,
    string Role,
    string FullName,
    string? CompanyName,
    string Phone,
    string Email,
    bool IsListed,
    bool IsVerified,
    bool IsActive,
    string? BusinessType,
    string? Sector,
    string? State,
    string? City,
    string? Turnover,
    string? Udyam,
    string? Employees,
    string? Description,
    string? Website,
    string? Established,
    string? SocialLinksJson,
    string? Remarks,
    ConnectMemberProfileReadOnlyDto? MemberProfile,
    IReadOnlyList<string> CustomerLockedFields,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record ConnectMemberProfileReadOnlyDto(
    string? Designation,
    string? BusinessType,
    string? Sector,
    string? State,
    string? City,
    string? Turnover,
    string? Udyam,
    string? Employees,
    string? Description,
    string? Website,
    string? Established,
    string? SocialLinksJson,
    DateTime? UpdatedAt);

public sealed record CreateConnectAdminListingRequest(
    Guid UserId,
    bool IsListed,
    bool IsVerified,
    bool IsActive,
    string? BusinessType,
    string? Sector,
    string? State,
    string? City,
    string? Turnover,
    string? Udyam,
    string? Employees,
    string? Description,
    string? Website,
    string? Established,
    string? SocialLinksJson,
    string? Remarks);

public sealed record UpdateConnectAdminListingRequest(
    bool IsListed,
    bool IsVerified,
    bool IsActive,
    string? BusinessType,
    string? Sector,
    string? State,
    string? City,
    string? Turnover,
    string? Udyam,
    string? Employees,
    string? Description,
    string? Website,
    string? Established,
    string? SocialLinksJson,
    string? Remarks);

public sealed record ConnectAdminListingListResponse(
    bool Success,
    string? Message = null,
    IReadOnlyList<ConnectAdminListingListItemDto>? Listings = null,
    int Total = 0,
    int Page = 1,
    int PageSize = 10);

public sealed record ConnectAdminListingDetailResponse(
    bool Success,
    string? Message = null,
    ConnectAdminListingDetailDto? Listing = null);

public sealed record ConnectAdminActionResponse(
    bool Success,
    string? Message = null,
    Guid? ListingId = null);

public sealed record ConnectUserSearchItemDto(
    Guid UserId,
    string Role,
    string FullName,
    string? CompanyName,
    string Phone,
    string Email,
    bool HasListing);

public sealed record ConnectUserSearchResponse(
    bool Success,
    string? Message = null,
    IReadOnlyList<ConnectUserSearchItemDto>? Users = null);
