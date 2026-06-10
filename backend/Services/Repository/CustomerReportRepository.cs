using Microsoft.EntityFrameworkCore;
using RB_Website_API.Data;
using RB_Website_API.Models;
using RB_Website_API.Services.IRepository;

namespace RB_Website_API.Services.Repository;

public sealed class CustomerReportRepository : ICustomerReportRepository
{
    private readonly AppDbContext _db;

    public CustomerReportRepository(AppDbContext db) => _db = db;

    public Task<CustomerReport?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.CustomerReports.FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<CustomerReport?> GetByIdForCustomerAsync(Guid id, Guid customerId, CancellationToken ct) =>
        _db.CustomerReports.FirstOrDefaultAsync(r => r.Id == id && r.CustomerId == customerId && r.IsActive, ct);

    public async Task AddAsync(CustomerReport report, CancellationToken ct)
    {
        await _db.CustomerReports.AddAsync(report, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);

    public async Task<IReadOnlyList<CustomerReport>> ListActiveByCustomerAsync(Guid customerId, CancellationToken ct) =>
        await _db.CustomerReports.AsNoTracking()
            .Where(r => r.CustomerId == customerId && r.IsActive)
            .OrderByDescending(r => r.UploadDate)
            .ToListAsync(ct);

    public async Task<(IReadOnlyList<CustomerReport> Items, int Total)> ListHistoryAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var q = from r in _db.CustomerReports.AsNoTracking()
                join u in _db.Users.AsNoTracking() on r.CustomerId equals u.UserId
                where r.IsActive
                select new { Report = r, User = u };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var lower = term.ToLowerInvariant();
            q = q.Where(x =>
                x.User.FullName.ToLower().Contains(lower)
                || x.User.Email.ToLower().Contains(lower)
                || x.Report.MemberId.ToLower().Contains(lower)
                || x.User.Phone.Contains(term));
        }

        var total = await q.CountAsync(ct);
        var pageNum = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, 100);

        var items = await q
            .OrderByDescending(x => x.Report.UploadDate)
            .Skip((pageNum - 1) * size)
            .Take(size)
            .Select(x => x.Report)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task IncrementDownloadAsync(CustomerReport report, CancellationToken ct)
    {
        report.DownloadCount++;
        report.LastDownloadDate = DateTime.Now;
        await _db.SaveChangesAsync(ct);
    }
}
