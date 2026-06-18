using RB_Website_API.Auth;
using RB_Website_API.DTO;
using RB_Website_API.Models;
using RB_Website_API.Services.IRepository;

namespace RB_Website_API.Services;

public sealed class EnquiryManagementService : IEnquiryManagementService
{
    private readonly IEnquiryManagementRepository _repo;

    public EnquiryManagementService(IEnquiryManagementRepository repo) => _repo = repo;

    public async Task<EnquiryManagementStatsResponse> GetStatsAsync(string actorRole, CancellationToken ct)
    {
        if (!UserRoles.IsAdminRole(actorRole))
            return new EnquiryManagementStatsResponse(false, "Not authorized.");

        var stats = await _repo.GetStatsAsync(ct);
        return new EnquiryManagementStatsResponse(true, "OK", stats);
    }

    public Task<EnquiryFiltersResponse> GetFiltersAsync(string actorRole, CancellationToken ct)
    {
        if (!UserRoles.IsAdminRole(actorRole))
            return Task.FromResult(new EnquiryFiltersResponse(false, "Not authorized."));

        return Task.FromResult(new EnquiryFiltersResponse(
            true,
            "OK",
            EnquirySources.All,
            EnquiryStatuses.All));
    }

    public async Task<EnquiryManagementListResponse> ListEnquiriesAsync(
        string actorRole,
        int page,
        int pageSize,
        string? search,
        string? status,
        string? source,
        string? dateFrom,
        string? dateTo,
        string? sortBy,
        string? sortDir,
        bool export,
        CancellationToken ct)
    {
        if (!UserRoles.IsAdminRole(actorRole))
            return new EnquiryManagementListResponse(false, "Not authorized.");

        (page, pageSize) = AdminListQuery.Normalize(page, pageSize, export);
        search = AdminListQuery.NormalizeSearch(search);
        var from = AdminListQuery.ParseDateFrom(dateFrom);
        var toEx = AdminListQuery.ParseDateToExclusive(dateTo);
        var (sortField, sortAsc) = NormalizeSort(sortBy, sortDir);

        var (rows, total) = await _repo.ListEnquiriesAsync(
            page, pageSize, search, status, source, from, toEx, sortField, sortAsc, ct);

        return new EnquiryManagementListResponse(true, "OK", rows, total, page, pageSize);
    }

    public async Task<EnquiryManagementDetailResponse> GetEnquiryAsync(
        string actorRole, int enquiryId, CancellationToken ct)
    {
        if (!UserRoles.IsAdminRole(actorRole))
            return new EnquiryManagementDetailResponse(false, "Not authorized.");

        var detail = await _repo.GetDetailAsync(enquiryId, ct);
        if (detail is null)
            return new EnquiryManagementDetailResponse(false, "Enquiry not found.");

        return new EnquiryManagementDetailResponse(true, "OK", detail);
    }

    public async Task<EnquiryActionResponse> UpdateStatusAsync(
        Guid actorId,
        string actorRole,
        int enquiryId,
        UpdateEnquiryStatusRequest req,
        CancellationToken ct)
    {
        if (!UserRoles.IsAdminRole(actorRole))
            return new EnquiryActionResponse(false, "Not authorized.");

        if (!EnquiryStatuses.IsValid(req.Status))
            return new EnquiryActionResponse(false, "Invalid status.");

        var entity = await _repo.GetByIdAsync(enquiryId, track: true, ct);
        if (entity is null)
            return new EnquiryActionResponse(false, "Enquiry not found.");

        var newStatus = EnquiryStatuses.Normalize(req.Status);
        var oldStatus = EnquiryStatuses.Normalize(entity.Status);
        if (oldStatus == newStatus)
            return new EnquiryActionResponse(true, "Status unchanged.", 0);

        entity.Status = newStatus;
        entity.UpdatedAt = DateTime.Now;

        await _repo.AddStatusHistoryAsync(new EnquiryStatusHistory
        {
            Id = Guid.NewGuid(),
            ContactSubmissionId = enquiryId,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ChangedByUserId = actorId,
            ChangedOn = DateTime.Now,
            Remarks = NormalizeRemarks(req.Remarks),
        }, ct);

        await _repo.SaveChangesAsync(ct);
        return new EnquiryActionResponse(true, $"Status updated to {newStatus}.", 1);
    }

    public async Task<EnquiryActionResponse> BulkUpdateStatusAsync(
        Guid actorId,
        string actorRole,
        BulkUpdateEnquiryStatusRequest req,
        CancellationToken ct)
    {
        if (!UserRoles.IsAdminRole(actorRole))
            return new EnquiryActionResponse(false, "Not authorized.");

        if (req.EnquiryIds is null || req.EnquiryIds.Count == 0)
            return new EnquiryActionResponse(false, "Select at least one enquiry.");

        if (!EnquiryStatuses.IsValid(req.Status))
            return new EnquiryActionResponse(false, "Invalid status.");

        var newStatus = EnquiryStatuses.Normalize(req.Status);
        var remarks = NormalizeRemarks(req.Remarks);
        var updated = 0;

        foreach (var enquiryId in req.EnquiryIds.Distinct())
        {
            var entity = await _repo.GetByIdAsync(enquiryId, track: true, ct);
            if (entity is null) continue;

            var oldStatus = EnquiryStatuses.Normalize(entity.Status);
            if (oldStatus == newStatus) continue;

            entity.Status = newStatus;
            entity.UpdatedAt = DateTime.Now;

            await _repo.AddStatusHistoryAsync(new EnquiryStatusHistory
            {
                Id = Guid.NewGuid(),
                ContactSubmissionId = enquiryId,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                ChangedByUserId = actorId,
                ChangedOn = DateTime.Now,
                Remarks = remarks,
            }, ct);

            updated++;
        }

        if (updated == 0)
            return new EnquiryActionResponse(true, "No enquiries required updating.", 0);

        await _repo.SaveChangesAsync(ct);
        return new EnquiryActionResponse(true, $"{updated} enquiry(s) updated to {newStatus}.", updated);
    }

    private static (string SortField, bool SortAsc) NormalizeSort(string? sortBy, string? sortDir)
    {
        var field = (sortBy ?? "").Trim().ToLowerInvariant();
        var asc = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);

        return field switch
        {
            "latest" => ("created", false),
            "oldest" => ("oldest", true),
            "name_asc" => ("name", true),
            "name_desc" => ("name", false),
            "name" or "company" or "source" or "status" or "subject" or "created" => (field, asc),
            _ => ("created", false),
        };
    }

    private static string? NormalizeRemarks(string? remarks)
    {
        var r = (remarks ?? "").Trim();
        if (r.Length == 0) return null;
        return r.Length > 800 ? r[..800] : r;
    }
}
