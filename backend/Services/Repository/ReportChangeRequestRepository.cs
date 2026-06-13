using Microsoft.EntityFrameworkCore;
using RB_Website_API.Auth;
using RB_Website_API.Data;
using RB_Website_API.Models;
using RB_Website_API.Services.IRepository;

namespace RB_Website_API.Services.Repository;

public sealed class ReportChangeRequestRepository : IReportChangeRequestRepository
{
    private readonly AppDbContext _db;

    public ReportChangeRequestRepository(AppDbContext db) => _db = db;

    public Task<ReportChangeRequest?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.ReportChangeRequests.FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<bool> HasPendingForReportAsync(Guid reportId, CancellationToken ct) =>
        _db.ReportChangeRequests.AsNoTracking()
            .AnyAsync(r => r.ReportId == reportId && r.Status == ReportChangeRequestCatalog.StatusPending, ct);

    public async Task AddAsync(ReportChangeRequest request, CancellationToken ct)
    {
        await _db.ReportChangeRequests.AddAsync(request, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);

    public async Task<(IReadOnlyList<ReportChangeRequest> Items, int Total)> ListAsync(
        Guid? requestedBy,
        string? status,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var q = _db.ReportChangeRequests.AsNoTracking().AsQueryable();

        if (requestedBy.HasValue)
            q = q.Where(r => r.RequestedBy == requestedBy.Value);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalized = ReportChangeRequestCatalog.NormalizeStatus(status);
            q = q.Where(r => r.Status == normalized);
        }

        var total = await q.CountAsync(ct);
        var pageNum = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, AdminListQuery.MaxExportSize);

        var items = await q
            .OrderByDescending(r => r.RequestedOn)
            .Skip((pageNum - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<int> CountPendingAsync(CancellationToken ct) =>
        _db.ReportChangeRequests.AsNoTracking()
            .CountAsync(r => r.Status == ReportChangeRequestCatalog.StatusPending, ct);

    public async Task<IReadOnlyDictionary<Guid, ReportChangeRequest>> GetLatestByReportIdsAsync(
        IReadOnlyList<Guid> reportIds,
        CancellationToken ct)
    {
        if (reportIds.Count == 0)
            return new Dictionary<Guid, ReportChangeRequest>();

        var rows = await _db.ReportChangeRequests.AsNoTracking()
            .Where(r => reportIds.Contains(r.ReportId))
            .OrderByDescending(r => r.RequestedOn)
            .ToListAsync(ct);

        return rows
            .GroupBy(r => r.ReportId)
            .ToDictionary(g => g.Key, g => g.First());
    }

    public async Task<IReadOnlyDictionary<Guid, ReportChangeRequest>> GetPendingByReportIdsAsync(
        IReadOnlyList<Guid> reportIds,
        CancellationToken ct)
    {
        if (reportIds.Count == 0)
            return new Dictionary<Guid, ReportChangeRequest>();

        var rows = await _db.ReportChangeRequests.AsNoTracking()
            .Where(r => reportIds.Contains(r.ReportId)
                        && r.Status == ReportChangeRequestCatalog.StatusPending)
            .ToListAsync(ct);

        return rows.ToDictionary(r => r.ReportId);
    }
}
