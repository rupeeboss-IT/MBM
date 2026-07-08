using Microsoft.EntityFrameworkCore;
using RB_Website_API.Data;
using RB_Website_API.DTO;
using RB_Website_API.Models;
using RB_Website_API.Services.IRepository;

namespace RB_Website_API.Services.Repository;

public sealed class CreditRepairManagementRepository : ICreditRepairManagementRepository
{
    private readonly AppDbContext _db;

    public CreditRepairManagementRepository(AppDbContext db) => _db = db;

    public async Task<CreditRepairManagementStatsDto> GetStatsAsync(
        DateTime? dateFrom,
        DateTime? dateToExclusive,
        CancellationToken ct)
    {
        var q = ApplyDateFilter(_db.CreditRepairLeads.AsNoTracking(), dateFrom, dateToExclusive);
        var today = DateTime.Now.Date;
        var tomorrow = today.AddDays(1);
        var weekAgo = today.AddDays(-7);

        var counts = await q
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Today = g.Count(x => x.CreatedAt >= today && x.CreatedAt < tomorrow),
                Last7Days = g.Count(x => x.CreatedAt >= weekAgo),
                Linked = g.Count(x => x.LeadId != null),
                Unlinked = g.Count(x => x.LeadId == null),
            })
            .FirstOrDefaultAsync(ct);

        if (counts is null)
            return new CreditRepairManagementStatsDto(0, 0, 0, 0, 0);

        return new CreditRepairManagementStatsDto(
            counts.Total,
            counts.Today,
            counts.Last7Days,
            counts.Linked,
            counts.Unlinked);
    }

    public async Task<IReadOnlyList<CreditRepairSourceBreakdownDto>> GetSourceBreakdownAsync(
        DateTime? dateFrom,
        DateTime? dateToExclusive,
        CancellationToken ct)
    {
        var q = ApplyDateFilter(_db.CreditRepairLeads.AsNoTracking(), dateFrom, dateToExclusive);

        var rows = await q
            .GroupBy(x => x.Source)
            .Select(g => new { Source = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Source)
            .Take(12)
            .ToListAsync(ct);

        return rows.Select(x => new CreditRepairSourceBreakdownDto(x.Source, x.Count)).ToList();
    }

    public async Task<IReadOnlyList<CreditRepairCampaignBreakdownDto>> GetCampaignBreakdownAsync(
        DateTime? dateFrom,
        DateTime? dateToExclusive,
        CancellationToken ct)
    {
        var q = ApplyDateFilter(_db.CreditRepairLeads.AsNoTracking(), dateFrom, dateToExclusive);

        var rows = await q
            .GroupBy(x => x.CampaignName)
            .Select(g => new { CampaignName = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.CampaignName)
            .Take(12)
            .ToListAsync(ct);

        return rows.Select(x => new CreditRepairCampaignBreakdownDto(x.CampaignName, x.Count)).ToList();
    }

    public async Task<(List<CreditRepairListItemDto> Rows, int Total)> ListLeadsAsync(
        int page,
        int pageSize,
        string? search,
        string? source,
        string? campaign,
        string? linkStatus,
        DateTime? dateFrom,
        DateTime? dateToExclusive,
        string sortField,
        bool sortAsc,
        CancellationToken ct)
    {
        var q = ApplyListFilters(
            _db.CreditRepairLeads.AsNoTracking(),
            search,
            source,
            campaign,
            linkStatus,
            dateFrom,
            dateToExclusive);

        var total = await q.CountAsync(ct);
        var items = await ApplySort(q, sortField, sortAsc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var rows = items.Select(x => new CreditRepairListItemDto(
            x.Id,
            x.FullName,
            x.Phone,
            x.Email,
            x.Source,
            x.CampaignName,
            x.ConsentAccepted,
            x.CreatedAt,
            x.LeadId)).ToList();

        return (rows, total);
    }

    public async Task<IReadOnlyList<string>> GetDistinctSourcesAsync(CancellationToken ct) =>
        await _db.CreditRepairLeads.AsNoTracking()
            .Select(x => x.Source)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<string>> GetDistinctCampaignsAsync(CancellationToken ct) =>
        await _db.CreditRepairLeads.AsNoTracking()
            .Select(x => x.CampaignName)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(ct);

    private static IQueryable<CreditRepairLead> ApplyDateFilter(
        IQueryable<CreditRepairLead> q,
        DateTime? dateFrom,
        DateTime? dateToExclusive)
    {
        if (dateFrom.HasValue)
            q = q.Where(x => x.CreatedAt >= dateFrom.Value);

        if (dateToExclusive.HasValue)
            q = q.Where(x => x.CreatedAt < dateToExclusive.Value);

        return q;
    }

    private static IQueryable<CreditRepairLead> ApplyListFilters(
        IQueryable<CreditRepairLead> q,
        string? search,
        string? source,
        string? campaign,
        string? linkStatus,
        DateTime? dateFrom,
        DateTime? dateToExclusive)
    {
        q = ApplyDateFilter(q, dateFrom, dateToExclusive);

        if (!string.IsNullOrWhiteSpace(source))
        {
            var normalized = source.Trim();
            q = q.Where(x => x.Source == normalized);
        }

        if (!string.IsNullOrWhiteSpace(campaign))
        {
            var normalized = campaign.Trim();
            q = q.Where(x => x.CampaignName == normalized);
        }

        if (string.Equals(linkStatus, "linked", StringComparison.OrdinalIgnoreCase))
            q = q.Where(x => x.LeadId != null);
        else if (string.Equals(linkStatus, "unlinked", StringComparison.OrdinalIgnoreCase))
            q = q.Where(x => x.LeadId == null);

        if (search is not null)
        {
            var s = search.ToLowerInvariant();
            if (int.TryParse(search.Trim(), out var leadIdSearch))
            {
                q = q.Where(x =>
                    x.LeadId == leadIdSearch
                    || x.FullName.ToLower().Contains(s)
                    || x.Phone.Contains(s)
                    || (x.Email ?? "").ToLower().Contains(s));
            }
            else
            {
                q = q.Where(x =>
                    x.FullName.ToLower().Contains(s)
                    || x.Phone.Contains(s)
                    || (x.Email ?? "").ToLower().Contains(s));
            }
        }

        return q;
    }

    private static IQueryable<CreditRepairLead> ApplySort(
        IQueryable<CreditRepairLead> q,
        string sortField,
        bool sortAsc)
    {
        return sortField switch
        {
            "name" => sortAsc ? q.OrderBy(x => x.FullName) : q.OrderByDescending(x => x.FullName),
            "phone" => sortAsc ? q.OrderBy(x => x.Phone) : q.OrderByDescending(x => x.Phone),
            "source" => sortAsc ? q.OrderBy(x => x.Source) : q.OrderByDescending(x => x.Source),
            "campaign" => sortAsc ? q.OrderBy(x => x.CampaignName) : q.OrderByDescending(x => x.CampaignName),
            "lead_id" or "leadid" => sortAsc
                ? q.OrderBy(x => x.LeadId)
                : q.OrderByDescending(x => x.LeadId),
            _ => sortAsc ? q.OrderBy(x => x.CreatedAt) : q.OrderByDescending(x => x.CreatedAt),
        };
    }
}
