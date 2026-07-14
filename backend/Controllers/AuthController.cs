using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RB_Website_API.Auth;
using RB_Website_API.Data;
using RB_Website_API.Services;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IOtpService _otp;
    private readonly ILogger<AuthController> _log;
    private readonly AppDbContext _db;
    private readonly IJwtTokenService _jwt;
    private readonly IConfiguration _config;

    public AuthController(
        IOtpService otp,
        ILogger<AuthController> log,
        AppDbContext db,
        IJwtTokenService jwt,
        IConfiguration config)
    {
        _otp    = otp;
        _log    = log;
        _db     = db;
        _jwt    = jwt;
        _config = config;
    }

    public sealed record OtpLoginRequest(string MobileNumber, string Otp);
    public sealed record OtpLoginResponse(
        bool    Success,
        string? Message = null,
        Guid?   UserId  = null,
        string? Role    = null,
        string? Token   = null);

    public sealed record CheckUserResponse(
        bool    Success,
        string? Message      = null,
        bool    Exists       = false,
        Guid?   UserId       = null,
        string? FullName     = null,
        string? MobileNumber = null,
        string? Role         = null,
        string? Token        = null);

    /// <summary>
    /// Android OTP login. The client must first request an OTP via
    /// POST /api/auth/otp/sms/send, then submit it here to obtain a JWT.
    /// The returned token is identical in claims and validity to the one
    /// issued by the standard username/password login.
    /// </summary>
    [HttpPost("verify-otp")]
    public async Task<ActionResult<OtpLoginResponse>> VerifyOtpLogin(
        [FromBody] OtpLoginRequest? req,
        CancellationToken ct)
    {
        if (req is null)
            return BadRequest(new OtpLoginResponse(false, "Request is required."));

        if (string.IsNullOrWhiteSpace(req.MobileNumber))
            return BadRequest(new OtpLoginResponse(false, "Mobile number is required."));

        if (string.IsNullOrWhiteSpace(req.Otp))
            return BadRequest(new OtpLoginResponse(false, "OTP is required."));

        var phone = IndianPhone.Digits(req.MobileNumber.Trim());
        var sw    = Stopwatch.StartNew();

        _log.LogInformation("OTP login attempt for {Phone}", phone);

        // 1. Validate OTP — fail fast with 401 before touching the DB
        try
        {
            await _otp.VerifySmsOtpAsync(phone, req.Otp.Trim(), ct);
        }
        catch (Exception ex)
        {
            _log.LogWarning(
                ex,
                "OTP login failed — invalid/expired OTP for {Phone} ElapsedMs={ElapsedMs}",
                phone, sw.ElapsedMilliseconds);
            return Unauthorized(new OtpLoginResponse(false, "Invalid or expired OTP."));
        }

        // 2. Resolve user
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Phone == phone, ct);

        if (user is null)
        {
            _log.LogWarning(
                "OTP login failed — user not found for {Phone} ElapsedMs={ElapsedMs}",
                phone, sw.ElapsedMilliseconds);
            return NotFound(new OtpLoginResponse(false, "No account found for this mobile number."));
        }

        // 3. Guard checks (mirrors existing /api/user/login)
        if (user.IsDeleted)
        {
            _log.LogWarning("OTP login rejected — deleted account UserId={UserId}", user.UserId);
            return Unauthorized(new OtpLoginResponse(false, "Account is not available."));
        }

        var role = (user.Role ?? "").Trim().ToLowerInvariant();
        if (role is "admin" or "superadmin")
        {
            _log.LogWarning("OTP login rejected — admin account UserId={UserId}", user.UserId);
            return Unauthorized(new OtpLoginResponse(false, "Admin accounts cannot use OTP login."));
        }

        if (user.IsActive != true)
        {
            _log.LogWarning("OTP login rejected — inactive account UserId={UserId}", user.UserId);
            return Unauthorized(new OtpLoginResponse(false, "Account is inactive."));
        }

        // 4. OTP is valid and user is verified — consume the OTP so it cannot be reused
        _otp.InvalidateSmsOtp(phone);

        // 5. Issue JWT using the same service as the standard login
        var token = _jwt.CreateToken(user.UserId, role, user.Email);

        _log.LogInformation(
            "OTP login successful UserId={UserId} Role={Role} ElapsedMs={ElapsedMs}",
            user.UserId, role, sw.ElapsedMilliseconds);

        return Ok(new OtpLoginResponse(true, "Login successful.", user.UserId, role, token));
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

    /// <summary>
    /// Checks whether an active user account exists for the given mobile number.
    /// Returns basic profile information when found; never exposes credentials or sensitive fields.
    /// When a valid X-Api-Key header is supplied, also issues a JWT token for the matched user.
    /// </summary>
    [HttpGet("check-user")]
    public async Task<ActionResult<CheckUserResponse>> CheckUser(
        [FromQuery] string? mobileNumber,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(mobileNumber))
            return BadRequest(new CheckUserResponse(false, "Mobile number is required."));

        var phone = IndianPhone.Digits(mobileNumber.Trim());

        if (phone.Length != 10 || phone[0] < '6' || phone[0] > '9')
            return BadRequest(new CheckUserResponse(false, "Invalid mobile number."));

        // Determine whether the caller wants a token (opt-in via X-Api-Key header)
        var incomingKey   = Request.Headers["X-Api-Key"].FirstOrDefault();
        var configuredKey = _config["InternalApi:Key"];
        var issueToken    = !string.IsNullOrWhiteSpace(incomingKey);

        if (issueToken && incomingKey != configuredKey)
        {
            _log.LogWarning("Check-user — invalid X-Api-Key for {Phone}", phone);
            return Unauthorized(new CheckUserResponse(false, "Invalid API key."));
        }

        _log.LogInformation("Check-user request for {Phone} IssueToken={IssueToken}", phone, issueToken);

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Phone == phone && !u.IsDeleted && u.IsActive == true, ct);

        if (user is null)
        {
            _log.LogInformation("Check-user — no active user found for {Phone}", phone);
            return Ok(new CheckUserResponse(true, "User not found.", Exists: false));
        }

        var role = (user.Role ?? "").Trim().ToLowerInvariant();

        string? token = null;
        if (issueToken)
        {
            if (role is "admin" or "superadmin")
            {
                _log.LogWarning("Check-user — token denied for admin account UserId={UserId}", user.UserId);
                return Unauthorized(new CheckUserResponse(false, "Token cannot be issued for admin accounts."));
            }

            token = _jwt.CreateToken(user.UserId, role, user.Email);
            _log.LogInformation(
                "Check-user — token issued UserId={UserId} Role={Role}",
                user.UserId, role);
        }
        else
        {
            _log.LogInformation(
                "Check-user — user found UserId={UserId} Role={Role}",
                user.UserId, role);
        }

        return Ok(new CheckUserResponse(
            true,
            "User found.",
            Exists: true,
            UserId: user.UserId,
            FullName: user.FullName,
            MobileNumber: user.Phone,
            Role: user.Role,
            Token: token));
    }
}
