using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RB_Website_API.Auth;
using RB_Website_API.Services;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/admin/lead-attribution")]
[Authorize(Policy = "AdminAccess")]
public sealed class LeadAttributionController : ControllerBase
{
    private readonly ILeadAttributionService _leads;

    public LeadAttributionController(ILeadAttributionService leads) => _leads = leads;

    [HttpGet("dashboard")]
    public Task<RB_Website_API.DTO.LeadAttributionStatsResponse> Dashboard(
        [FromQuery] string? dateFrom = null,
        [FromQuery] string? dateTo = null,
        CancellationToken ct = default) =>
        _leads.GetDashboardAsync(
            CurrentUser.RequireUserId(User),
            CurrentUser.GetRole(User) ?? "",
            dateFrom,
            dateTo,
            ct);

    [HttpGet("filters")]
    public Task<RB_Website_API.DTO.LeadFilterOptionsResponse> Filters(CancellationToken ct) =>
        _leads.GetFilterOptionsAsync(CurrentUser.RequireUserId(User), CurrentUser.GetRole(User) ?? "", ct);

    [HttpGet("customers")]
    public Task<RB_Website_API.DTO.LeadCustomerListResponse> ListCustomers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = AdminListQuery.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] string? sourceType = null,
        [FromQuery] string? sourceName = null,
        [FromQuery] string? employeeName = null,
        [FromQuery] string? partnerName = null,
        [FromQuery] string? planCode = null,
        [FromQuery] string? dateFrom = null,
        [FromQuery] string? dateTo = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = null,
        [FromQuery] bool export = false,
        CancellationToken ct = default) =>
        _leads.ListCustomersAsync(
            CurrentUser.RequireUserId(User),
            CurrentUser.GetRole(User) ?? "",
            page, pageSize, search, sourceType, sourceName, employeeName, partnerName, planCode,
            dateFrom, dateTo, sortBy, sortDir, export, ct);

    [HttpGet("customers/{userId:guid}")]
    public Task<RB_Website_API.DTO.LeadCustomerDetailResponse> GetCustomer(Guid userId, CancellationToken ct) =>
        _leads.GetCustomerAsync(
            CurrentUser.RequireUserId(User),
            CurrentUser.GetRole(User) ?? "",
            userId,
            ct);

    [HttpGet("performers/{performerType}/details")]
    public Task<RB_Website_API.DTO.LeadPerformerDetailResponse> GetPerformerDetails(
        string performerType,
        [FromQuery] string code,
        [FromQuery] string? dateFrom = null,
        [FromQuery] string? dateTo = null,
        CancellationToken ct = default) =>
        _leads.GetPerformerDetailsAsync(
            CurrentUser.RequireUserId(User),
            CurrentUser.GetRole(User) ?? "",
            performerType,
            code,
            dateFrom,
            dateTo,
            ct);
}
