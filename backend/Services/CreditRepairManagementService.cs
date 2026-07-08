using RB_Website_API.Auth;
using RB_Website_API.DTO;
using RB_Website_API.Services.IRepository;

namespace RB_Website_API.Services;

public sealed class CreditRepairManagementService : ICreditRepairManagementService
{
    private readonly ICreditRepairManagementRepository _repo;

    public CreditRepairManagementService(ICreditRepairManagementRepository repo) => _repo = repo;

    public async Task<CreditRepairManagementStatsResponse> GetStatsAsync(
        string actorRole,
        string? dateFrom,
        string? dateTo,
        CancellationToken ct)
    {
        if (!UserRoles.IsAdminRole(actorRole))
            return new CreditRepairManagementStatsResponse(false, "Not authorized.");

        var from = AdminListQuery.ParseDateFrom(dateFrom);
        var toEx = AdminListQuery.ParseDateToExclusive(dateTo);

        var stats = await _repo.GetStatsAsync(from, toEx, ct);
        var sources = await _repo.GetSourceBreakdownAsync(from, toEx, ct);
        var campaigns = await _repo.GetCampaignBreakdownAsync(from, toEx, ct);

        return new CreditRepairManagementStatsResponse(true, "OK", stats, sources, campaigns);
    }

    public async Task<CreditRepairFiltersResponse> GetFiltersAsync(string actorRole, CancellationToken ct)
    {
        if (!UserRoles.IsAdminRole(actorRole))
            return new CreditRepairFiltersResponse(false, "Not authorized.");

        var sources = await _repo.GetDistinctSourcesAsync(ct);
        var campaigns = await _repo.GetDistinctCampaignsAsync(ct);
        return new CreditRepairFiltersResponse(true, "OK", sources, campaigns);
    }

    public async Task<CreditRepairManagementListResponse> ListLeadsAsync(
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
        CancellationToken ct)
    {
        if (!UserRoles.IsAdminRole(actorRole))
            return new CreditRepairManagementListResponse(false, "Not authorized.");

        (page, pageSize) = AdminListQuery.Normalize(page, pageSize, export);
        search = AdminListQuery.NormalizeSearch(search);
        var from = AdminListQuery.ParseDateFrom(dateFrom);
        var toEx = AdminListQuery.ParseDateToExclusive(dateTo);
        var (sortField, sortAsc) = NormalizeSort(sortBy, sortDir);

        var (rows, total) = await _repo.ListLeadsAsync(
            page,
            pageSize,
            search,
            source,
            campaign,
            linkStatus,
            from,
            toEx,
            sortField,
            sortAsc,
            ct);

        return new CreditRepairManagementListResponse(true, "OK", rows, total, page, pageSize);
    }

    private static (string SortField, bool SortAsc) NormalizeSort(string? sortBy, string? sortDir)
    {
        var key = (sortBy ?? "").Trim().ToLowerInvariant();
        var dir = (sortDir ?? "").Trim().ToLowerInvariant();

        return key switch
        {
            "latest" => ("created", false),
            "oldest" => ("created", true),
            "name_asc" => ("name", true),
            "name_desc" => ("name", false),
            "phone" => ("phone", dir != "desc"),
            "source" => ("source", dir != "desc"),
            "campaign" => ("campaign", dir != "desc"),
            "lead_id" or "leadid" => ("lead_id", dir != "desc"),
            "" => ("created", false),
            _ => (NormalizeSortField(key), dir == "asc"),
        };
    }

    private static string NormalizeSortField(string field) =>
        field switch
        {
            "fullname" or "member" or "customer" => "name",
            "createdat" or "date" => "created",
            "campaignname" => "campaign",
            _ => field,
        };
}
