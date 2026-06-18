using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RB_Website_API.Auth;
using RB_Website_API.DTO;
using RB_Website_API.Services;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/admin/vendor-management")]
[Authorize(Policy = "AdminAccess")]
public sealed class VendorManagementController : ControllerBase
{
    private readonly IVendorManagementService _vendors;

    public VendorManagementController(IVendorManagementService vendors) => _vendors = vendors;

    [HttpGet("stats")]
    public Task<VendorManagementStatsResponse> Stats(CancellationToken ct) =>
        _vendors.GetStatsAsync(CurrentUser.GetRole(User) ?? "", ct);

    [HttpGet("plans")]
    public Task<VendorPlanListResponse> Plans(CancellationToken ct) =>
        _vendors.ListPlansAsync(CurrentUser.GetRole(User) ?? "", ct);

    [HttpGet("plan-mappings")]
    public Task<VendorPlanMappingListResponse> PlanMappings(CancellationToken ct) =>
        _vendors.ListPlanMappingsAsync(CurrentUser.GetRole(User) ?? "", ct);

    [HttpGet("vendors")]
    public Task<VendorManagementListResponse> ListVendors(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = AdminListQuery.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] string? dateFrom = null,
        [FromQuery] string? dateTo = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = null,
        [FromQuery] bool export = false,
        CancellationToken ct = default) =>
        _vendors.ListVendorsAsync(
            CurrentUser.GetRole(User) ?? "",
            page, pageSize, search, status, dateFrom, dateTo, sortBy, sortDir, export, ct);

    [HttpGet("vendors/{vendorId:guid}")]
    public Task<VendorManagementDetailResponse> GetVendor(Guid vendorId, CancellationToken ct) =>
        _vendors.GetVendorAsync(CurrentUser.GetRole(User) ?? "", vendorId, ct);

    [HttpPost("vendors")]
    [Authorize(Policy = "SuperAdminOnly")]
    public Task<VendorActionResponse> CreateVendor([FromBody] CreateVendorRequest? req, CancellationToken ct) =>
        req is null
            ? Task.FromResult(new VendorActionResponse(false, "Request is required."))
            : _vendors.CreateVendorAsync(CurrentUser.RequireUserId(User), CurrentUser.GetRole(User) ?? "", req, ct);

    [HttpPut("vendors/{vendorId:guid}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public Task<VendorActionResponse> UpdateVendor(
        Guid vendorId, [FromBody] UpdateVendorRequest? req, CancellationToken ct) =>
        req is null
            ? Task.FromResult(new VendorActionResponse(false, "Request is required."))
            : _vendors.UpdateVendorAsync(
                CurrentUser.RequireUserId(User), CurrentUser.GetRole(User) ?? "", vendorId, req, ct);

    [HttpPatch("vendors/{vendorId:guid}/active")]
    [Authorize(Policy = "SuperAdminOnly")]
    public Task<VendorActionResponse> SetActive(
        Guid vendorId, [FromBody] SetVendorActiveRequest? req, CancellationToken ct) =>
        req is null
            ? Task.FromResult(new VendorActionResponse(false, "Request is required."))
            : _vendors.SetActiveAsync(
                CurrentUser.RequireUserId(User), CurrentUser.GetRole(User) ?? "", vendorId, req, ct);

    [HttpDelete("vendors/{vendorId:guid}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public Task<VendorActionResponse> DeleteVendor(
        Guid vendorId, [FromBody] DeleteVendorRequest? req, CancellationToken ct) =>
        _vendors.SoftDeleteAsync(
            CurrentUser.RequireUserId(User),
            CurrentUser.GetRole(User) ?? "",
            vendorId,
            req ?? new DeleteVendorRequest(null),
            ct);

    [HttpPut("vendors/{vendorId:guid}/plans")]
    [Authorize(Policy = "SuperAdminOnly")]
    public Task<VendorActionResponse> AssignPlans(
        Guid vendorId, [FromBody] AssignVendorPlansRequest? req, CancellationToken ct) =>
        req is null
            ? Task.FromResult(new VendorActionResponse(false, "Request is required."))
            : _vendors.AssignPlansAsync(
                CurrentUser.RequireUserId(User), CurrentUser.GetRole(User) ?? "", vendorId, req, ct);

    [HttpGet("vendors/{vendorId:guid}/audit")]
    public Task<VendorAuditListResponse> Audit(
        Guid vendorId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = AdminListQuery.DefaultPageSize,
        CancellationToken ct = default) =>
        _vendors.ListAuditAsync(CurrentUser.GetRole(User) ?? "", vendorId, page, pageSize, ct);
}
