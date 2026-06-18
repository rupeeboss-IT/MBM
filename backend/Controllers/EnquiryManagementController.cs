using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RB_Website_API.Auth;
using RB_Website_API.DTO;
using RB_Website_API.Services;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/admin/enquiry-management")]
[Authorize(Policy = "AdminAccess")]
public sealed class EnquiryManagementController : ControllerBase
{
    private readonly IEnquiryManagementService _enquiries;

    public EnquiryManagementController(IEnquiryManagementService enquiries) => _enquiries = enquiries;

    [HttpGet("stats")]
    public Task<EnquiryManagementStatsResponse> Stats(CancellationToken ct) =>
        _enquiries.GetStatsAsync(CurrentUser.GetRole(User) ?? "", ct);

    [HttpGet("filters")]
    public Task<EnquiryFiltersResponse> Filters(CancellationToken ct) =>
        _enquiries.GetFiltersAsync(CurrentUser.GetRole(User) ?? "", ct);

    [HttpGet("enquiries")]
    public Task<EnquiryManagementListResponse> ListEnquiries(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = AdminListQuery.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] string? source = null,
        [FromQuery] string? dateFrom = null,
        [FromQuery] string? dateTo = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = null,
        [FromQuery] bool export = false,
        CancellationToken ct = default) =>
        _enquiries.ListEnquiriesAsync(
            CurrentUser.GetRole(User) ?? "",
            page, pageSize, search, status, source, dateFrom, dateTo, sortBy, sortDir, export, ct);

    [HttpGet("enquiries/{enquiryId:int}")]
    public Task<EnquiryManagementDetailResponse> GetEnquiry(int enquiryId, CancellationToken ct) =>
        _enquiries.GetEnquiryAsync(CurrentUser.GetRole(User) ?? "", enquiryId, ct);

    [HttpPatch("enquiries/{enquiryId:int}/status")]
    public Task<EnquiryActionResponse> UpdateStatus(
        int enquiryId, [FromBody] UpdateEnquiryStatusRequest? req, CancellationToken ct) =>
        req is null
            ? Task.FromResult(new EnquiryActionResponse(false, "Request is required."))
            : _enquiries.UpdateStatusAsync(
                CurrentUser.RequireUserId(User),
                CurrentUser.GetRole(User) ?? "",
                enquiryId,
                req,
                ct);

    [HttpPost("enquiries/bulk-status")]
    public Task<EnquiryActionResponse> BulkUpdateStatus(
        [FromBody] BulkUpdateEnquiryStatusRequest? req, CancellationToken ct) =>
        req is null
            ? Task.FromResult(new EnquiryActionResponse(false, "Request is required."))
            : _enquiries.BulkUpdateStatusAsync(
                CurrentUser.RequireUserId(User),
                CurrentUser.GetRole(User) ?? "",
                req,
                ct);
}
