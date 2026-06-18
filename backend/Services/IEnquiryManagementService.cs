using RB_Website_API.DTO;

namespace RB_Website_API.Services;

public interface IEnquiryManagementService
{
    Task<EnquiryManagementStatsResponse> GetStatsAsync(string actorRole, CancellationToken ct);

    Task<EnquiryFiltersResponse> GetFiltersAsync(string actorRole, CancellationToken ct);

    Task<EnquiryManagementListResponse> ListEnquiriesAsync(
        string actorRole,
        int page,
        int pageSize,
        string? search,
        string? status,
        string? source,
        string? dateFrom,
        string? dateTo,
        string? sortBy,
        string? sortDir,
        bool export,
        CancellationToken ct);

    Task<EnquiryManagementDetailResponse> GetEnquiryAsync(string actorRole, int enquiryId, CancellationToken ct);

    Task<EnquiryActionResponse> UpdateStatusAsync(
        Guid actorId,
        string actorRole,
        int enquiryId,
        UpdateEnquiryStatusRequest req,
        CancellationToken ct);

    Task<EnquiryActionResponse> BulkUpdateStatusAsync(
        Guid actorId,
        string actorRole,
        BulkUpdateEnquiryStatusRequest req,
        CancellationToken ct);
}
