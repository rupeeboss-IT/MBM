using Microsoft.EntityFrameworkCore;
using RB_Website_API.Auth;
using RB_Website_API.Data;
using RB_Website_API.DTO;
using RB_Website_API.Models;
using RB_Website_API.Services.IRepository;

namespace RB_Website_API.Services.Repository;

public sealed class VendorManagementRepository : IVendorManagementRepository
{
    private readonly AppDbContext _db;

    public VendorManagementRepository(AppDbContext db) => _db = db;

    public Task<Vendor?> GetByIdAsync(Guid vendorId, bool track, CancellationToken ct)
    {
        var q = track ? _db.Vendors : _db.Vendors.AsNoTracking();
        return q.FirstOrDefaultAsync(v => v.VendorId == vendorId && !v.IsDeleted, ct);
    }

    public async Task AddVendorAsync(Vendor vendor, CancellationToken ct)
    {
        _db.Vendors.Add(vendor);
        await _db.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);

    public async Task<(List<VendorListItemDto> Rows, int Total)> ListVendorsAsync(
        int page,
        int pageSize,
        string? search,
        string? status,
        DateTime? dateFrom,
        DateTime? dateToExclusive,
        string sortField,
        bool sortAsc,
        CancellationToken ct)
    {
        var q = _db.Vendors.AsNoTracking().Where(v => !v.IsDeleted);

        var statusFilter = AdminListQuery.NormalizeStatus(status);
        if (statusFilter == "active") q = q.Where(v => v.IsActive);
        else if (statusFilter == "inactive") q = q.Where(v => !v.IsActive);

        if (dateFrom.HasValue) q = q.Where(v => v.CreatedAt >= dateFrom.Value);
        if (dateToExclusive.HasValue) q = q.Where(v => v.CreatedAt < dateToExclusive.Value);

        if (search is not null)
        {
            var s = search.ToLowerInvariant();
            q = q.Where(v =>
                v.ServiceName.ToLower().Contains(s)
                || v.CompanyName.ToLower().Contains(s)
                || v.ContactPersonName.ToLower().Contains(s)
                || v.Mobile.Contains(s)
                || (v.AlternateMobile ?? "").Contains(s)
                || v.Email.ToLower().Contains(s)
                || (v.AlternateEmail ?? "").ToLower().Contains(s));
        }

        var total = await q.CountAsync(ct);
        var sorted = ApplySort(q, sortField, sortAsc);
        var vendorIds = await sorted
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => v.VendorId)
            .ToListAsync(ct);

        var planLookup = await (
            from m in _db.VendorPlanMappings.AsNoTracking()
            join p in _db.Plans.AsNoTracking() on m.PlanId equals p.PlanId
            where vendorIds.Contains(m.VendorId)
            select new { m.VendorId, p.Name }
        ).ToListAsync(ct);

        var planNamesByVendor = planLookup
            .GroupBy(x => x.VendorId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(x => x.Name).OrderBy(n => n).ToList());

        var vendors = await _db.Vendors.AsNoTracking()
            .Where(v => vendorIds.Contains(v.VendorId))
            .ToListAsync(ct);

        var orderMap = vendorIds.Select((id, idx) => (id, idx)).ToDictionary(x => x.id, x => x.idx);
        var rows = vendors
            .OrderBy(v => orderMap[v.VendorId])
            .Select(v => new VendorListItemDto(
                v.VendorId,
                v.ServiceName,
                v.CompanyName,
                v.ContactPersonName,
                v.Mobile,
                v.AlternateMobile,
                v.Email,
                v.AlternateEmail,
                planNamesByVendor.TryGetValue(v.VendorId, out var names) ? names : Array.Empty<string>(),
                v.IsActive,
                v.CreatedAt,
                v.UpdatedAt))
            .ToList();

        return (rows, total);
    }

    public async Task<VendorDetailDto?> GetDetailAsync(Guid vendorId, CancellationToken ct)
    {
        var v = await _db.Vendors.AsNoTracking()
            .FirstOrDefaultAsync(x => x.VendorId == vendorId && !x.IsDeleted, ct);
        if (v is null) return null;

        var plans = await (
            from m in _db.VendorPlanMappings.AsNoTracking()
            join p in _db.Plans.AsNoTracking() on m.PlanId equals p.PlanId
            where m.VendorId == vendorId
            orderby p.Name
            select new VendorPlanDto(p.PlanId, p.Code, p.Name)
        ).ToListAsync(ct);

        return new VendorDetailDto(
            v.VendorId,
            v.ServiceName,
            v.CompanyName,
            v.ContactPersonName,
            v.Mobile,
            v.AlternateMobile,
            v.Email,
            v.AlternateEmail,
            v.Website,
            v.Address,
            v.City,
            v.State,
            v.Country,
            v.Pincode,
            v.Remarks,
            v.IsActive,
            v.IsDeleted,
            v.DeletedAt,
            v.CreatedAt,
            v.UpdatedAt,
            plans);
    }

    public async Task<VendorManagementStatsDto> GetStatsAsync(CancellationToken ct)
    {
        var vendors = _db.Vendors.AsNoTracking().Where(v => !v.IsDeleted);
        var total = await vendors.CountAsync(ct);
        var active = await vendors.CountAsync(v => v.IsActive, ct);
        var inactive = total - active;

        var assigned = await (
            from m in _db.VendorPlanMappings.AsNoTracking()
            join v in _db.Vendors.AsNoTracking() on m.VendorId equals v.VendorId
            where !v.IsDeleted
            select m.VendorId
        ).Distinct().CountAsync(ct);

        return new VendorManagementStatsDto(total, active, inactive, assigned);
    }

  public async Task<IReadOnlyList<VendorPlanDto>> ListActivePlansAsync(CancellationToken ct)
    {
        var plans = await _db.Plans.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new VendorPlanDto(p.PlanId, p.Code, p.Name))
            .ToListAsync(ct);
        return plans;
    }

    public async Task<IReadOnlyList<int>> GetVendorPlanIdsAsync(Guid vendorId, CancellationToken ct)
    {
        var ids = await _db.VendorPlanMappings.AsNoTracking()
            .Where(m => m.VendorId == vendorId)
            .Select(m => m.PlanId)
            .ToListAsync(ct);
        return ids;
    }

    public async Task SetVendorPlansAsync(Guid vendorId, IReadOnlyList<int> planIds, Guid actorId, CancellationToken ct)
    {
        var existing = await _db.VendorPlanMappings
            .Where(m => m.VendorId == vendorId)
            .ToListAsync(ct);

        if (existing.Count > 0)
        {
            _db.VendorPlanMappings.RemoveRange(existing);
        }

        var now = DateTime.Now;
        foreach (var planId in planIds.Distinct())
        {
            _db.VendorPlanMappings.Add(new VendorPlanMapping
            {
                Id = Guid.NewGuid(),
                VendorId = vendorId,
                PlanId = planId,
                AssignedAt = now,
                AssignedByUserId = actorId,
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task AddAuditLogAsync(VendorAuditLog log, CancellationToken ct)
    {
        _db.VendorAuditLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<(List<VendorAuditLogDto> Rows, int Total)> ListAuditLogsAsync(
        Guid vendorId, int page, int pageSize, CancellationToken ct)
    {
        var q = _db.VendorAuditLogs.AsNoTracking().Where(a => a.VendorId == vendorId);
        var total = await q.CountAsync(ct);
        var rows = await q
            .OrderByDescending(a => a.PerformedOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .GroupJoin(
                _db.Users.AsNoTracking(),
                a => a.PerformedByUserId,
                u => u.UserId,
                (a, users) => new { a, users })
            .SelectMany(
                x => x.users.DefaultIfEmpty(),
                (x, u) => new VendorAuditLogDto(
                    x.a.Id,
                    x.a.VendorId,
                    x.a.Action,
                    x.a.PerformedByUserId,
                    u != null ? u.FullName : null,
                    x.a.PerformedOn,
                    x.a.PreviousValues,
                    x.a.NewValues,
                    x.a.Remarks))
            .ToListAsync(ct);

        return (rows, total);
    }

    public async Task<IReadOnlyList<VendorPlanMappingGroupDto>> ListPlanMappingsAsync(CancellationToken ct)
    {
        var plans = await _db.Plans.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

        var mappings = await (
            from m in _db.VendorPlanMappings.AsNoTracking()
            join v in _db.Vendors.AsNoTracking() on m.VendorId equals v.VendorId
            where !v.IsDeleted
            select new
            {
                m.PlanId,
                v.VendorId,
                v.ServiceName,
                v.CompanyName,
                v.IsActive,
            }
        ).ToListAsync(ct);

        return plans.Select(p => new VendorPlanMappingGroupDto(
            p.PlanId,
            p.Code,
            p.Name,
            mappings
                .Where(m => m.PlanId == p.PlanId)
                .OrderBy(m => m.ServiceName)
                .Select(m => new VendorPlanMappingItemDto(m.VendorId, m.ServiceName, m.CompanyName, m.IsActive))
                .ToList()))
            .ToList();
    }

    private static IQueryable<Vendor> ApplySort(IQueryable<Vendor> q, string sortField, bool sortAsc) =>
        sortField switch
        {
            "service" => sortAsc ? q.OrderBy(v => v.ServiceName) : q.OrderByDescending(v => v.ServiceName),
            "company" => sortAsc ? q.OrderBy(v => v.CompanyName) : q.OrderByDescending(v => v.CompanyName),
            "contact" => sortAsc ? q.OrderBy(v => v.ContactPersonName) : q.OrderByDescending(v => v.ContactPersonName),
            "status" => sortAsc ? q.OrderBy(v => v.IsActive) : q.OrderByDescending(v => v.IsActive),
            _ => sortAsc ? q.OrderBy(v => v.CreatedAt) : q.OrderByDescending(v => v.CreatedAt),
        };
}
