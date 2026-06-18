using RB_Website_API.DTO;

namespace RB_Website_API.Services;

public interface IVendorManagementService
{
    Task<VendorManagementStatsResponse> GetStatsAsync(string actorRole, CancellationToken ct);

    Task<VendorManagementListResponse> ListVendorsAsync(
        string actorRole, int page, int pageSize, string? search, string? status,
        string? dateFrom, string? dateTo, string? sortBy, string? sortDir, bool export, CancellationToken ct);

    Task<VendorManagementDetailResponse> GetVendorAsync(string actorRole, Guid vendorId, CancellationToken ct);

    Task<VendorActionResponse> CreateVendorAsync(Guid actorId, string actorRole, CreateVendorRequest req, CancellationToken ct);

    Task<VendorActionResponse> UpdateVendorAsync(
        Guid actorId, string actorRole, Guid vendorId, UpdateVendorRequest req, CancellationToken ct);

    Task<VendorActionResponse> SetActiveAsync(
        Guid actorId, string actorRole, Guid vendorId, SetVendorActiveRequest req, CancellationToken ct);

    Task<VendorActionResponse> SoftDeleteAsync(
        Guid actorId, string actorRole, Guid vendorId, DeleteVendorRequest req, CancellationToken ct);

    Task<VendorActionResponse> AssignPlansAsync(
        Guid actorId, string actorRole, Guid vendorId, AssignVendorPlansRequest req, CancellationToken ct);

    Task<VendorAuditListResponse> ListAuditAsync(
        string actorRole, Guid vendorId, int page, int pageSize, CancellationToken ct);

    Task<VendorPlanListResponse> ListPlansAsync(string actorRole, CancellationToken ct);

    Task<VendorPlanMappingListResponse> ListPlanMappingsAsync(string actorRole, CancellationToken ct);
}
