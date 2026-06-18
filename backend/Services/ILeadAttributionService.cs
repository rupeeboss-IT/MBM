using RB_Website_API.DTO;

namespace RB_Website_API.Services;

public interface ILeadAttributionService
{
    Task<LeadAttributionStatsResponse> GetDashboardAsync(
        Guid actorId, string actorRole, string? dateFrom, string? dateTo, CancellationToken ct);

    Task<LeadCustomerListResponse> ListCustomersAsync(
        Guid actorId,
        string actorRole,
        int page,
        int pageSize,
        string? search,
        string? sourceType,
        string? sourceName,
        string? employeeName,
        string? partnerName,
        string? planCode,
        string? dateFrom,
        string? dateTo,
        string? sortBy,
        string? sortDir,
        bool export,
        CancellationToken ct);

    Task<LeadCustomerDetailResponse> GetCustomerAsync(
        Guid actorId, string actorRole, Guid userId, CancellationToken ct);

    Task<LeadFilterOptionsResponse> GetFilterOptionsAsync(
        Guid actorId, string actorRole, CancellationToken ct);

    Task<LeadPerformerDetailResponse> GetPerformerDetailsAsync(
        Guid actorId,
        string actorRole,
        string performerType,
        string code,
        string? dateFrom,
        string? dateTo,
        CancellationToken ct);
}
