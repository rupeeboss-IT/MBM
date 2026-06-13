using RB_Website_API.Models;

namespace RB_Website_API.Services.IRepository;

public interface ISdrReportRepository
{
    Task<CustomerReport?> GetLatestValidSdrReportAsync(Guid userId, DateTime asOf, CancellationToken ct);

    Task<CustomerReport?> GetByIdForCustomerAsync(Guid reportId, Guid customerId, CancellationToken ct);

    Task AddReportAsync(CustomerReport report, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
