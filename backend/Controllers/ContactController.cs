using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RB_Website_API.Services;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/contact")]
public sealed class ContactController : ControllerBase
{
    private readonly IContactService _contact;

    public ContactController(IContactService contact) => _contact = contact;

    public sealed record SubmitContactRequest(
        string FullName,
        string Mobile,
        string Email,
        int SubjectId,
        string Message,
        bool ConsentAccepted);

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

        var (success, message, submissionId) = await _contact.SubmitAsync(
            req.FullName,
            req.Mobile,
            req.Email,
            req.SubjectId,
            req.Message,
            req.ConsentAccepted,
            ct);

        if (!success)
            return BadRequest(new SubmitContactResponse(false, message));

        return Ok(new SubmitContactResponse(true, message, submissionId));
    }
}
