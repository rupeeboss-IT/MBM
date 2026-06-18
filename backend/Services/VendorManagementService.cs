using System.Text.Json;
using RB_Website_API.Auth;
using RB_Website_API.DTO;
using RB_Website_API.Models;
using RB_Website_API.Services.IRepository;

namespace RB_Website_API.Services;

public sealed class VendorManagementService : IVendorManagementService
{
    private readonly IVendorManagementRepository _repo;

    public VendorManagementService(IVendorManagementRepository repo) => _repo = repo;

    public async Task<VendorManagementStatsResponse> GetStatsAsync(string actorRole, CancellationToken ct)
    {
        if (!UserRoles.IsAdminRole(actorRole))
            return new VendorManagementStatsResponse(false, "Not authorized.");

        var stats = await _repo.GetStatsAsync(ct);
        return new VendorManagementStatsResponse(true, "OK", stats);
    }

    public async Task<VendorManagementListResponse> ListVendorsAsync(
        string actorRole, int page, int pageSize, string? search, string? status,
        string? dateFrom, string? dateTo, string? sortBy, string? sortDir, bool export, CancellationToken ct)
    {
        if (!UserRoles.IsAdminRole(actorRole))
            return new VendorManagementListResponse(false, "Not authorized.");

        (page, pageSize) = AdminListQuery.Normalize(page, pageSize, export);
        search = AdminListQuery.NormalizeSearch(search);
        var from = AdminListQuery.ParseDateFrom(dateFrom);
        var toEx = AdminListQuery.ParseDateToExclusive(dateTo);
        var (sortField, sortAsc) = NormalizeVendorSort(sortBy, sortDir);

        var (rows, total) = await _repo.ListVendorsAsync(
            page, pageSize, search, status, from, toEx, sortField, sortAsc, ct);

        return new VendorManagementListResponse(true, "OK", rows, total, page, pageSize);
    }

    public async Task<VendorManagementDetailResponse> GetVendorAsync(string actorRole, Guid vendorId, CancellationToken ct)
    {
        if (!UserRoles.IsAdminRole(actorRole))
            return new VendorManagementDetailResponse(false, "Not authorized.");

        var detail = await _repo.GetDetailAsync(vendorId, ct);
        if (detail is null)
            return new VendorManagementDetailResponse(false, "Vendor not found.");

        return new VendorManagementDetailResponse(true, "OK", detail);
    }

    public async Task<VendorActionResponse> CreateVendorAsync(
        Guid actorId, string actorRole, CreateVendorRequest req, CancellationToken ct)
    {
        if (!UserRoles.IsSuperAdmin(actorRole))
            return new VendorActionResponse(false, "Only Super Admin can create vendors.");

        var validation = ValidateVendor(req.ServiceName, req.CompanyName, req.ContactPersonName,
            req.Mobile, req.AlternateMobile, req.Email, req.AlternateEmail);
        if (validation is not null)
            return new VendorActionResponse(false, validation);

        var now = DateTime.Now;
        var vendor = new Vendor
        {
            VendorId = Guid.NewGuid(),
            ServiceName = req.ServiceName.Trim(),
            CompanyName = req.CompanyName.Trim(),
            ContactPersonName = req.ContactPersonName.Trim(),
            Mobile = IndianPhone.Digits(req.Mobile),
            AlternateMobile = string.IsNullOrWhiteSpace(req.AlternateMobile)
                ? null
                : IndianPhone.Digits(req.AlternateMobile),
            Email = req.Email.Trim(),
            AlternateEmail = string.IsNullOrWhiteSpace(req.AlternateEmail) ? null : req.AlternateEmail.Trim(),
            Website = NormalizeOptional(req.Website),
            Address = NormalizeOptional(req.Address),
            City = NormalizeOptional(req.City),
            State = NormalizeOptional(req.State),
            Country = NormalizeOptional(req.Country),
            Pincode = NormalizeOptional(req.Pincode),
            Remarks = NormalizeOptional(req.Remarks),
            IsActive = req.IsActive,
            IsDeleted = false,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = actorId,
        };

        await _repo.AddVendorAsync(vendor, ct);

        if (req.PlanIds is { Count: > 0 })
        {
            var planResult = await AssignPlansInternalAsync(actorId, vendor.VendorId, req.PlanIds, ct);
            if (!planResult.Success)
                return planResult;
        }

        await WriteAuditAsync(vendor.VendorId, VendorAuditActions.Created, actorId, null, SnapshotVendor(vendor), null, ct);
        return new VendorActionResponse(true, "Vendor created.", vendor.VendorId);
    }

    public async Task<VendorActionResponse> UpdateVendorAsync(
        Guid actorId, string actorRole, Guid vendorId, UpdateVendorRequest req, CancellationToken ct)
    {
        if (!UserRoles.IsSuperAdmin(actorRole))
            return new VendorActionResponse(false, "Only Super Admin can update vendors.");

        var validation = ValidateVendor(req.ServiceName, req.CompanyName, req.ContactPersonName,
            req.Mobile, req.AlternateMobile, req.Email, req.AlternateEmail);
        if (validation is not null)
            return new VendorActionResponse(false, validation);

        var vendor = await _repo.GetByIdAsync(vendorId, track: true, ct);
        if (vendor is null)
            return new VendorActionResponse(false, "Vendor not found.");

        var previous = SnapshotVendor(vendor);
        var oldPlanIds = await _repo.GetVendorPlanIdsAsync(vendorId, ct);

        vendor.ServiceName = req.ServiceName.Trim();
        vendor.CompanyName = req.CompanyName.Trim();
        vendor.ContactPersonName = req.ContactPersonName.Trim();
        vendor.Mobile = IndianPhone.Digits(req.Mobile);
        vendor.AlternateMobile = string.IsNullOrWhiteSpace(req.AlternateMobile)
            ? null
            : IndianPhone.Digits(req.AlternateMobile);
        vendor.Email = req.Email.Trim();
        vendor.AlternateEmail = string.IsNullOrWhiteSpace(req.AlternateEmail) ? null : req.AlternateEmail.Trim();
        vendor.Website = NormalizeOptional(req.Website);
        vendor.Address = NormalizeOptional(req.Address);
        vendor.City = NormalizeOptional(req.City);
        vendor.State = NormalizeOptional(req.State);
        vendor.Country = NormalizeOptional(req.Country);
        vendor.Pincode = NormalizeOptional(req.Pincode);
        vendor.Remarks = NormalizeOptional(req.Remarks);
        vendor.IsActive = req.IsActive;
        vendor.UpdatedAt = DateTime.Now;

        await _repo.SaveChangesAsync(ct);

        if (req.PlanIds is not null)
        {
            var planAudit = await SyncPlansWithAuditAsync(actorId, vendorId, oldPlanIds, req.PlanIds, ct);
            if (!planAudit.Success)
                return planAudit;
        }

        await WriteAuditAsync(vendorId, VendorAuditActions.Updated, actorId, previous, SnapshotVendor(vendor), null, ct);
        return new VendorActionResponse(true, "Vendor updated.", vendorId);
    }

    public async Task<VendorActionResponse> SetActiveAsync(
        Guid actorId, string actorRole, Guid vendorId, SetVendorActiveRequest req, CancellationToken ct)
    {
        if (!UserRoles.IsSuperAdmin(actorRole))
            return new VendorActionResponse(false, "Only Super Admin can change vendor status.");

        var vendor = await _repo.GetByIdAsync(vendorId, track: true, ct);
        if (vendor is null)
            return new VendorActionResponse(false, "Vendor not found.");

        if (req.IsActive == vendor.IsActive)
            return new VendorActionResponse(true, "No change.", vendorId);

        if (!req.IsActive && string.IsNullOrWhiteSpace(req.Remarks))
            return new VendorActionResponse(false, "Remarks are required to deactivate a vendor.");

        var oldStatus = vendor.IsActive;
        vendor.IsActive = req.IsActive;
        vendor.UpdatedAt = DateTime.Now;
        await _repo.SaveChangesAsync(ct);

        var action = req.IsActive ? VendorAuditActions.Activated : VendorAuditActions.Deactivated;
        await WriteAuditAsync(
            vendorId,
            action,
            actorId,
            new { IsActive = oldStatus },
            new { IsActive = req.IsActive },
            req.Remarks?.Trim(),
            ct);

        return new VendorActionResponse(true, req.IsActive ? "Vendor activated." : "Vendor deactivated.", vendorId);
    }

    public async Task<VendorActionResponse> SoftDeleteAsync(
        Guid actorId, string actorRole, Guid vendorId, DeleteVendorRequest req, CancellationToken ct)
    {
        if (!UserRoles.IsSuperAdmin(actorRole))
            return new VendorActionResponse(false, "Only Super Admin can delete vendors.");

        var vendor = await _repo.GetByIdAsync(vendorId, track: true, ct);
        if (vendor is null)
            return new VendorActionResponse(false, "Vendor not found.");

        var previous = SnapshotVendor(vendor);
        var now = DateTime.Now;
        vendor.IsDeleted = true;
        vendor.IsActive = false;
        vendor.DeletedAt = now;
        vendor.DeletedByUserId = actorId;
        vendor.UpdatedAt = now;
        await _repo.SaveChangesAsync(ct);

        await WriteAuditAsync(
            vendorId,
            VendorAuditActions.Deleted,
            actorId,
            previous,
            new { IsDeleted = true },
            req.Remarks?.Trim(),
            ct);

        return new VendorActionResponse(true, "Vendor deleted.", vendorId);
    }

    public async Task<VendorActionResponse> AssignPlansAsync(
        Guid actorId, string actorRole, Guid vendorId, AssignVendorPlansRequest req, CancellationToken ct)
    {
        if (!UserRoles.IsSuperAdmin(actorRole))
            return new VendorActionResponse(false, "Only Super Admin can assign plans.");

        var vendor = await _repo.GetByIdAsync(vendorId, track: false, ct);
        if (vendor is null)
            return new VendorActionResponse(false, "Vendor not found.");

        var oldPlanIds = await _repo.GetVendorPlanIdsAsync(vendorId, ct);
        return await SyncPlansWithAuditAsync(actorId, vendorId, oldPlanIds, req.PlanIds ?? Array.Empty<int>(), ct);
    }

    public async Task<VendorAuditListResponse> ListAuditAsync(
        string actorRole, Guid vendorId, int page, int pageSize, CancellationToken ct)
    {
        if (!UserRoles.IsAdminRole(actorRole))
            return new VendorAuditListResponse(false, "Not authorized.");

        var vendor = await _repo.GetByIdAsync(vendorId, track: false, ct);
        if (vendor is null)
            return new VendorAuditListResponse(false, "Vendor not found.");

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, AdminListQuery.MaxPageSize);
        var (rows, total) = await _repo.ListAuditLogsAsync(vendorId, page, pageSize, ct);
        return new VendorAuditListResponse(true, "OK", rows, total, page, pageSize);
    }

    public async Task<VendorPlanListResponse> ListPlansAsync(string actorRole, CancellationToken ct)
    {
        if (!UserRoles.IsAdminRole(actorRole))
            return new VendorPlanListResponse(false, "Not authorized.");

        var plans = await _repo.ListActivePlansAsync(ct);
        return new VendorPlanListResponse(true, "OK", plans);
    }

    public async Task<VendorPlanMappingListResponse> ListPlanMappingsAsync(string actorRole, CancellationToken ct)
    {
        if (!UserRoles.IsAdminRole(actorRole))
            return new VendorPlanMappingListResponse(false, "Not authorized.");

        var groups = await _repo.ListPlanMappingsAsync(ct);
        return new VendorPlanMappingListResponse(true, "OK", groups);
    }

    private async Task<VendorActionResponse> AssignPlansInternalAsync(
        Guid actorId, Guid vendorId, IReadOnlyList<int> planIds, CancellationToken ct)
    {
        var validIds = await ValidatePlanIdsAsync(planIds, ct);
        if (validIds is null)
            return new VendorActionResponse(false, "One or more selected plans are invalid.");

        await _repo.SetVendorPlansAsync(vendorId, validIds, actorId, ct);
        return new VendorActionResponse(true, "Plans assigned.", vendorId);
    }

    private async Task<VendorActionResponse> SyncPlansWithAuditAsync(
        Guid actorId,
        Guid vendorId,
        IReadOnlyList<int> oldPlanIds,
        IReadOnlyList<int> newPlanIds,
        CancellationToken ct)
    {
        var validIds = await ValidatePlanIdsAsync(newPlanIds, ct);
        if (validIds is null)
            return new VendorActionResponse(false, "One or more selected plans are invalid.");

        var oldSet = oldPlanIds.ToHashSet();
        var newSet = validIds.ToHashSet();
        var added = newSet.Except(oldSet).ToList();
        var removed = oldSet.Except(newSet).ToList();

        if (added.Count == 0 && removed.Count == 0)
            return new VendorActionResponse(true, "No plan changes.", vendorId);

        await _repo.SetVendorPlansAsync(vendorId, validIds, actorId, ct);

        if (added.Count > 0)
        {
            await WriteAuditAsync(
                vendorId,
                VendorAuditActions.PlanAssigned,
                actorId,
                new { PlanIds = oldPlanIds },
                new { PlanIds = added },
                null,
                ct);
        }

        if (removed.Count > 0)
        {
            await WriteAuditAsync(
                vendorId,
                VendorAuditActions.PlanRemoved,
                actorId,
                new { PlanIds = oldPlanIds },
                new { PlanIds = removed },
                null,
                ct);
        }

        return new VendorActionResponse(true, "Plans updated.", vendorId);
    }

    private async Task<IReadOnlyList<int>?> ValidatePlanIdsAsync(IReadOnlyList<int> planIds, CancellationToken ct)
    {
        if (planIds.Count == 0) return Array.Empty<int>();

        var activePlans = await _repo.ListActivePlansAsync(ct);
        var activeIds = activePlans.Select(p => p.PlanId).ToHashSet();
        var distinct = planIds.Distinct().ToList();
        if (distinct.Any(id => !activeIds.Contains(id)))
            return null;

        return distinct;
    }

    private static (string SortField, bool SortAsc) NormalizeVendorSort(string? sortBy, string? sortDir)
    {
        var field = (sortBy ?? "").Trim().ToLowerInvariant();
        var asc = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);

        return field switch
        {
            "service" => ("service", asc),
            "company" => ("company", asc),
            "contact" => ("contact", asc),
            "status" => ("status", asc),
            "oldest" => ("created", true),
            "name_asc" => ("service", true),
            "name_desc" => ("service", false),
            "latest" or _ => ("created", false),
        };
    }

    private static string? ValidateVendor(
        string serviceName,
        string companyName,
        string contactPersonName,
        string mobile,
        string? alternateMobile,
        string email,
        string? alternateEmail)
    {
        if (string.IsNullOrWhiteSpace(serviceName)) return "Service name is required.";
        if (string.IsNullOrWhiteSpace(companyName)) return "Company name is required.";
        if (string.IsNullOrWhiteSpace(contactPersonName)) return "Contact person name is required.";
        if (string.IsNullOrWhiteSpace(mobile)) return "Mobile number is required.";
        if (IndianPhone.Digits(mobile).Length != 10) return "Invalid mobile number.";
        if (!string.IsNullOrWhiteSpace(alternateMobile) && IndianPhone.Digits(alternateMobile).Length != 10)
            return "Invalid alternate mobile number.";
        if (string.IsNullOrWhiteSpace(email)) return "Email is required.";
        if (!string.IsNullOrWhiteSpace(alternateEmail) && !alternateEmail.Contains('@'))
            return "Invalid alternate email address.";
        return null;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static object SnapshotVendor(Vendor v) => new
    {
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
    };

    private Task WriteAuditAsync(
        Guid vendorId,
        string action,
        Guid actorId,
        object? previous,
        object? current,
        string? remarks,
        CancellationToken ct) =>
        _repo.AddAuditLogAsync(new VendorAuditLog
        {
            Id = Guid.NewGuid(),
            VendorId = vendorId,
            Action = action,
            PerformedByUserId = actorId,
            PerformedOn = DateTime.Now,
            PreviousValues = previous is null ? null : JsonSerializer.Serialize(previous),
            NewValues = current is null ? null : JsonSerializer.Serialize(current),
            Remarks = remarks,
        }, ct);
}
