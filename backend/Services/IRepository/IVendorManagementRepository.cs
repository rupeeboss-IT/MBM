using RB_Website_API.DTO;
using RB_Website_API.Models;

namespace RB_Website_API.Services.IRepository;

public interface IVendorManagementRepository
{
    Task<Vendor?> GetByIdAsync(Guid vendorId, bool track, CancellationToken ct);
    Task AddVendorAsync(Vendor vendor, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);

    Task<(List<VendorListItemDto> Rows, int Total)> ListVendorsAsync(
        int page,
        int pageSize,
        string? search,
        string? status,
        DateTime? dateFrom,
        DateTime? dateToExclusive,
        string sortField,
        bool sortAsc,
        CancellationToken ct);

    Task<VendorDetailDto?> GetDetailAsync(Guid vendorId, CancellationToken ct);
    Task<VendorManagementStatsDto> GetStatsAsync(CancellationToken ct);

    Task<IReadOnlyList<VendorPlanDto>> ListActivePlansAsync(CancellationToken ct);
    Task<IReadOnlyList<int>> GetVendorPlanIdsAsync(Guid vendorId, CancellationToken ct);
    Task SetVendorPlansAsync(Guid vendorId, IReadOnlyList<int> planIds, Guid actorId, CancellationToken ct);

    Task AddAuditLogAsync(VendorAuditLog log, CancellationToken ct);
    Task<(List<VendorAuditLogDto> Rows, int Total)> ListAuditLogsAsync(
        Guid vendorId, int page, int pageSize, CancellationToken ct);

    Task<IReadOnlyList<VendorPlanMappingGroupDto>> ListPlanMappingsAsync(CancellationToken ct);
}
