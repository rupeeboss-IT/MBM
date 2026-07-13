using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RB_Website_API.Services;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/credit-rebuild")]
public sealed class CreditRebuildController : ControllerBase
{
    private readonly ICreditRebuildService _creditRebuild;
    private readonly IRecaptchaService _recaptcha;

    public CreditRebuildController(ICreditRebuildService creditRebuild, IRecaptchaService recaptcha)
    {
        _creditRebuild = creditRebuild;
        _recaptcha = recaptcha;
    }

    public sealed record SubmitCreditRebuildEnquiryRequest(
        string FullName,
        string Mobile,
        string Email,
        bool ConsentAccepted,
        string? AdvisorCode = null,
        string? RecaptchaToken = null);

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

        var (rcOk, rcReason) = await _recaptcha.VerifyAsync(req.RecaptchaToken, "credit_rebuild_enquiry", ct);
        if (!rcOk) return BadRequest(new SubmitCreditRebuildEnquiryResponse(false, rcReason ?? "reCAPTCHA verification failed."));

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
