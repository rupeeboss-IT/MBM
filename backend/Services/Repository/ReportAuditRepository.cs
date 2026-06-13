using Microsoft.EntityFrameworkCore;
using RB_Website_API.Data;
using RB_Website_API.Models;
using RB_Website_API.Services.IRepository;

namespace RB_Website_API.Services.Repository;

public sealed class ReportAuditRepository : IReportAuditRepository
{
    private readonly AppDbContext _db;

    public ReportAuditRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(ReportAuditLog entry, CancellationToken ct)
    {
        await _db.ReportAuditLogs.AddAsync(entry, ct);
        await _db.SaveChangesAsync(ct);
    }

    public Task StageAsync(ReportAuditLog entry, CancellationToken ct) =>
        _db.ReportAuditLogs.AddAsync(entry, ct).AsTask();

    public async Task<IReadOnlyList<ReportAuditLog>> ListByReportIdAsync(Guid reportId, CancellationToken ct) =>
        await _db.ReportAuditLogs.AsNoTracking()
            .Where(a => a.ReportId == reportId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
}
