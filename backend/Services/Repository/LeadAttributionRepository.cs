using Microsoft.EntityFrameworkCore;
using RB_Website_API.Auth;
using RB_Website_API.Data;
using RB_Website_API.Models;
using RB_Website_API.Services.IRepository;

namespace RB_Website_API.Services.Repository;

public sealed class LeadAttributionRepository : ILeadAttributionRepository
{
    private readonly AppDbContext _db;

    public LeadAttributionRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Guid>> GetScopedPartnerIdsAsync(Guid actorId, CancellationToken ct)
    {
        var ids = await _db.Users.AsNoTracking()
            .Where(u => !u.IsDeleted && u.Role == UserRoles.Partner && u.CreatedByUserId == actorId)
            .Select(u => u.UserId)
            .ToListAsync(ct);
        return ids;
    }

    public Task<List<User>> ListScopedMembersAsync(
        bool isSuperAdmin,
        Guid actorId,
        IReadOnlyList<Guid> partnerIds,
        CancellationToken ct)
    {
        var q = _db.Users.AsNoTracking()
            .Where(u => !u.IsDeleted && u.Role == UserRoles.Member);

        if (!isSuperAdmin)
        {
            q = q.Where(u =>
                u.CreatedByUserId == null
                || u.CreatedByUserId == actorId
                || (u.CreatedByUserId != null && partnerIds.Contains(u.CreatedByUserId.Value)));
        }

        return q.OrderByDescending(u => u.CreatedAt).ToListAsync(ct);
    }

    public async Task<Dictionary<Guid, int>> GetReportCountsAsync(IReadOnlyList<Guid> userIds, CancellationToken ct)
    {
        if (userIds.Count == 0) return new Dictionary<Guid, int>();

        var rows = await _db.CustomerReports.AsNoTracking()
            .Where(r => userIds.Contains(r.CustomerId) && r.IsActive)
            .GroupBy(r => r.CustomerId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return rows.ToDictionary(x => x.Key, x => x.Count);
    }

    public async Task<Dictionary<Guid, (string? PlanCode, string? PlanName, string Status)>> GetActivePlansAsync(
        IReadOnlyList<Guid> userIds,
        DateTime now,
        CancellationToken ct)
    {
        if (userIds.Count == 0) return new Dictionary<Guid, (string?, string?, string)>();

        var rows = await (
            from up in _db.UserPlans.AsNoTracking()
            join p in _db.Plans.AsNoTracking() on up.PlanId equals p.PlanId
            where userIds.Contains(up.UserId)
                  && up.Status == "Active"
                  && (up.ActiveTo == null || up.ActiveTo > now)
            orderby up.ActiveFrom descending
            select new { up.UserId, up.PlanCode, p.Name, up.Status }
        ).ToListAsync(ct);

        return rows
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var first = g.First();
                    return ((string?)first.PlanCode, (string?)first.Name, first.Status);
                });
    }

    public async Task<Dictionary<Guid, int>> GetMembershipSalesCountsAsync(IReadOnlyList<Guid> userIds, CancellationToken ct)
    {
        if (userIds.Count == 0) return new Dictionary<Guid, int>();

        var reportOnlyCode = SchemeDiscoveryCatalog.OneTimePlanCode;
        var rows = await _db.PaymentOrders.AsNoTracking()
            .Where(po => userIds.Contains(po.UserId)
                         && po.Status == "Paid"
                         && po.PlanCode != reportOnlyCode)
            .GroupBy(po => po.UserId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return rows.ToDictionary(x => x.Key, x => x.Count);
    }

    public async Task<Dictionary<Guid, int>> GetReportPurchaseCountsAsync(IReadOnlyList<Guid> userIds, CancellationToken ct)
    {
        if (userIds.Count == 0) return new Dictionary<Guid, int>();

        var reportOnlyCode = SchemeDiscoveryCatalog.OneTimePlanCode;
        var rows = await _db.PaymentOrders.AsNoTracking()
            .Where(po => userIds.Contains(po.UserId)
                         && po.Status == "Paid"
                         && po.PlanCode == reportOnlyCode)
            .GroupBy(po => po.UserId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return rows.ToDictionary(x => x.Key, x => x.Count);
    }

    public async Task<Dictionary<Guid, FirstReferralRow>> GetFirstPaidReferralsAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken ct)
    {
        if (userIds.Count == 0) return new Dictionary<Guid, FirstReferralRow>();

        var paidOrders = await (
            from po in _db.PaymentOrders.AsNoTracking()
            join p in _db.Plans.AsNoTracking() on po.PlanId equals p.PlanId into planJoin
            from p in planJoin.DefaultIfEmpty()
            join por in _db.PaymentOrderReferrals.AsNoTracking() on po.PaymentOrderId equals por.PaymentOrderId into porJoin
            from por in porJoin.DefaultIfEmpty()
            where userIds.Contains(po.UserId) && po.Status == "Paid"
            orderby po.CreatedAt
            select new
            {
                po.UserId,
                po.PaymentOrderId,
                ReferralCode = por != null ? por.ReferralCode : null,
                po.CreatedAt,
                po.PlanCode,
                PlanName = p != null ? p.Name : po.PlanCode,
            }
        ).ToListAsync(ct);

        return paidOrders
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var first = g.OrderBy(x => x.CreatedAt).First();
                    return new FirstReferralRow(
                        first.PaymentOrderId,
                        first.ReferralCode,
                        first.CreatedAt,
                        first.PlanCode,
                        first.PlanName);
                });
    }

    public async Task<Dictionary<Guid, RegistrationAdvisorRow>> GetRegistrationAdvisorsAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken ct)
    {
        if (userIds.Count == 0) return new Dictionary<Guid, RegistrationAdvisorRow>();

        var rows = await _db.UserRegistrationLeads.AsNoTracking()
            .Where(r => userIds.Contains(r.UserId) && r.LeadPushedAt != default)
            .Select(r => new
            {
                r.UserId,
                r.AdvisorCode,
                r.ResolvedEmpCode,
                r.LeadType,
                r.BrokerId,
                r.UsedDefaultEmployee,
            })
            .ToListAsync(ct);

        return rows.ToDictionary(
            x => x.UserId,
            x => new RegistrationAdvisorRow(
                x.AdvisorCode,
                x.ResolvedEmpCode,
                x.LeadType,
                x.BrokerId,
                x.UsedDefaultEmployee));
    }

    public async Task<IReadOnlyList<PaymentHistoryRow>> GetPaymentHistoryAsync(Guid userId, CancellationToken ct)
    {
        var rows = await (
            from po in _db.PaymentOrders.AsNoTracking()
            join p in _db.Plans.AsNoTracking() on po.PlanId equals p.PlanId into planJoin
            from p in planJoin.DefaultIfEmpty()
            join por in _db.PaymentOrderReferrals.AsNoTracking() on po.PaymentOrderId equals por.PaymentOrderId into porJoin
            from por in porJoin.DefaultIfEmpty()
            where po.UserId == userId && po.Status == "Paid"
            orderby po.CreatedAt descending
            select new PaymentHistoryRow(
                po.PaymentOrderId,
                po.PlanCode,
                p != null ? p.Name : po.PlanCode,
                po.CreatedAt,
                por != null ? por.ReferralCode : null,
                po.TotalAmountPaise)
        ).ToListAsync(ct);

        return rows;
    }

    public async Task<IReadOnlyList<ReferralPaymentRow>> GetReferralPaymentsForUsersAsync(
        IReadOnlyList<Guid> userIds,
        DateTime? paidFrom,
        DateTime? paidToExclusive,
        CancellationToken ct)
    {
        if (userIds.Count == 0) return Array.Empty<ReferralPaymentRow>();

        var q =
            from po in _db.PaymentOrders.AsNoTracking()
            join u in _db.Users.AsNoTracking() on po.UserId equals u.UserId
            join p in _db.Plans.AsNoTracking() on po.PlanId equals p.PlanId into planJoin
            from p in planJoin.DefaultIfEmpty()
            join por in _db.PaymentOrderReferrals.AsNoTracking() on po.PaymentOrderId equals por.PaymentOrderId
            where userIds.Contains(po.UserId)
                  && po.Status == "Paid"
                  && por.ReferralCode != null
                  && por.ReferralCode != ""
            select new { po, u, p, por };

        if (paidFrom.HasValue)
            q = q.Where(x => x.po.CreatedAt >= paidFrom.Value);
        if (paidToExclusive.HasValue)
            q = q.Where(x => x.po.CreatedAt < paidToExclusive.Value);

        var rows = await q
            .OrderByDescending(x => x.po.CreatedAt)
            .Select(x => new ReferralPaymentRow(
                x.po.PaymentOrderId,
                x.po.UserId,
                x.u.MemberId ?? "",
                x.u.FullName,
                x.po.PlanCode,
                x.p != null ? x.p.Name : x.po.PlanCode,
                x.po.CreatedAt,
                x.por.ReferralCode!,
                x.po.TotalAmountPaise))
            .ToListAsync(ct);

        return rows;
    }

    public async Task<Dictionary<Guid, User>> GetUsersByIdsAsync(IReadOnlyList<Guid> userIds, CancellationToken ct)
    {
        if (userIds.Count == 0) return new Dictionary<Guid, User>();

        var users = await _db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.UserId))
            .ToListAsync(ct);

        return users.ToDictionary(u => u.UserId);
    }

    public Task<User?> GetMemberAsync(Guid userId, CancellationToken ct) =>
        _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted && u.Role == UserRoles.Member, ct);
}
