using Microsoft.EntityFrameworkCore;
using RB_Website_API.Auth;
using RB_Website_API.Data;
using RB_Website_API.DTO;
using RB_Website_API.Models;
using RB_Website_API.Services.IRepository;

namespace RB_Website_API.Services.Repository;

public sealed class UserManagementRepository : IUserManagementRepository
{
    private readonly AppDbContext _db;

    public UserManagementRepository(AppDbContext db) => _db = db;

    public Task<User?> GetByIdAsync(Guid userId, bool track, CancellationToken ct)
    {
        var q = track ? _db.Users : _db.Users.AsNoTracking();
        return q.FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted, ct);
    }

    public Task<bool> EmailExistsAsync(string email, Guid? excludeUserId, CancellationToken ct)
    {
        var q = _db.Users.AsNoTracking().Where(u => u.Email == email && !u.IsDeleted);
        if (excludeUserId.HasValue)
            q = q.Where(u => u.UserId != excludeUserId.Value);
        return q.AnyAsync(ct);
    }

    public Task<bool> PhoneExistsAsync(string phone, Guid? excludeUserId, CancellationToken ct)
    {
        var q = _db.Users.AsNoTracking().Where(u => u.Phone == phone && !u.IsDeleted);
        if (excludeUserId.HasValue)
            q = q.Where(u => u.UserId != excludeUserId.Value);
        return q.AnyAsync(ct);
    }

    public async Task AddUserAsync(User user, CancellationToken ct)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);

    public async Task<(List<ManagedUserDto> Rows, int Total)> ListUsersAsync(
        string role,
        bool isSuperAdmin,
        Guid actorId,
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
        var q = _db.Users.AsNoTracking()
            .Where(u => !u.IsDeleted && (u.Role ?? "").ToLower() == role);

        if (!isSuperAdmin && UserRoles.IsMemberRole(role))
            q = q.Where(u => u.CreatedByUserId == null || u.CreatedByUserId == actorId);

        var statusFilter = AdminListQuery.NormalizeStatus(status);
        if (statusFilter == "active") q = q.Where(u => u.IsActive);
        else if (statusFilter == "inactive") q = q.Where(u => !u.IsActive);

        if (dateFrom.HasValue) q = q.Where(u => u.CreatedAt >= dateFrom.Value);
        if (dateToExclusive.HasValue) q = q.Where(u => u.CreatedAt < dateToExclusive.Value);

        if (search is not null)
        {
            var s = search.ToLowerInvariant();
            q = q.Where(u =>
                u.FullName.ToLower().Contains(s)
                || u.Email.ToLower().Contains(s)
                || u.Phone.Contains(s)
                || (u.CompanyName ?? "").ToLower().Contains(s)
                || (u.MemberId ?? "").ToLower().Contains(s));
        }

        var total = await q.CountAsync(ct);
        var sorted = AdminUserSort.Apply(q, sortField, sortAsc);
        var rows = await sorted
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new ManagedUserDto(
                u.UserId,
                u.Role,
                u.FullName,
                u.Email,
                u.Phone,
                u.CompanyName,
                u.MemberId,
                u.IsActive,
                u.IsDeleted,
                u.CreatedAt,
                u.UpdatedAt,
                u.CreatedByUserId,
                u.CreatedByUserId == null
                    ? null
                    : _db.Users.Where(c => c.UserId == u.CreatedByUserId).Select(c => c.FullName).FirstOrDefault()))
            .ToListAsync(ct);

        return (rows, total);
    }

    public async Task<UserManagementStatsDto> GetStatsAsync(bool isSuperAdmin, Guid actorId, CancellationToken ct)
    {
        var q = _db.Users.AsNoTracking().Where(u => !u.IsDeleted);

        if (!isSuperAdmin)
            q = q.Where(u =>
                (u.Role ?? "").ToLower() == UserRoles.Partner
                || (u.Role ?? "").ToLower() == UserRoles.Member);

        async Task<(int Total, int Active, int Inactive)> CountRole(string role, bool restrictToActor)
        {
            var roleQ = q.Where(u => (u.Role ?? "").ToLower() == role);
            if (restrictToActor && !isSuperAdmin)
                roleQ = roleQ.Where(u => u.CreatedByUserId == null || u.CreatedByUserId == actorId);

            var total = await roleQ.CountAsync(ct);
            var active = await roleQ.CountAsync(u => u.IsActive, ct);
            return (total, active, total - active);
        }

        (int Total, int Active, int Inactive) admins = isSuperAdmin
            ? await CountRole(UserRoles.Admin, restrictToActor: false)
            : (0, 0, 0);

        var partners = await CountRole(UserRoles.Partner, restrictToActor: !isSuperAdmin);
        var members = await CountRole(UserRoles.Member, restrictToActor: !isSuperAdmin);

        return new UserManagementStatsDto(
            admins.Total,
            admins.Active,
            admins.Inactive,
            partners.Total,
            partners.Active,
            partners.Inactive,
            members.Total,
            members.Active,
            members.Inactive);
    }

    public async Task<ManagedUserDetailDto?> GetDetailAsync(Guid userId, CancellationToken ct)
    {
        var now = DateTime.Now;
        return await (
            from u in _db.Users.AsNoTracking()
            where u.UserId == userId && !u.IsDeleted
            select new ManagedUserDetailDto(
                u.UserId,
                u.Role,
                u.FullName,
                u.Email,
                u.Phone,
                u.CompanyName,
                u.MemberId,
                u.IsActive,
                u.IsDeleted,
                u.DeletedAt,
                u.CreatedAt,
                u.UpdatedAt,
                u.CreatedByUserId,
                u.CreatedByUserId == null
                    ? null
                    : _db.Users.Where(c => c.UserId == u.CreatedByUserId).Select(c => c.FullName).FirstOrDefault(),
                _db.UserPlans
                    .Where(up => up.UserId == u.UserId && up.Status == "Active" && (up.ActiveTo == null || up.ActiveTo > now))
                    .OrderByDescending(up => up.ActiveFrom)
                    .Select(up => up.PlanCode)
                    .FirstOrDefault(),
                (from up in _db.UserPlans
                 join p in _db.Plans on up.PlanId equals p.PlanId
                 where up.UserId == u.UserId && up.Status == "Active" && (up.ActiveTo == null || up.ActiveTo > now)
                 orderby up.ActiveFrom descending
                 select p.Name).FirstOrDefault()))
            .FirstOrDefaultAsync(ct);
    }

    public async Task AddAuditLogAsync(UserAuditLog entry, CancellationToken ct)
    {
        _db.UserAuditLog.Add(entry);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddStatusHistoryAsync(UserStatusHistory entry, CancellationToken ct)
    {
        _db.UserStatusHistory.Add(entry);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<(List<UserAuditLogDto> Rows, int Total)> ListAuditLogsAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var q = _db.UserAuditLog.AsNoTracking().Where(a => a.UserId == userId);
        var total = await q.CountAsync(ct);
        var rows = await q
            .OrderByDescending(a => a.PerformedOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new UserAuditLogDto(
                a.Id,
                a.UserId,
                a.UserType,
                a.Action,
                a.PerformedByUserId,
                _db.Users.Where(u => u.UserId == a.PerformedByUserId).Select(u => u.FullName).FirstOrDefault(),
                a.PerformedOn,
                a.PreviousValues,
                a.NewValues,
                a.Remarks))
            .ToListAsync(ct);
        return (rows, total);
    }

    public async Task<(List<UserStatusHistoryDto> Rows, int Total)> ListStatusHistoryAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var q = _db.UserStatusHistory.AsNoTracking().Where(h => h.UserId == userId);
        var total = await q.CountAsync(ct);
        var rows = await q
            .OrderByDescending(h => h.PerformedOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new UserStatusHistoryDto(
                h.Id,
                h.UserId,
                h.UserType,
                h.ActionType,
                h.OldStatus,
                h.NewStatus,
                h.Remarks,
                h.PerformedByUserId,
                _db.Users.Where(u => u.UserId == h.PerformedByUserId).Select(u => u.FullName).FirstOrDefault(),
                h.PerformedOn))
            .ToListAsync(ct);
        return (rows, total);
    }

    public Task<string?> GetUserDisplayNameAsync(Guid userId, CancellationToken ct) =>
        _db.Users.AsNoTracking()
            .Where(u => u.UserId == userId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(ct);
}
