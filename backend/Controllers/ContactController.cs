using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RB_Website_API.Services;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/contact")]
public sealed class ContactController : ControllerBase
{
    private readonly IContactService _contact;
    private readonly IRecaptchaService _recaptcha;

    public ContactController(IContactService contact, IRecaptchaService recaptcha)
    {
        _contact = contact;
        _recaptcha = recaptcha;
    }

    public sealed record SubmitContactRequest(
        string FullName,
        string Mobile,
        string Email,
        int SubjectId,
        string Message,
        bool ConsentAccepted,
        string? LeadSource = null,
        string? RecaptchaToken = null);

    public sealed record SubmitContactResponse(
        bool Success,
        string? Message = null,
        int? SubmissionId = null);

    [AllowAnonymous]
    [HttpPost("submit")]
    public async Task<ActionResult<SubmitContactResponse>> Submit(
        [FromBody] SubmitContactRequest? req,
        CancellationToken ct)
    {
        if (req is null)
            return BadRequest(new SubmitContactResponse(false, "Invalid request."));

        var (rcOk, rcReason) = await _recaptcha.VerifyAsync(req.RecaptchaToken, "contact_submit", ct);
        if (!rcOk) return BadRequest(new SubmitContactResponse(false, rcReason ?? "reCAPTCHA verification failed."));

        var (success, message, submissionId) = await _contact.SubmitAsync(
            req.FullName,
            req.Mobile,
            req.Email,
            req.SubjectId,
            req.Message,
            req.ConsentAccepted,
            req.LeadSource,
            ct);

        if (!success)
            return BadRequest(new SubmitContactResponse(false, message));

        return Ok(new SubmitContactResponse(true, message, submissionId));
    }

    public sealed record SubmitCallbackRequest(
        string FullName,
        string Mobile,
        int SubjectId,
        string Message,
        bool ConsentAccepted,
        string? RecaptchaToken = null);

    [AllowAnonymous]
    [HttpPost("callback")]
    public async Task<ActionResult<SubmitContactResponse>> SubmitCallback(
        [FromBody] SubmitCallbackRequest? req,
        CancellationToken ct)
    {
        if (req is null)
            return BadRequest(new SubmitContactResponse(false, "Invalid request."));

        var (rcOk, rcReason) = await _recaptcha.VerifyAsync(req.RecaptchaToken, "home_callback", ct);
        if (!rcOk) return BadRequest(new SubmitContactResponse(false, rcReason ?? "reCAPTCHA verification failed."));

        var (success, message, submissionId) = await _contact.SubmitCallbackAsync(
            req.FullName,
            req.Mobile,
            req.SubjectId,
            req.Message,
            req.ConsentAccepted,
            ct);

        if (!success)
            return BadRequest(new SubmitContactResponse(false, message));

        return Ok(new SubmitContactResponse(true, message, submissionId));
    }
}
