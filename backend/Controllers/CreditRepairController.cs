using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RB_Website_API.Features.CreditRepair;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/credit-repair")]
public sealed class CreditRepairController : ControllerBase
{
    private readonly ISubmitCreditRepairLeadHandler _handler;

    public CreditRepairController(ISubmitCreditRepairLeadHandler handler) => _handler = handler;

    public sealed record SubmitCreditRepairLeadRequest(
        string FullName,
        string Mobile,
        string Email,
        bool ConsentAccepted,
        string? AdvisorCode = null);

    public sealed record SubmitCreditRepairLeadResponse(
        bool Success,
        string? Message = null,
        int? LeadId = null);

    [AllowAnonymous]
    [HttpPost("submit")]
    public async Task<ActionResult<SubmitCreditRepairLeadResponse>> Submit(
        [FromBody] SubmitCreditRepairLeadRequest? req,
        CancellationToken ct)
    {
        if (req is null)
            return BadRequest(new SubmitCreditRepairLeadResponse(false, "Invalid request."));

        var correlationId = HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("n");

        var (success, message, leadId) = await _handler.HandleAsync(
            new SubmitCreditRepairLeadCommand(
                req.FullName,
                req.Mobile,
                req.Email,
                req.ConsentAccepted,
                req.AdvisorCode),
            correlationId,
            ct);

        if (!success)
            return BadRequest(new SubmitCreditRepairLeadResponse(false, message));

        return Ok(new SubmitCreditRepairLeadResponse(true, message, leadId));
    }
}

