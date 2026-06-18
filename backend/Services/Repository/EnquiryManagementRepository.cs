using Microsoft.EntityFrameworkCore;
using RB_Website_API.Auth;
using RB_Website_API.Data;
using RB_Website_API.DTO;
using RB_Website_API.Models;
using RB_Website_API.Services.IRepository;

namespace RB_Website_API.Services.Repository;

public sealed class EnquiryManagementRepository : IEnquiryManagementRepository
{
    private readonly AppDbContext _db;

    public EnquiryManagementRepository(AppDbContext db) => _db = db;

    public async Task<EnquiryManagementStatsDto> GetStatsAsync(CancellationToken ct)
    {
        var counts = await _db.ContactSubmissions.AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                New = g.Count(x => x.Status == EnquiryStatuses.New),
                Read = g.Count(x => x.Status == EnquiryStatuses.Read),
                InProgress = g.Count(x => x.Status == EnquiryStatuses.InProgress),
                Resolved = g.Count(x => x.Status == EnquiryStatuses.Resolved),
                Closed = g.Count(x => x.Status == EnquiryStatuses.Closed),
            })
            .FirstOrDefaultAsync(ct);

        if (counts is null)
            return new EnquiryManagementStatsDto(0, 0, 0, 0, 0, 0);

        return new EnquiryManagementStatsDto(
            counts.Total,
            counts.New,
            counts.Read,
            counts.InProgress,
            counts.Resolved,
            counts.Closed);
    }

    public async Task<(List<EnquiryListItemDto> Rows, int Total)> ListEnquiriesAsync(
        int page,
        int pageSize,
        string? search,
        string? status,
        string? source,
        DateTime? dateFrom,
        DateTime? dateToExclusive,
        string sortField,
        bool sortAsc,
        CancellationToken ct)
    {
        var q = _db.ContactSubmissions.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = EnquiryStatuses.Normalize(status);
            q = q.Where(x => x.Status == normalizedStatus);
        }

        if (!string.IsNullOrWhiteSpace(source))
        {
            var normalizedSource = EnquirySources.Normalize(source);
            q = q.Where(x => x.Source == normalizedSource);
        }

        if (dateFrom.HasValue) q = q.Where(x => x.CreatedAt >= dateFrom.Value);
        if (dateToExclusive.HasValue) q = q.Where(x => x.CreatedAt < dateToExclusive.Value);

        if (search is not null)
        {
            var s = search.ToLowerInvariant();
            q = q.Where(x =>
                x.FullName.ToLower().Contains(s)
                || x.Phone.Contains(s)
                || x.Email.ToLower().Contains(s)
                || (x.CompanyName ?? "").ToLower().Contains(s));
        }

        var total = await q.CountAsync(ct);
        var sorted = ApplySort(q, sortField, sortAsc);
        var items = await sorted
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var assigneeIds = items
            .Where(x => x.AssignedToUserId.HasValue)
            .Select(x => x.AssignedToUserId!.Value)
            .Distinct()
            .ToList();

        var assigneeNames = assigneeIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.Users.AsNoTracking()
                .Where(u => assigneeIds.Contains(u.UserId))
                .Select(u => new { u.UserId, Name = u.FullName ?? u.Email })
                .ToDictionaryAsync(x => x.UserId, x => x.Name, ct);

        var rows = items.Select(x => new EnquiryListItemDto(
            x.Id,
            x.FullName,
            x.CompanyName,
            x.Phone,
            x.Email,
            ResolveDisplaySource(x),
            ContactSubjectCatalog.GetLabel(x.SubjectId),
            x.CreatedAt,
            EnquiryStatuses.Normalize(x.Status),
            x.AssignedToUserId.HasValue && assigneeNames.TryGetValue(x.AssignedToUserId.Value, out var name)
                ? name
                : null,
            x.Status == EnquiryStatuses.New))
            .ToList();

        return (rows, total);
    }

    public async Task<EnquiryDetailDto?> GetDetailAsync(int enquiryId, CancellationToken ct)
    {
        var x = await _db.ContactSubmissions.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == enquiryId, ct);
        if (x is null) return null;

        string? assignedToName = null;
        if (x.AssignedToUserId.HasValue)
        {
            assignedToName = await _db.Users.AsNoTracking()
                .Where(u => u.UserId == x.AssignedToUserId.Value)
                .Select(u => u.FullName ?? u.Email)
                .FirstOrDefaultAsync(ct);
        }

        var history = await (
            from h in _db.EnquiryStatusHistories.AsNoTracking()
            where h.ContactSubmissionId == enquiryId
            orderby h.ChangedOn descending
            select h
        ).ToListAsync(ct);

        var changerIds = history.Select(h => h.ChangedByUserId).Distinct().ToList();
        var changerNames = changerIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.Users.AsNoTracking()
                .Where(u => changerIds.Contains(u.UserId))
                .Select(u => new { u.UserId, Name = u.FullName ?? u.Email })
                .ToDictionaryAsync(x => x.UserId, x => x.Name, ct);

        var historyDtos = history.Select(h => new EnquiryStatusHistoryDto(
            h.Id,
            h.OldStatus,
            h.NewStatus,
            h.ChangedByUserId,
            changerNames.TryGetValue(h.ChangedByUserId, out var n) ? n : null,
            h.ChangedOn,
            h.Remarks)).ToList();

        return new EnquiryDetailDto(
            x.Id,
            x.FullName,
            x.CompanyName,
            x.Phone,
            x.Email,
            ContactSubjectCatalog.GetLabel(x.SubjectId),
            x.Message,
            ResolveDisplaySource(x),
            x.CreatedAt,
            EnquiryStatuses.Normalize(x.Status),
            assignedToName,
            historyDtos);
    }

    public Task<ContactSubmission?> GetByIdAsync(int enquiryId, bool track, CancellationToken ct)
    {
        var q = track ? _db.ContactSubmissions : _db.ContactSubmissions.AsNoTracking();
        return q.FirstOrDefaultAsync(x => x.Id == enquiryId, ct);
    }

    public async Task AddStatusHistoryAsync(EnquiryStatusHistory entry, CancellationToken ct)
    {
        _db.EnquiryStatusHistories.Add(entry);
        await _db.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);

    private static string ResolveDisplaySource(ContactSubmission x)
    {
        if (!string.IsNullOrWhiteSpace(x.Source) && x.Source != EnquirySources.OtherPages)
            return EnquirySources.Normalize(x.Source);

        if (string.IsNullOrWhiteSpace(x.Email) && x.SubjectId == 3)
            return EnquirySources.SchemeDiscoveryPage;

        if (!string.IsNullOrWhiteSpace(x.Email))
            return EnquirySources.ContactUsPage;

        return EnquirySources.OtherPages;
    }

    private static IQueryable<ContactSubmission> ApplySort(IQueryable<ContactSubmission> q, string sortField, bool sortAsc)
    {
        return sortField switch
        {
            "name" => sortAsc ? q.OrderBy(x => x.FullName) : q.OrderByDescending(x => x.FullName),
            "company" => sortAsc
                ? q.OrderBy(x => x.CompanyName ?? "")
                : q.OrderByDescending(x => x.CompanyName ?? ""),
            "source" => sortAsc ? q.OrderBy(x => x.Source) : q.OrderByDescending(x => x.Source),
            "status" => sortAsc ? q.OrderBy(x => x.Status) : q.OrderByDescending(x => x.Status),
            "subject" => sortAsc ? q.OrderBy(x => x.SubjectId) : q.OrderByDescending(x => x.SubjectId),
            "oldest" => q.OrderBy(x => x.CreatedAt),
            _ => q.OrderByDescending(x => x.CreatedAt),
        };
    }
}
