using Microsoft.AspNetCore.Mvc;
using RB_Website_API.Auth;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IOtpService _otp;

    public AuthController(IOtpService otp)
    {
        _otp = otp;
    }

    [HttpPost("otp/email/send")]
    public async Task<ActionResult<ApiOk>> SendEmailOtp([FromBody] SendEmailOtpRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Email)) return BadRequest(new ApiOk(false, "Email is required."));
        await _otp.SendEmailOtpAsync(req.Email, ct);
        return Ok(new ApiOk(true, "OTP sent to email."));
    }

    [HttpPost("otp/email/verify")]
    public async Task<ActionResult<ApiOk>> VerifyEmailOtp([FromBody] VerifyEmailOtpRequest req, CancellationToken ct)
    {
        try
        {
            await _otp.VerifyEmailOtpAsync(req.Email, req.Code, ct);
            return Ok(new ApiOk(true, "Email verified."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiOk(false, ex.Message));
        }
    }

    [HttpPost("otp/sms/send")]
    public async Task<ActionResult<ApiOk>> SendSmsOtp([FromBody] SendSmsOtpRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Phone)) return BadRequest(new ApiOk(false, "Phone is required."));
        await _otp.SendSmsOtpAsync(req.Phone, ct);
        return Ok(new ApiOk(true, "OTP sent via SMS."));
    }

    [HttpPost("otp/sms/verify")]
    public async Task<ActionResult<ApiOk>> VerifySmsOtp([FromBody] VerifySmsOtpRequest req, CancellationToken ct)
    {
        try
        {
            await _otp.VerifySmsOtpAsync(req.Phone, req.Code, ct);
            return Ok(new ApiOk(true, "Mobile verified."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiOk(false, ex.Message));
        }
    }
}

