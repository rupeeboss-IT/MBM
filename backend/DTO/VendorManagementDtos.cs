namespace RB_Website_API.DTO;

public static class VendorAuditActions
{
    public const string Created = "Created";
    public const string Updated = "Updated";
    public const string Deleted = "Deleted";
    public const string Activated = "Activated";
    public const string Deactivated = "Deactivated";
    public const string PlanAssigned = "PlanAssigned";
    public const string PlanRemoved = "PlanRemoved";
}

public sealed record VendorPlanDto(int PlanId, string Code, string Name);

public sealed record VendorListItemDto(
    Guid VendorId,
    string ServiceName,
    string CompanyName,
    string ContactPersonName,
    string Mobile,
    string? AlternateMobile,
    string Email,
    string? AlternateEmail,
    IReadOnlyList<string> AssignedPlanNames,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record VendorDetailDto(
    Guid VendorId,
    string ServiceName,
    string CompanyName,
    string ContactPersonName,
    string Mobile,
    string? AlternateMobile,
    string Email,
    string? AlternateEmail,
    string? Website,
    string? Address,
    string? City,
    string? State,
    string? Country,
    string? Pincode,
    string? Remarks,
    bool IsActive,
    bool IsDeleted,
    DateTime? DeletedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<VendorPlanDto> AssignedPlans);

public sealed record VendorAuditLogDto(
    Guid Id,
    Guid VendorId,
    string Action,
    Guid PerformedByUserId,
    string? PerformedByName,
    DateTime PerformedOn,
    string? PreviousValues,
    string? NewValues,
    string? Remarks);

public sealed record VendorPlanMappingGroupDto(
    int PlanId,
    string PlanCode,
    string PlanName,
    IReadOnlyList<VendorPlanMappingItemDto> Vendors);

public sealed record VendorPlanMappingItemDto(
    Guid VendorId,
    string ServiceName,
    string CompanyName,
    bool IsActive);

public sealed record VendorManagementStatsDto(
    int TotalVendors,
    int ActiveVendors,
    int InactiveVendors,
    int VendorsAssignedToPlans);

public sealed record CreateVendorRequest(
    string ServiceName,
    string CompanyName,
    string ContactPersonName,
    string Mobile,
    string? AlternateMobile,
    string Email,
    string? AlternateEmail,
    string? Website,
    string? Address,
    string? City,
    string? State,
    string? Country,
    string? Pincode,
    string? Remarks,
    bool IsActive,
    IReadOnlyList<int>? PlanIds);

public sealed record UpdateVendorRequest(
    string ServiceName,
    string CompanyName,
    string ContactPersonName,
    string Mobile,
    string? AlternateMobile,
    string Email,
    string? AlternateEmail,
    string? Website,
    string? Address,
    string? City,
    string? State,
    string? Country,
    string? Pincode,
    string? Remarks,
    bool IsActive,
    IReadOnlyList<int>? PlanIds);

public sealed record SetVendorActiveRequest(bool IsActive, string? Remarks);

public sealed record DeleteVendorRequest(string? Remarks);

public sealed record AssignVendorPlansRequest(IReadOnlyList<int> PlanIds);

public sealed record VendorManagementListResponse(
    bool Success,
    string? Message = null,
    IReadOnlyList<VendorListItemDto>? Vendors = null,
    int Total = 0,
    int Page = 1,
    int PageSize = 10);

public sealed record VendorManagementDetailResponse(
    bool Success,
    string? Message = null,
    VendorDetailDto? Vendor = null);

public sealed record VendorActionResponse(
    bool Success,
    string? Message = null,
    Guid? VendorId = null);

public sealed record VendorManagementStatsResponse(
    bool Success,
    string? Message = null,
    VendorManagementStatsDto? Stats = null);

public sealed record VendorAuditListResponse(
    bool Success,
    string? Message = null,
    IReadOnlyList<VendorAuditLogDto>? Items = null,
    int Total = 0,
    int Page = 1,
    int PageSize = 10);

public sealed record VendorPlanListResponse(
    bool Success,
    string? Message = null,
    IReadOnlyList<VendorPlanDto>? Plans = null);

public sealed record VendorPlanMappingListResponse(
    bool Success,
    string? Message = null,
    IReadOnlyList<VendorPlanMappingGroupDto>? Groups = null);
