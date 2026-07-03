using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RB_Website_API.Services;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/credit-rebuild")]
public sealed class CreditRebuildController : ControllerBase
{
    private readonly ICreditRebuildService _creditRebuild;

    public CreditRebuildController(ICreditRebuildService creditRebuild) => _creditRebuild = creditRebuild;

    public sealed record SubmitCreditRebuildEnquiryRequest(
        string FullName,
        string Mobile,
        string Email,
        bool ConsentAccepted,
        string? AdvisorCode = null);

    public sealed record SubmitCreditRebuildEnquiryResponse(
        bool Success,
        string? Message = null,
        int? LeadId = null);

    [AllowAnonymous]
    [HttpPost("enquire")]
    public async Task<ActionResult<SubmitCreditRebuildEnquiryResponse>> Enquire(
        [FromBody] SubmitCreditRebuildEnquiryRequest? req,
        CancellationToken ct)
    {
        if (req is null)
            return BadRequest(new SubmitCreditRebuildEnquiryResponse(false, "Invalid request."));

        var (success, message, leadId) = await _creditRebuild.SubmitEnquiryAsync(
            req.FullName,
            req.Mobile,
            req.Email,
            req.ConsentAccepted,
            req.AdvisorCode,
            ct);

        if (!success)
            return BadRequest(new SubmitCreditRebuildEnquiryResponse(false, message));

        return Ok(new SubmitCreditRebuildEnquiryResponse(true, message, leadId));
    }
}
