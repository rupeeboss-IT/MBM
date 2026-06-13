using RB_Website_API.Models;

namespace RB_Website_API.Services.IRepository;

public interface ICustomerReportRepository
{
    Task<CustomerReport?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<CustomerReport?> GetByIdForCustomerAsync(Guid id, Guid customerId, CancellationToken ct);
    Task AddAsync(CustomerReport report, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
    Task<IReadOnlyList<CustomerReport>> ListActiveByCustomerAsync(Guid customerId, CancellationToken ct);

    Task<(IReadOnlyList<CustomerReport> Items, int Total)> ListHistoryAsync(
        string? search,
        int page,
        int pageSize,
        DateTime? dateFrom,
        DateTime? dateToExclusive,
        string sortBy,
        bool sortAsc,
        CancellationToken ct);

    Task IncrementDownloadAsync(CustomerReport report, CancellationToken ct);
    Task SoftDeleteAsync(CustomerReport report, CancellationToken ct);
    Task UpdateReportAsync(CustomerReport report, CancellationToken ct);
}
