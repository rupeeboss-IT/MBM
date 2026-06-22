using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RB_Website_API.Referrals.Services;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/referral")]
public sealed class ReferralController : ControllerBase
{
    private readonly IEmployeeValidationService _employees;

    public ReferralController(IEmployeeValidationService employees)
    {
        _employees = employees;
    }

    public sealed record ValidateReferralRequest(string ReferralCode);

    public sealed record ValidateReferralResponse(
        bool Success,
        string? Message = null,
        int? EmployeeId = null,
        string? EmployeeName = null,
        string? ReferralCode = null,
        string? ReferralType = null,
        string? DisplayName = null,
        int? BrokerId = null,
        bool Inactive = false);

    // Keep it public: payment page needs it before login sometimes.
    [AllowAnonymous]
    [HttpPost("validate")]
    public async Task<ActionResult<ValidateReferralResponse>> Validate([FromBody] ValidateReferralRequest? req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req?.ReferralCode))
            return BadRequest(new ValidateReferralResponse(false, "Referral code is required."));

        var result = await _employees.ValidateReferralCodeAsync(req.ReferralCode, ct);
        if (!result.IsValid)
        {
            return Ok(new ValidateReferralResponse(
                false,
                result.Message,
                Inactive: result.IsInactive));
        }

        return Ok(new ValidateReferralResponse(
            true,
            "OK",
            result.EmployeeId,
            result.DisplayName,
            result.ReferralCode,
            result.ReferralType?.ToString(),
            result.DisplayName,
            result.BrokerId));
    }
}
