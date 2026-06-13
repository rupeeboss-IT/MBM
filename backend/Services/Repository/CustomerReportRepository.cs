using Microsoft.EntityFrameworkCore;
using RB_Website_API.Auth;
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
        DateTime? dateFrom,
        DateTime? dateToExclusive,
        string sortBy,
        bool sortAsc,
        CancellationToken ct)
    {
        var q = from r in _db.CustomerReports.AsNoTracking()
                join u in _db.Users.AsNoTracking() on r.CustomerId equals u.UserId
                where r.IsActive
                select new { Report = r, User = u };

        if (dateFrom.HasValue)
            q = q.Where(x => x.Report.UploadDate >= dateFrom.Value);
        if (dateToExclusive.HasValue)
            q = q.Where(x => x.Report.UploadDate < dateToExclusive.Value);

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
        var size = Math.Clamp(pageSize, 1, AdminListQuery.MaxExportSize);

        var sorted = sortBy switch
        {
            "name" or "customer" or "fullname" => sortAsc ? q.OrderBy(x => x.User.FullName) : q.OrderByDescending(x => x.User.FullName),
            "memberid" => sortAsc ? q.OrderBy(x => x.Report.MemberId) : q.OrderByDescending(x => x.Report.MemberId),
            "downloads" or "downloadcount" => sortAsc ? q.OrderBy(x => x.Report.DownloadCount) : q.OrderByDescending(x => x.Report.DownloadCount),
            "lastdownload" or "lastdownloaddate" => sortAsc ? q.OrderBy(x => x.Report.LastDownloadDate) : q.OrderByDescending(x => x.Report.LastDownloadDate),
            "file" or "filename" or "originalfilename" => sortAsc ? q.OrderBy(x => x.Report.OriginalFileName) : q.OrderByDescending(x => x.Report.OriginalFileName),
            "size" or "filesize" => sortAsc ? q.OrderBy(x => x.Report.FileSize) : q.OrderByDescending(x => x.Report.FileSize),
            _ => sortAsc ? q.OrderBy(x => x.Report.UploadDate) : q.OrderByDescending(x => x.Report.UploadDate),
        };

        var items = await sorted
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

    public async Task SoftDeleteAsync(CustomerReport report, CancellationToken ct)
    {
        report.IsActive = false;
        await _db.SaveChangesAsync(ct);
    }

    public Task UpdateReportAsync(CustomerReport report, CancellationToken ct) =>
        _db.SaveChangesAsync(ct);
}
