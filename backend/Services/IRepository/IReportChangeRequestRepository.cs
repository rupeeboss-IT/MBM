using RB_Website_API.Models;

namespace RB_Website_API.Services.IRepository;

public interface IReportChangeRequestRepository
{
    Task<ReportChangeRequest?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<bool> HasPendingForReportAsync(Guid reportId, CancellationToken ct);
    Task AddAsync(ReportChangeRequest request, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);

    Task<(IReadOnlyList<ReportChangeRequest> Items, int Total)> ListAsync(
        Guid? requestedBy,
        string? status,
        int page,
        int pageSize,
        CancellationToken ct);

    Task<int> CountPendingAsync(CancellationToken ct);

    Task<IReadOnlyDictionary<Guid, ReportChangeRequest>> GetLatestByReportIdsAsync(
        IReadOnlyList<Guid> reportIds,
        CancellationToken ct);

    Task<IReadOnlyDictionary<Guid, ReportChangeRequest>> GetPendingByReportIdsAsync(
        IReadOnlyList<Guid> reportIds,
        CancellationToken ct);
}
