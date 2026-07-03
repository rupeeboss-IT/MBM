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
    private readonly IReportChangeRequestService _changeRequests;
    private readonly ISdrReportService _sdrReports;
    private readonly IReportAuditService _audit;

    public AdminReportsController(
        ICustomerReportService reports,
        IReportChangeRequestService changeRequests,
        ISdrReportService sdrReports,
        IReportAuditService audit)
    {
        _reports = reports;
        _changeRequests = changeRequests;
        _sdrReports = sdrReports;
        _audit = audit;
    }

    public sealed record SearchCustomersResponse(bool Success, string? Message, List<CustomerSearchResultDto>? Customers);

    public sealed record UploadReportResponse(bool Success, string? Message, Guid? ReportId);

    public sealed record HistoryResponse(
        bool Success,
        string? Message,
        List<AdminReportHistoryItemDto>? Items,
        int TotalCount,
        int Page,
        int PageSize);

    public sealed record ChangeRequestResponse(bool Success, string? Message, Guid? RequestId);

    public sealed record ChangeRequestListResponse(
        bool Success,
        string? Message,
        List<ReportChangeRequestListItemDto>? Items,
        int TotalCount,
        int Page,
        int PageSize);

    public sealed record ChangeRequestDetailResponse(
        bool Success,
        string? Message,
        ReportChangeRequestDetailDto? Request);

    public sealed record PendingCountResponse(bool Success, int Count);

    public sealed record ActionResponse(bool Success, string? Message);

    public sealed record AuditHistoryResponse(bool Success, string? Message, List<ReportAuditLogItemDto>? Items);

    public sealed record GenerateSdrBody(Guid CustomerId, string UdyamNumber);

    public sealed record ListSdrReportsResponse(bool Success, string? Message, List<CustomerReportListItemDto>? Items);

    [HttpPost("sdr/generate")]
    public async Task<ActionResult<AdminSdrGenerateResponse>> GenerateSdr(
        [FromBody] GenerateSdrBody? body,
        CancellationToken ct)
    {
        if (body is null || body.CustomerId == Guid.Empty)
            return BadRequest(new AdminSdrGenerateResponse(false, "Customer is required.", null, null, null, null));
        if (string.IsNullOrWhiteSpace(body.UdyamNumber))
            return BadRequest(new AdminSdrGenerateResponse(false, "Udyam Registration Number is required.", null, null, null, null));

        var adminId = CurrentUser.RequireUserId(User);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _sdrReports.GenerateForAdminAsync(
            adminId,
            body.CustomerId,
            body.UdyamNumber,
            ct);

        if (result.ReportId is Guid reportId)
        {
            await _audit.LogAsync(
                ReportAuditService.ActionSdrAdminGenerate,
                adminId,
                reportId,
                body.CustomerId,
                ip,
                ct);
        }

        if (!result.Success || result.Outcome == SdrReportCatalog.OutcomeFailed)
            return BadRequest(new AdminSdrGenerateResponse(
                false,
                result.UserMessage,
                result.ReportId,
                null,
                result.ExpiryDate,
                result.Outcome));

        return Ok(new AdminSdrGenerateResponse(
            true,
            result.UserMessage,
            result.ReportId,
            null,
            result.ExpiryDate,
            result.Outcome));
    }

    [HttpGet("sdr/customers/{customerId:guid}")]
    public async Task<ActionResult<ListSdrReportsResponse>> ListCustomerSdrReports(
        Guid customerId,
        CancellationToken ct)
    {
        if (customerId == Guid.Empty)
            return BadRequest(new ListSdrReportsResponse(false, "Customer is required.", null));

        var items = await _reports.ListCustomerSdrReportsForAdminAsync(customerId, ct);
        return Ok(new ListSdrReportsResponse(true, "OK", items.ToList()));
    }

    [HttpGet("{reportId:guid}/download")]
    public async Task<IActionResult> DownloadReport(Guid reportId, CancellationToken ct)
    {
        var adminId = CurrentUser.RequireUserId(User);
        var (allowed, error, path, downloadName) = await _reports.GetDownloadForAdminAsync(
            adminId,
            reportId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            ct);

        if (!allowed)
            return NotFound(new { success = false, message = error ?? "Report not found." });

        var contentType = (downloadName ?? "").EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
            ? "application/pdf"
            : "application/zip";
        return PhysicalFile(path!, contentType, downloadName);
    }

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
        [FromQuery] int pageSize = AdminListQuery.DefaultPageSize,
        [FromQuery] string? dateFrom = null,
        [FromQuery] string? dateTo = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = null,
        [FromQuery] bool export = false,
        CancellationToken ct = default)
    {
        var result = await _reports.ListAdminHistoryAsync(
            search,
            page,
            pageSize,
            dateFrom,
            dateTo,
            sortBy,
            sortDir,
            export,
            ct);
        return Ok(new HistoryResponse(
            result.Success,
            result.Message,
            result.Items.ToList(),
            result.TotalCount,
            result.Page,
            result.PageSize));
    }

    [HttpPost("change-requests")]
    [RequestSizeLimit(104_857_600)]
    public async Task<ActionResult<ChangeRequestResponse>> CreateChangeRequest(
        [FromForm] Guid reportId,
        [FromForm] string requestType,
        [FromForm] string reason,
        [FromForm] string? newOriginalFileName,
        IFormFile? file,
        CancellationToken ct)
    {
        if (reportId == Guid.Empty)
            return BadRequest(new ChangeRequestResponse(false, "Report is required.", null));

        var userId = CurrentUser.RequireUserId(User);
        var isSuperAdmin = CurrentUser.IsSuperAdmin(User);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        var (success, message, requestId) = await _changeRequests.CreateRequestAsync(
            userId,
            isSuperAdmin,
            reportId,
            requestType,
            reason,
            newOriginalFileName,
            file,
            ip,
            ct);

        if (!success)
            return BadRequest(new ChangeRequestResponse(false, message, null));

        return Ok(new ChangeRequestResponse(true, message, requestId));
    }

    [HttpGet("change-requests")]
    public async Task<ActionResult<ChangeRequestListResponse>> ListChangeRequests(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = AdminListQuery.DefaultPageSize,
        CancellationToken ct = default)
    {
        var userId = CurrentUser.RequireUserId(User);
        var isSuperAdmin = CurrentUser.IsSuperAdmin(User);
        var result = await _changeRequests.ListRequestsAsync(userId, isSuperAdmin, status, page, pageSize, ct);
        return Ok(new ChangeRequestListResponse(
            result.Success,
            result.Message,
            result.Items.ToList(),
            result.TotalCount,
            result.Page,
            result.PageSize));
    }

    [HttpGet("change-requests/pending-count")]
    public async Task<ActionResult<PendingCountResponse>> PendingCount(CancellationToken ct)
    {
        if (!CurrentUser.IsSuperAdmin(User))
            return Ok(new PendingCountResponse(true, 0));

        var count = await _changeRequests.GetPendingCountAsync(true, ct);
        return Ok(new PendingCountResponse(true, count));
    }

    [HttpGet("change-requests/{requestId:guid}")]
    public async Task<ActionResult<ChangeRequestDetailResponse>> GetChangeRequest(Guid requestId, CancellationToken ct)
    {
        var userId = CurrentUser.RequireUserId(User);
        var isSuperAdmin = CurrentUser.IsSuperAdmin(User);
        var (success, message, detail) = await _changeRequests.GetRequestDetailAsync(userId, isSuperAdmin, requestId, ct);
        if (!success)
            return BadRequest(new ChangeRequestDetailResponse(false, message, null));
        return Ok(new ChangeRequestDetailResponse(true, message, detail));
    }

    [HttpPost("change-requests/{requestId:guid}/approve")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<ActionResult<ActionResponse>> ApproveChangeRequest(
        Guid requestId,
        [FromBody] ReviewRequestBody? body,
        CancellationToken ct)
    {
        var userId = CurrentUser.RequireUserId(User);
        var (success, message) = await _changeRequests.ApproveRequestAsync(
            userId,
            requestId,
            body?.Remarks,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            ct);
        if (!success)
            return BadRequest(new ActionResponse(false, message));
        return Ok(new ActionResponse(true, message));
    }

    [HttpPost("change-requests/{requestId:guid}/reject")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<ActionResult<ActionResponse>> RejectChangeRequest(
        Guid requestId,
        [FromBody] ReviewRequestBody? body,
        CancellationToken ct)
    {
        var userId = CurrentUser.RequireUserId(User);
        var (success, message) = await _changeRequests.RejectRequestAsync(
            userId,
            requestId,
            body?.Remarks,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            ct);
        if (!success)
            return BadRequest(new ActionResponse(false, message));
        return Ok(new ActionResponse(true, message));
    }

    [HttpDelete("{reportId:guid}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<ActionResult<ActionResponse>> DirectDelete(
        Guid reportId,
        [FromBody] DirectActionBody body,
        CancellationToken ct)
    {
        var userId = CurrentUser.RequireUserId(User);
        var (success, message) = await _changeRequests.DirectSoftDeleteAsync(
            userId,
            reportId,
            body.Reason,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            ct);
        if (!success)
            return BadRequest(new ActionResponse(false, message));
        return Ok(new ActionResponse(true, message));
    }

    [HttpPut("{reportId:guid}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<ActionResult<ActionResponse>> DirectEdit(
        Guid reportId,
        [FromBody] DirectEditBody body,
        CancellationToken ct)
    {
        var userId = CurrentUser.RequireUserId(User);
        var (success, message) = await _changeRequests.DirectEditAsync(
            userId,
            reportId,
            body.OriginalFileName,
            body.Reason,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            ct);
        if (!success)
            return BadRequest(new ActionResponse(false, message));
        return Ok(new ActionResponse(true, message));
    }

    [HttpPost("{reportId:guid}/replace")]
    [Authorize(Policy = "SuperAdminOnly")]
    [RequestSizeLimit(104_857_600)]
    public async Task<ActionResult<ActionResponse>> DirectReplace(
        Guid reportId,
        [FromForm] string reason,
        IFormFile? file,
        CancellationToken ct)
    {
        if (file is null)
            return BadRequest(new ActionResponse(false, "ZIP file is required."));

        var userId = CurrentUser.RequireUserId(User);
        var (success, message) = await _changeRequests.DirectReplaceAsync(
            userId,
            reportId,
            file,
            reason,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            ct);
        if (!success)
            return BadRequest(new ActionResponse(false, message));
        return Ok(new ActionResponse(true, message));
    }

    [HttpGet("{reportId:guid}/audit")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<ActionResult<AuditHistoryResponse>> AuditHistory(Guid reportId, CancellationToken ct)
    {
        var userId = CurrentUser.RequireUserId(User);
        var items = await _changeRequests.ListAuditHistoryAsync(userId, true, reportId, ct);
        return Ok(new AuditHistoryResponse(true, "OK", items.ToList()));
    }

    public sealed record ReviewRequestBody(string? Remarks);

    public sealed record DirectActionBody(string Reason);

    public sealed record DirectEditBody(string OriginalFileName, string Reason);
}
