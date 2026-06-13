using Microsoft.EntityFrameworkCore;
using RB_Website_API.Auth;
using RB_Website_API.Data;
using RB_Website_API.Models;
using RB_Website_API.Services.IRepository;

namespace RB_Website_API.Services.Repository;

public sealed class SdrReportRepository : ISdrReportRepository
{
    private readonly AppDbContext _db;

    public SdrReportRepository(AppDbContext db) => _db = db;

    public Task<CustomerReport?> GetLatestValidSdrReportAsync(Guid userId, DateTime asOf, CancellationToken ct) =>
        _db.CustomerReports.AsNoTracking()
            .Where(r => r.CustomerId == userId
                        && r.ReportType == SdrReportCatalog.ReportType
                        && r.IsActive
                        && r.ExpiryDate != null
                        && r.ExpiryDate >= asOf)
            .OrderByDescending(r => r.UploadDate)
            .FirstOrDefaultAsync(ct);

    public Task<CustomerReport?> GetByIdForCustomerAsync(Guid reportId, Guid customerId, CancellationToken ct) =>
        _db.CustomerReports.AsNoTracking()
            .FirstOrDefaultAsync(r =>
                r.Id == reportId
                && r.CustomerId == customerId
                && r.IsActive, ct);

    public async Task AddReportAsync(CustomerReport report, CancellationToken ct) =>
        await _db.CustomerReports.AddAsync(report, ct);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
