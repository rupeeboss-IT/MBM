using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RB_Website_API.Auth;
using RB_Website_API.DTO;
using RB_Website_API.Services;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/customer/reports")]
public sealed class CustomerReportsController : ControllerBase
{
    private readonly ICustomerReportService _reports;

    public CustomerReportsController(ICustomerReportService reports) => _reports = reports;

    public sealed record ListReportsResponse(bool Success, string? Message, List<CustomerReportListItemDto>? Items);

    [Authorize(Policy = "MemberAccess")]
    [HttpGet]
    public async Task<ActionResult<ListReportsResponse>> List(CancellationToken ct)
    {
        var userId = CurrentUser.RequireUserId(User);
        var items = await _reports.ListCustomerReportsAsync(userId, ct);
        return Ok(new ListReportsResponse(true, "OK", items.ToList()));
    }

    /// <summary>Secured download — validates ownership and active subscription before streaming file.</summary>
    [Authorize(Policy = "MemberAccess")]
    [HttpGet("{reportId:guid}/download")]
    public async Task<IActionResult> DownloadReport(Guid reportId, CancellationToken ct)
    {
        var userId = CurrentUser.RequireUserId(User);
        var (allowed, error, path, downloadName) = await _reports.GetDownloadForCustomerAsync(
            userId,
            reportId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            ct);

        if (!allowed)
        {
            if (error == "Access Denied.")
                return StatusCode(403, new { success = false, message = "Access Denied." });
            return NotFound(new { success = false, message = error ?? "Access Denied." });
        }

        return PhysicalFile(path!, "application/zip", downloadName);
    }
}
