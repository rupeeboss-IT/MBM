using RB_Website_API.Models;

namespace RB_Website_API.Services.IRepository;

public interface IReportAuditRepository
{
    Task AddAsync(ReportAuditLog entry, CancellationToken ct);
}
