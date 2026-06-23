using Microsoft.EntityFrameworkCore;
using RB_Website_API.Auth;
using RB_Website_API.Data;
using RB_Website_API.DTO;
using RB_Website_API.Models;
using RB_Website_API.Services.IRepository;

namespace RB_Website_API.Services.Repository;

public sealed class ConnectRepository : IConnectRepository
{
    private readonly AppDbContext _db;

    public ConnectRepository(AppDbContext db) => _db = db;

    public async Task<(IReadOnlyList<ConnectListingRow> Rows, int Total)> SearchListedAsync(
        int page,
        int pageSize,
        string? search,
        string? sector,
        string? state,
        string? turnover,
        Guid? excludeUserId,
        CancellationToken ct)
    {
        var q = from listing in _db.ConnectAdminListings.AsNoTracking()
                join user in _db.Users.AsNoTracking() on listing.UserId equals user.UserId
                join member in _db.ConnectMemberProfiles.AsNoTracking() on listing.UserId equals member.UserId into mj
                from member in mj.DefaultIfEmpty()
                where listing.IsListed && listing.IsActive && !user.IsDeleted && user.IsActive
                      && user.Role != null
                      && (user.Role.ToLower() == UserRoles.Member || user.Role.ToLower() == UserRoles.Partner)
                select new { listing, user, member };

        if (excludeUserId.HasValue && excludeUserId.Value != Guid.Empty)
            q = q.Where(x => x.listing.UserId != excludeUserId.Value);

        if (!string.IsNullOrWhiteSpace(sector))
        {
            var s = sector.Trim();
            q = q.Where(x =>
                (x.member != null && x.member.Sector == s)
                || (x.member == null || x.member.Sector == null || x.member.Sector == "") && x.listing.Sector == s
                || (x.member != null && (x.member.Sector == null || x.member.Sector == "") && x.listing.Sector == s));
        }

        if (!string.IsNullOrWhiteSpace(state))
        {
            var s = state.Trim();
            q = q.Where(x =>
                (x.member != null && x.member.State == s)
                || ((x.member == null || x.member.State == null || x.member.State == "") && x.listing.State == s));
        }

        if (!string.IsNullOrWhiteSpace(turnover))
        {
            var t = turnover.Trim();
            q = q.Where(x =>
                (x.member != null && x.member.Turnover == t)
                || ((x.member == null || x.member.Turnover == null || x.member.Turnover == "") && x.listing.Turnover == t));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            q = q.Where(x =>
                x.user.FullName.ToLower().Contains(s)
                || (x.user.CompanyName ?? "").ToLower().Contains(s)
                || (x.listing.Sector ?? "").ToLower().Contains(s)
                || (x.listing.City ?? "").ToLower().Contains(s)
                || (x.listing.Description ?? "").ToLower().Contains(s)
                || (x.member != null && (
                    (x.member.Sector ?? "").ToLower().Contains(s)
                    || (x.member.City ?? "").ToLower().Contains(s)
                    || (x.member.Description ?? "").ToLower().Contains(s))));
        }

        var total = await q.CountAsync(ct);
        var rows = await q
            .OrderByDescending(x => x.listing.IsVerified)
            .ThenBy(x => x.user.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ConnectListingRow(x.listing, x.user, x.member))
            .ToListAsync(ct);

        return (rows, total);
    }

    public async Task<ConnectStatsDto> GetStatsAsync(CancellationToken ct)
    {
        var baseQ = _db.ConnectAdminListings.AsNoTracking()
            .Where(l => l.IsListed && l.IsActive);

        var listed = await baseQ.CountAsync(ct);

        var sectorRows = await (
            from l in baseQ
            join m in _db.ConnectMemberProfiles.AsNoTracking() on l.UserId equals m.UserId into mj
            from m in mj.DefaultIfEmpty()
            select new { MemberSector = m.Sector, l.Sector }
        ).ToListAsync(ct);

        var sectors = sectorRows
            .Select(x => ConnectProfileMerger.MergeField(x.MemberSector, x.Sector))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var stateRows = await (
            from l in baseQ
            join m in _db.ConnectMemberProfiles.AsNoTracking() on l.UserId equals m.UserId into mj
            from m in mj.DefaultIfEmpty()
            select new { MemberState = m.State, l.State }
        ).ToListAsync(ct);

        var states = stateRows
            .Select(x => ConnectProfileMerger.MergeField(x.MemberState, x.State))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        return new ConnectStatsDto(listed, sectors, states);
    }

    public async Task<ConnectListingRow?> GetListedByUserIdAsync(Guid userId, CancellationToken ct)
    {
        return await (
            from listing in _db.ConnectAdminListings.AsNoTracking()
            join user in _db.Users.AsNoTracking() on listing.UserId equals user.UserId
            join member in _db.ConnectMemberProfiles.AsNoTracking() on listing.UserId equals member.UserId into mj
            from member in mj.DefaultIfEmpty()
            where listing.UserId == userId && listing.IsListed && listing.IsActive && !user.IsDeleted
            select new ConnectListingRow(listing, user, member)
        ).FirstOrDefaultAsync(ct);
    }

    public Task<ConnectAdminListing?> GetAdminListingByUserIdAsync(Guid userId, bool track, CancellationToken ct)
    {
        var q = track ? _db.ConnectAdminListings : _db.ConnectAdminListings.AsNoTracking();
        return q.FirstOrDefaultAsync(l => l.UserId == userId, ct);
    }

    public Task<ConnectAdminListing?> GetAdminListingByIdAsync(Guid listingId, bool track, CancellationToken ct)
    {
        var q = track ? _db.ConnectAdminListings : _db.ConnectAdminListings.AsNoTracking();
        return q.FirstOrDefaultAsync(l => l.ListingId == listingId, ct);
    }

    public Task<ConnectMemberProfile?> GetMemberProfileAsync(Guid userId, bool track, CancellationToken ct)
    {
        var q = track ? _db.ConnectMemberProfiles : _db.ConnectMemberProfiles.AsNoTracking();
        return q.FirstOrDefaultAsync(m => m.UserId == userId, ct);
    }

    public async Task<(IReadOnlyList<ConnectAdminListingListItemDto> Rows, int Total)> ListAdminListingsAsync(
        int page, int pageSize, string? search, string? role, string? status, CancellationToken ct)
    {
        var q = from listing in _db.ConnectAdminListings.AsNoTracking()
                join user in _db.Users.AsNoTracking() on listing.UserId equals user.UserId
                join member in _db.ConnectMemberProfiles.AsNoTracking() on listing.UserId equals member.UserId into mj
                from member in mj.DefaultIfEmpty()
                where !user.IsDeleted && user.Role != null
                      && (user.Role.ToLower() == UserRoles.Member || user.Role.ToLower() == UserRoles.Partner)
                select new { listing, user, member };

        if (!string.IsNullOrWhiteSpace(role))
        {
            var r = role.Trim().ToLowerInvariant();
            q = q.Where(x => (x.user.Role ?? "").ToLower() == r);
        }

        var statusFilter = (status ?? "").Trim().ToLowerInvariant();
        if (statusFilter == "listed") q = q.Where(x => x.listing.IsListed);
        else if (statusFilter == "unlisted") q = q.Where(x => !x.listing.IsListed);
        else if (statusFilter == "active") q = q.Where(x => x.listing.IsActive);
        else if (statusFilter == "inactive") q = q.Where(x => !x.listing.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            q = q.Where(x =>
                x.user.FullName.ToLower().Contains(s)
                || x.user.Email.ToLower().Contains(s)
                || x.user.Phone.Contains(s)
                || (x.user.CompanyName ?? "").ToLower().Contains(s));
        }

        var total = await q.CountAsync(ct);
        var pageRows = await q
            .OrderByDescending(x => x.listing.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var rows = pageRows.Select(x => new ConnectAdminListingListItemDto(
            x.listing.ListingId,
            x.listing.UserId,
            x.user.Role ?? "",
            x.user.FullName,
            x.user.CompanyName,
            x.user.Phone,
            x.user.Email,
            x.listing.IsListed,
            x.listing.IsVerified,
            x.listing.IsActive,
            ConnectProfileMerger.MergeField(x.member?.Sector, x.listing.Sector),
            ConnectProfileMerger.MergeField(x.member?.City, x.listing.City),
            ConnectProfileMerger.MergeField(x.member?.State, x.listing.State),
            x.listing.CreatedAt,
            x.listing.UpdatedAt)).ToList();

        return (rows, total);
    }

    public async Task AddAdminListingAsync(ConnectAdminListing listing, CancellationToken ct)
    {
        _db.ConnectAdminListings.Add(listing);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddMemberProfileAsync(ConnectMemberProfile profile, CancellationToken ct)
    {
        _db.ConnectMemberProfiles.Add(profile);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<(IReadOnlyList<ConnectUserSearchItemDto> Rows, int Total)> SearchMembersForAdminAsync(
        string? search, string? role, int limit, CancellationToken ct)
    {
        var q = _db.Users.AsNoTracking()
            .Where(u => !u.IsDeleted && u.IsActive && u.Role != null
                && (u.Role.ToLower() == UserRoles.Member || u.Role.ToLower() == UserRoles.Partner));

        if (!string.IsNullOrWhiteSpace(role))
        {
            var r = role.Trim().ToLowerInvariant();
            q = q.Where(u => (u.Role ?? "").ToLower() == r);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            q = q.Where(u =>
                u.FullName.ToLower().Contains(s)
                || u.Email.ToLower().Contains(s)
                || u.Phone.Contains(s)
                || (u.CompanyName ?? "").ToLower().Contains(s)
                || (u.MemberId ?? "").ToLower().Contains(s));
        }

        var total = await q.CountAsync(ct);
        var users = await q.OrderBy(u => u.FullName).Take(limit).ToListAsync(ct);
        var userIds = users.Select(u => u.UserId).ToList();
        var listingIds = await _db.ConnectAdminListings.AsNoTracking()
            .Where(l => userIds.Contains(l.UserId))
            .Select(l => l.UserId)
            .ToListAsync(ct);
        var listingSet = listingIds.ToHashSet();

        var rows = users.Select(u => new ConnectUserSearchItemDto(
            u.UserId,
            u.Role ?? "",
            u.FullName,
            u.CompanyName,
            u.Phone,
            u.Email,
            listingSet.Contains(u.UserId))).ToList();

        return (rows, total);
    }

    public Task<ConnectRequest?> GetRequestBetweenAsync(Guid fromUserId, Guid toUserId, bool track, CancellationToken ct)
    {
        var q = track ? _db.ConnectRequests : _db.ConnectRequests.AsNoTracking();
        return q
            .Where(r =>
                (r.FromUserId == fromUserId && r.ToUserId == toUserId)
                || (r.FromUserId == toUserId && r.ToUserId == fromUserId))
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    public Task<ConnectRequest?> GetRequestByIdAsync(Guid requestId, bool track, CancellationToken ct)
    {
        var q = track ? _db.ConnectRequests : _db.ConnectRequests.AsNoTracking();
        return q.FirstOrDefaultAsync(r => r.RequestId == requestId, ct);
    }

    public async Task AddRequestAsync(ConnectRequest request, CancellationToken ct)
    {
        _db.ConnectRequests.Add(request);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ConnectIncomingRequestDto>> ListIncomingPendingAsync(Guid toUserId, CancellationToken ct)
    {
        return await (
            from r in _db.ConnectRequests.AsNoTracking()
            join u in _db.Users.AsNoTracking() on r.FromUserId equals u.UserId
            where r.ToUserId == toUserId && r.Status == ConnectRequestStatuses.Pending
            orderby r.CreatedAt descending
            select new ConnectIncomingRequestDto(
                r.RequestId,
                r.FromUserId,
                u.FullName,
                u.CompanyName,
                r.Message,
                r.CreatedAt)
        ).ToListAsync(ct);
    }

    public Task<int> CountUnlocksAsync(Guid viewerUserId, CancellationToken ct) =>
        _db.ConnectContactUnlocks.CountAsync(u => u.ViewerUserId == viewerUserId, ct);

    public Task<bool> HasUnlockAsync(Guid viewerUserId, Guid targetUserId, CancellationToken ct) =>
        _db.ConnectContactUnlocks.AnyAsync(
            u => u.ViewerUserId == viewerUserId && u.TargetUserId == targetUserId, ct);

    public async Task AddUnlockAsync(ConnectContactUnlock unlock, CancellationToken ct)
    {
        _db.ConnectContactUnlocks.Add(unlock);
        await _db.SaveChangesAsync(ct);
    }

    public Task<User?> GetMemberUserAsync(Guid userId, CancellationToken ct) =>
        _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(
                u => u.UserId == userId
                     && !u.IsDeleted
                     && u.Role != null
                     && (u.Role.ToLower() == UserRoles.Member || u.Role.ToLower() == UserRoles.Partner), ct);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
