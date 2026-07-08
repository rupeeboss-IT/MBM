using RB_Website_API.DTO;

namespace RB_Website_API.Services.IRepository;

public interface ICreditRepairManagementRepository
{
    Task<CreditRepairManagementStatsDto> GetStatsAsync(
        DateTime? dateFrom,
        DateTime? dateToExclusive,
        CancellationToken ct);

    Task<IReadOnlyList<CreditRepairSourceBreakdownDto>> GetSourceBreakdownAsync(
        DateTime? dateFrom,
        DateTime? dateToExclusive,
        CancellationToken ct);

    Task<IReadOnlyList<CreditRepairCampaignBreakdownDto>> GetCampaignBreakdownAsync(
        DateTime? dateFrom,
        DateTime? dateToExclusive,
        CancellationToken ct);

    Task<(List<CreditRepairListItemDto> Rows, int Total)> ListLeadsAsync(
        int page,
        int pageSize,
        string? search,
        string? source,
        string? campaign,
        string? linkStatus,
        DateTime? dateFrom,
        DateTime? dateToExclusive,
        string sortField,
        bool sortAsc,
        CancellationToken ct);

    Task<IReadOnlyList<string>> GetDistinctSourcesAsync(CancellationToken ct);

    Task<IReadOnlyList<string>> GetDistinctCampaignsAsync(CancellationToken ct);
}
