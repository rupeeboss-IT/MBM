using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RB_Website_API.Auth;
using RB_Website_API.DTO;
using RB_Website_API.Services;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/admin/credit-repair")]
[Authorize(Policy = "AdminAccess")]
public sealed class CreditRepairManagementController : ControllerBase
{
    private readonly ICreditRepairManagementService _creditRepair;

    public CreditRepairManagementController(ICreditRepairManagementService creditRepair) =>
        _creditRepair = creditRepair;

    [HttpGet("stats")]
    public Task<CreditRepairManagementStatsResponse> Stats(
        [FromQuery] string? dateFrom = null,
        [FromQuery] string? dateTo = null,
        CancellationToken ct = default) =>
        _creditRepair.GetStatsAsync(CurrentUser.GetRole(User) ?? "", dateFrom, dateTo, ct);

    [HttpGet("filters")]
    public Task<CreditRepairFiltersResponse> Filters(CancellationToken ct) =>
        _creditRepair.GetFiltersAsync(CurrentUser.GetRole(User) ?? "", ct);

    [HttpGet("leads")]
    public Task<CreditRepairManagementListResponse> ListLeads(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = AdminListQuery.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] string? source = null,
        [FromQuery] string? campaign = null,
        [FromQuery] string? linkStatus = null,
        [FromQuery] string? dateFrom = null,
        [FromQuery] string? dateTo = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = null,
        [FromQuery] bool export = false,
        CancellationToken ct = default) =>
        _creditRepair.ListLeadsAsync(
            CurrentUser.GetRole(User) ?? "",
            page,
            pageSize,
            search,
            source,
            campaign,
            linkStatus,
            dateFrom,
            dateTo,
            sortBy,
            sortDir,
            export,
            ct);
}
