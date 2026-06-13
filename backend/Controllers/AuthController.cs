using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RB_Website_API.Auth;
using RB_Website_API.Services;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IOtpService _otp;
    private readonly ILogger<AuthController> _log;

    public AuthController(IOtpService otp, ILogger<AuthController> log)
    {
        _otp = otp;
        _log = log;
    }

    [HttpPost("otp/email/send")]
    public async Task<ActionResult<ApiOk>> SendEmailOtp([FromBody] SendEmailOtpRequest? req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req?.Email))
        {
            _log.LogWarning("Registration OTP request rejected: email is required.");
            return BadRequest(new ApiOk(false, "Email is required."));
        }

        var email = req.Email.Trim();
        _log.LogInformation("Registration OTP request received for {Email}", email);
        _log.LogInformation("Registration OTP email validation passed for {Email}", email);

        var sw = Stopwatch.StartNew();
        try
        {
            await _otp.SendEmailOtpAsync(email, ct);
            _log.LogInformation(
                "Registration OTP send completed for {Email} ElapsedMs={ElapsedMs}",
                email,
                sw.ElapsedMilliseconds);
            return Ok(new ApiOk(true, "OTP sent to email."));
        }
        catch (OtpRateLimitExceededException ex)
        {
            _log.LogWarning(
                ex,
                "Email OTP rate limit for {Email} ElapsedMs={ElapsedMs}",
                email,
                sw.ElapsedMilliseconds);
            return StatusCode(StatusCodes.Status429TooManyRequests,
                new ApiOk(false, UserFriendlyErrorMapper.GetUserMessage(ex, "send_email_otp")));
        }
        catch (Exception ex)
        {
            _log.LogError(
                ex,
                "Send email OTP failed for {Email} ElapsedMs={ElapsedMs} ClientDisconnected={ClientDisconnected}",
                email,
                sw.ElapsedMilliseconds,
                ct.IsCancellationRequested);
            return BadRequest(new ApiOk(false, UserFriendlyErrorMapper.GetUserMessage(ex, "send_email_otp")));
        }
    }

    [HttpPost("otp/email/verify")]
    public async Task<ActionResult<ApiOk>> VerifyEmailOtp([FromBody] VerifyEmailOtpRequest? req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req?.Email)) return BadRequest(new ApiOk(false, "Email is required."));
        if (string.IsNullOrWhiteSpace(req.Code)) return BadRequest(new ApiOk(false, "OTP code is required."));
        try
        {
            await _otp.VerifyEmailOtpAsync(req.Email.Trim(), req.Code.Trim(), ct);
            return Ok(new ApiOk(true, "Email verified."));
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Verify email OTP failed for {Email}", req.Email);
            return BadRequest(new ApiOk(false, UserFriendlyErrorMapper.GetUserMessage(ex, "verify_email_otp")));
        }
    }

    [HttpPost("otp/sms/send")]
    public async Task<ActionResult<ApiOk>> SendSmsOtp([FromBody] SendSmsOtpRequest? req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req?.Phone)) return BadRequest(new ApiOk(false, "Phone is required."));
        try
        {
            await _otp.SendSmsOtpAsync(req.Phone.Trim(), ct);
            return Ok(new ApiOk(true, "OTP sent via SMS."));
        }
        catch (OtpRateLimitExceededException ex)
        {
            _log.LogWarning(ex, "SMS OTP rate limit for {Phone}", req.Phone);
            return StatusCode(StatusCodes.Status429TooManyRequests,
                new ApiOk(false, UserFriendlyErrorMapper.GetUserMessage(ex, "send_sms_otp")));
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Send SMS OTP failed for {Phone}", req.Phone);
            return BadRequest(new ApiOk(false, UserFriendlyErrorMapper.GetUserMessage(ex, "send_sms_otp")));
        }
    }

    [HttpPost("otp/sms/verify")]
    public async Task<ActionResult<ApiOk>> VerifySmsOtp([FromBody] VerifySmsOtpRequest? req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req?.Phone)) return BadRequest(new ApiOk(false, "Phone is required."));
        if (string.IsNullOrWhiteSpace(req.Code)) return BadRequest(new ApiOk(false, "OTP code is required."));
        try
        {
            await _otp.VerifySmsOtpAsync(req.Phone.Trim(), req.Code.Trim(), ct);
            return Ok(new ApiOk(true, "Mobile verified."));
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Verify SMS OTP failed for {Phone}", req.Phone);
            return BadRequest(new ApiOk(false, UserFriendlyErrorMapper.GetUserMessage(ex, "verify_sms_otp")));
        }
    }
}
