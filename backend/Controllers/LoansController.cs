using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RB_Website_API.Services;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/loans")]
public sealed class LoansController : ControllerBase
{
    private readonly ILoanApplicationService _loans;
    private readonly IRecaptchaService _recaptcha;

    public LoansController(ILoanApplicationService loans, IRecaptchaService recaptcha)
    {
        _loans = loans;
        _recaptcha = recaptcha;
    }

    public sealed record SubmitLoanApplicationRequest(
        string FullName,
        string Mobile,
        string? Email,
        string Pincode,
        int LoanTypeId,
        string LoanAmount,
        bool ConsentAccepted,
        string? ReferralCode = null,
        string? RecaptchaToken = null);

    public sealed record SubmitLoanApplicationResponse(
        bool Success,
        string? Message = null,
        int? LeadId = null);

    [AllowAnonymous]
    [HttpPost("apply")]
    public async Task<ActionResult<SubmitLoanApplicationResponse>> Apply(
        [FromBody] SubmitLoanApplicationRequest? req,
        CancellationToken ct)
    {
        if (req is null)
            return BadRequest(new SubmitLoanApplicationResponse(false, "Invalid request."));

        var (rcOk, rcReason) = await _recaptcha.VerifyAsync(req.RecaptchaToken, "loan_apply", ct);
        if (!rcOk) return BadRequest(new SubmitLoanApplicationResponse(false, rcReason ?? "reCAPTCHA verification failed."));

        var (success, message, leadId) = await _loans.SubmitAsync(
            req.FullName,
            req.Mobile,
            req.Email,
            req.Pincode,
            req.LoanTypeId,
            req.LoanAmount,
            req.ConsentAccepted,
            req.ReferralCode,
            ct);

        if (!success)
            return BadRequest(new SubmitLoanApplicationResponse(false, message));

        return Ok(new SubmitLoanApplicationResponse(true, message, leadId));
    }
}
