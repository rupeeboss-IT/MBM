using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RB_Website_API.Auth;
using RB_Website_API.DTO;
using RB_Website_API.Services;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/admin/reports")]
[Authorize(Policy = "AdminAccess")]
public sealed class AdminReportsController : ControllerBase
{
    private readonly ICustomerReportService _reports;

    public AdminReportsController(ICustomerReportService reports) => _reports = reports;

    public sealed record SearchCustomersResponse(bool Success, string? Message, List<CustomerSearchResultDto>? Customers);

    public sealed record UploadReportResponse(bool Success, string? Message, Guid? ReportId);

    public sealed record HistoryResponse(
        bool Success,
        string? Message,
        List<AdminReportHistoryItemDto>? Items,
        int TotalCount,
        int Page,
        int PageSize);

    [HttpGet("customers/search")]
    public async Task<ActionResult<SearchCustomersResponse>> SearchCustomers(
        [FromQuery] string? memberId,
        [FromQuery] string? mobile,
        [FromQuery] string? email,
        [FromQuery] string? customerName,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(memberId)
            && string.IsNullOrWhiteSpace(mobile)
            && string.IsNullOrWhiteSpace(email)
            && string.IsNullOrWhiteSpace(customerName))
        {
            return BadRequest(new SearchCustomersResponse(false, "Enter at least one search field.", null));
        }

        var results = await _reports.SearchCustomersAsync(memberId, mobile, email, customerName, ct);
        return Ok(new SearchCustomersResponse(true, "OK", results.ToList()));
    }

    [HttpGet("customers/{customerId:guid}/subscription")]
    public async Task<ActionResult<object>> ValidateSubscription(Guid customerId, CancellationToken ct)
    {
        var (ok, err, subId) = await _reports.ValidateActiveSubscriptionAsync(customerId, ct);
        if (!ok)
            return BadRequest(new { success = false, message = err });
        return Ok(new { success = true, message = "OK", subscriptionId = subId });
    }

    [HttpPost("upload")]
    [RequestSizeLimit(104_857_600)]
    public async Task<ActionResult<UploadReportResponse>> Upload(
        [FromForm] Guid customerId,
        IFormFile? file,
        CancellationToken ct)
    {
        if (customerId == Guid.Empty)
            return BadRequest(new UploadReportResponse(false, "Customer is required.", null));
        if (file is null)
            return BadRequest(new UploadReportResponse(false, "ZIP file is required.", null));

        var adminId = CurrentUser.RequireUserId(User);
        var (success, message, reportId) = await _reports.UploadReportAsync(
            adminId,
            customerId,
            file,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            ct);

        if (!success)
            return BadRequest(new UploadReportResponse(false, message, null));

        return Ok(new UploadReportResponse(true, message, reportId));
    }

    [HttpGet("history")]
    public async Task<ActionResult<HistoryResponse>> History(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _reports.ListAdminHistoryAsync(search, page, pageSize, ct);
        return Ok(new HistoryResponse(
            result.Success,
            result.Message,
            result.Items.ToList(),
            result.TotalCount,
            result.Page,
            result.PageSize));
    }
}
