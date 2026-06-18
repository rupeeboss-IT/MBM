using RB_Website_API.DTO;
using RB_Website_API.Models;

namespace RB_Website_API.Services.IRepository;

public interface IEnquiryManagementRepository
{
    Task<EnquiryManagementStatsDto> GetStatsAsync(CancellationToken ct);

    Task<(List<EnquiryListItemDto> Rows, int Total)> ListEnquiriesAsync(
        int page,
        int pageSize,
        string? search,
        string? status,
        string? source,
        DateTime? dateFrom,
        DateTime? dateToExclusive,
        string sortField,
        bool sortAsc,
        CancellationToken ct);

    Task<EnquiryDetailDto?> GetDetailAsync(int enquiryId, CancellationToken ct);

    Task<ContactSubmission?> GetByIdAsync(int enquiryId, bool track, CancellationToken ct);

    Task AddStatusHistoryAsync(EnquiryStatusHistory entry, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
