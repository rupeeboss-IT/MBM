using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RB_Website_API.Auth;
using RB_Website_API.DTO;
using RB_Website_API.Services;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/scheme-discovery")]
public sealed class SchemeDiscoveryController : ControllerBase
{
    private readonly ISchemeDiscoveryService _schemeDiscovery;
    private readonly ILogger<SchemeDiscoveryController> _logger;

    public SchemeDiscoveryController(
        ISchemeDiscoveryService schemeDiscovery,
        ILogger<SchemeDiscoveryController> logger)
    {
        _schemeDiscovery = schemeDiscovery;
        _logger = logger;
    }

    public sealed record SubmitRequestBody(string UdyamNumber);

    [Authorize(Policy = "MemberAccess")]
    [HttpGet("status")]
    public async Task<ActionResult<SchemeDiscoveryStatusDto>> Status(CancellationToken ct)
    {
        var userId = CurrentUser.RequireUserId(User);
        var status = await _schemeDiscovery.GetStatusAsync(userId, ct);
        if (!status.Success)
            return BadRequest(status);
        return Ok(status);
    }

    [Authorize(Policy = "MemberAccess")]
    [HttpPost("draft")]
    public async Task<ActionResult<SchemeDiscoverySubmitResponse>> SaveDraft(
        [FromBody] SubmitRequestBody? body,
        CancellationToken ct)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.UdyamNumber))
            return BadRequest(new SchemeDiscoverySubmitResponse(false, "Udyam Registration Number is required.", null, null));

        var userId = CurrentUser.RequireUserId(User);
        var result = await _schemeDiscovery.SaveDraftAsync(userId, body.UdyamNumber, ct);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    public sealed record FinalizeRequestBody(Guid RequestId);

    [Authorize(Policy = "MemberAccess")]
    [HttpPost("finalize")]
    public async Task<ActionResult<SchemeDiscoverySubmitResponse>> Finalize(
        [FromBody] FinalizeRequestBody? body,
        CancellationToken ct)
    {
        if (body is null || body.RequestId == Guid.Empty)
            return BadRequest(new SchemeDiscoverySubmitResponse(false, "Request id is required.", null, null));

        var userId = CurrentUser.RequireUserId(User);
        _logger.LogInformation(
            "[SchemeDiscovery API] POST finalize START UserId={UserId} RequestId={RequestId}",
            userId,
            body.RequestId);

        var result = await _schemeDiscovery.FinalizeRequestAsync(userId, body.RequestId, ct);

        _logger.LogInformation(
            "[SchemeDiscovery API] POST finalize RESPONSE UserId={UserId} RequestId={RequestId} Success={Success} Outcome={Outcome} Status={Status} ReportId={ReportId}",
            userId,
            body.RequestId,
            result.Success,
            result.Outcome,
            result.Status,
            result.ReportId);

        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize(Policy = "MemberAccess")]
    [HttpPost("request")]
    public async Task<ActionResult<SchemeDiscoverySubmitResponse>> Submit(
        [FromBody] SubmitRequestBody? body,
        CancellationToken ct)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.UdyamNumber))
            return BadRequest(new SchemeDiscoverySubmitResponse(false, "Udyam Registration Number is required.", null, null));

        var userId = CurrentUser.RequireUserId(User);
        var result = await _schemeDiscovery.SubmitRequestAsync(userId, body.UdyamNumber, ct);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    public sealed record EmailReportBody(Guid ReportId);

    [Authorize(Policy = "MemberAccess")]
    [HttpPost("email-report")]
    public async Task<ActionResult<ApiOk>> EmailReport(
        [FromBody] EmailReportBody? body,
        CancellationToken ct)
    {
        if (body is null || body.ReportId == Guid.Empty)
            return BadRequest(new ApiOk(false, "Report id is required."));

        var userId = CurrentUser.RequireUserId(User);
        var result = await _schemeDiscovery.EmailReportAsync(userId, body.ReportId, ct);
        if (!result.Success)
            return BadRequest(new ApiOk(false, result.Message ?? "Could not email your report."));
        return Ok(new ApiOk(true, result.Message ?? "Report sent to your registered email address."));
    }
}
