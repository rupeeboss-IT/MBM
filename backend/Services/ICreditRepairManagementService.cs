using RB_Website_API.DTO;

namespace RB_Website_API.Services;

public interface ICreditRepairManagementService
{
    Task<CreditRepairManagementStatsResponse> GetStatsAsync(
        string actorRole,
        string? dateFrom,
        string? dateTo,
        CancellationToken ct);

    Task<CreditRepairFiltersResponse> GetFiltersAsync(string actorRole, CancellationToken ct);

    Task<CreditRepairManagementListResponse> ListLeadsAsync(
        string actorRole,
        int page,
        int pageSize,
        string? search,
        string? source,
        string? campaign,
        string? linkStatus,
        string? dateFrom,
        string? dateTo,
        string? sortBy,
        string? sortDir,
        bool export,
        CancellationToken ct);
}
