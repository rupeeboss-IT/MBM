using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RB_Website_API.Auth;
using RB_Website_API.Data;
using RB_Website_API.Models;
using RB_Website_API.Referrals.Services;
using RB_Website_API.Services;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/user")]
public sealed class UserController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IOtpService _otp;
    private readonly IJwtTokenService _jwt;
    private readonly IMemberIdGeneratorService _memberIds;
    private readonly ILeadPushService _leadPush;
    private readonly IEmployeeValidationService _employees;
    private readonly RegistrationWelcomeEmailService _registrationWelcome;
    private readonly ILogger<UserController> _logger;

    public UserController(
        AppDbContext db,
        IOtpService otp,
        IJwtTokenService jwt,
        IMemberIdGeneratorService memberIds,
        ILeadPushService leadPush,
        IEmployeeValidationService employees,
        RegistrationWelcomeEmailService registrationWelcome,
        ILogger<UserController> logger)
    {
        _db = db;
        _otp = otp;
        _jwt = jwt;
        _memberIds = memberIds;
        _leadPush = leadPush;
        _employees = employees;
        _registrationWelcome = registrationWelcome;
        _logger = logger;
    }

    public sealed record RegisterRequest(
        string Role,
        string FullName,
        string Email,
        string Phone,
        string? CompanyName,
        string Password,
        bool ConsentAccepted,
        string? RegistrationSource = null,
        string? AdvisorCode = null
    );

    public sealed record RegisterResponse(
        bool Success,
        string? Message = null,
        Guid? UserId = null,
        string? MemberId = null,
        string? Role = null,
        string? Token = null);

    public sealed record LoginRequest(string Identifier, string Password);
    public sealed record LoginResponse(bool Success, string? Message = null, Guid? UserId = null, string? Role = null, string? Token = null);
    public sealed record ForgotPasswordRequest(string? Identifier);
    public sealed record ForgotPasswordResponse(bool Success, string? Message = null, string? Channel = null, string? Reason = null);
    public sealed record VerifyPasswordResetOtpRequest(string? Identifier, string? Code);
    public sealed record VerifyPasswordResetOtpResponse(bool Success, string? Message = null);
    public sealed record ResetPasswordRequest(string? Identifier, string? NewPassword, string? ConfirmPassword);
    public sealed record ResetPasswordResponse(bool Success, string? Message = null);
    public sealed record MeResponse(
        bool Success,
        string? Message = null,
        Guid? UserId = null,
        string? Role = null,
        string? FullName = null,
        string? Email = null,
        string? Phone = null,
        string? CompanyName = null,
        DateTime? EmailVerifiedAt = null,
        DateTime? PhoneVerifiedAt = null,
        DateTime? CreatedAt = null,
        string? MemberId = null,
        /// <summary>Advisor code captured at registration, if any.</summary>
        string? RegistrationAdvisorCode = null,
        /// <summary>True when registration used a validated non-default advisor (locks attribution).</summary>
        bool RegistrationAdvisorLocked = false,
        string? RegistrationAdvisorDisplayName = null
    );

    [Authorize(Policy = "MemberAccess")]
    [HttpGet("me")]
    public async Task<ActionResult<MeResponse>> Me(CancellationToken ct)
    {
        var userId = CurrentUser.RequireUserId(User);

        var dto = await _db.Users
            .AsNoTracking()
            .Where(u => u.UserId == userId && !u.IsDeleted)
            .Select(u => new
            {
                u.UserId,
                u.Role,
                u.FullName,
                u.Email,
                u.Phone,
                u.CompanyName,
                u.EmailVerifiedAt,
                u.PhoneVerifiedAt,
                u.CreatedAt,
                u.MemberId,
                u.IsActive,
            })
            .FirstOrDefaultAsync(ct);

        if (dto is null) return NotFound(new MeResponse(false, "User not found."));
        if (!dto.IsActive) return Unauthorized(new MeResponse(false, "Account is inactive."));

        var regLead = await _db.UserRegistrationLeads.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, ct);

        string? advisorCode = null;
        var advisorLocked = false;
        string? advisorDisplayName = null;

        if (regLead is not null && !string.IsNullOrWhiteSpace(regLead.AdvisorCode))
        {
            advisorCode = regLead.AdvisorCode.Trim();
            var validation = await _employees.ValidateReferralCodeAsync(advisorCode, ct);
            advisorLocked = validation.IsValid;
            if (validation.IsValid && !string.IsNullOrWhiteSpace(validation.DisplayName))
                advisorDisplayName = StripReferrerRoleSuffix(validation.DisplayName);
        }

        return Ok(new MeResponse(
            true,
            "OK",
            dto.UserId,
            dto.Role,
            dto.FullName,
            dto.Email,
            dto.Phone,
            dto.CompanyName,
            dto.EmailVerifiedAt,
            dto.PhoneVerifiedAt,
            dto.CreatedAt,
            dto.MemberId,
            advisorCode,
            advisorLocked,
            advisorDisplayName
        ));
    }

    private static string StripReferrerRoleSuffix(string displayName)
    {
        var trimmed = displayName.Trim();
        var idx = trimmed.LastIndexOf('(');
        if (idx > 0 && trimmed.EndsWith(')'))
            return trimmed[..idx].Trim();
        return trimmed;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest? req, CancellationToken ct)
    {
        if (req is null) return BadRequest(new LoginResponse(false, "Request is required."));
        if (string.IsNullOrWhiteSpace(req.Identifier)) return BadRequest(new LoginResponse(false, "Email or phone is required."));
        if (string.IsNullOrWhiteSpace(req.Password)) return BadRequest(new LoginResponse(false, "Password is required."));

        var ident = req.Identifier.Trim();
        var email = ident.Contains('@') ? ident.ToLowerInvariant() : null;
        var phone = email is null ? IndianPhone.Digits(ident) : null;

        User? user = null;
        if (email is not null)
        {
            user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);
        }
        else if (!string.IsNullOrEmpty(phone))
        {
            user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Phone == phone, ct);
        }

        if (user is null)
            return Unauthorized(new LoginResponse(false, "Invalid email/phone or password."));

        if (user.IsDeleted)
            return Unauthorized(new LoginResponse(false, "Account is not available."));

        var role = (user.Role ?? "").Trim().ToLowerInvariant();
        if (role == "admin" || role == "superadmin")
            return Unauthorized(new LoginResponse(false, "Please use Admin Login."));

        if (user.IsActive != true)
            return Unauthorized(new LoginResponse(false, "Account is inactive."));

        var ok = PasswordHasher.Verify(req.Password, user.PasswordSalt, user.PasswordHash);
        if (!ok)
            return Unauthorized(new LoginResponse(false, "Invalid email/phone or password."));

        var token = _jwt.CreateToken(user.UserId, role, user.Email);
        return Ok(new LoginResponse(true, "Login successful.", user.UserId, role, token));
    }

    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest? req, CancellationToken ct)
    {
        if (req is null) return BadRequest(new RegisterResponse(false, "Request is required."));
        if (string.IsNullOrWhiteSpace(req.Role)) return BadRequest(new RegisterResponse(false, "Role is required."));
        if (string.IsNullOrWhiteSpace(req.FullName)) return BadRequest(new RegisterResponse(false, "Full name is required."));
        if (string.IsNullOrWhiteSpace(req.Email)) return BadRequest(new RegisterResponse(false, "Email is required."));
        if (string.IsNullOrWhiteSpace(req.Phone)) return BadRequest(new RegisterResponse(false, "Phone is required."));
        if (string.IsNullOrWhiteSpace(req.Password)) return BadRequest(new RegisterResponse(false, "Password is required."));
        if (req.ConsentAccepted != true) return BadRequest(new RegisterResponse(false, "Consent is required."));

        var email = req.Email.Trim().ToLowerInvariant();
        var phoneDigits = IndianPhone.Digits(req.Phone);
        if (phoneDigits.Length != 10) return BadRequest(new RegisterResponse(false, "Invalid phone number."));

        // Require OTP verification before creating the account.
        await _otp.EnsureEmailVerifiedAsync(email, ct);
        await _otp.EnsureSmsVerifiedAsync(phoneDigits, ct);

        var existingByEmail = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);
        if (existingByEmail is not null)
        {
            if (TryIdempotentRegisterResponse(existingByEmail, phoneDigits, req.Password, out var idempotent))
            {
                _logger.LogInformation(
                    "Registration idempotent retry for existing email {Email} (UserId={UserId})",
                    email,
                    existingByEmail.UserId);
                await TryPushRegistrationLeadAsync(existingByEmail.UserId, req.RegistrationSource, req.AdvisorCode, ct);
                return Ok(idempotent);
            }

            return Conflict(new RegisterResponse(false, "Email already exists."));
        }

        var existingByPhone = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Phone == phoneDigits, ct);
        if (existingByPhone is not null)
        {
            if (TryIdempotentRegisterResponse(existingByPhone, phoneDigits, req.Password, out var idempotent))
            {
                _logger.LogInformation(
                    "Registration idempotent retry for existing phone {Phone} (UserId={UserId})",
                    phoneDigits,
                    existingByPhone.UserId);
                await TryPushRegistrationLeadAsync(existingByPhone.UserId, req.RegistrationSource, req.AdvisorCode, ct);
                return Ok(idempotent);
            }

            return Conflict(new RegisterResponse(false, "Phone already exists."));
        }

        var memberRole = req.Role.Trim().ToLowerInvariant();
        if (memberRole is not ("member" or "partner"))
            return BadRequest(new RegisterResponse(false, "Invalid role for registration."));

        var (hash, salt) = PasswordHasher.Hash(req.Password);
        var now = DateTime.Now;

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var allocatedMemberId = await _memberIds.AllocateNextMemberIdAsync(ct);

            var user = new User
            {
                UserId = Guid.NewGuid(),
                MemberId = allocatedMemberId,
                Role = req.Role.Trim(),
                FullName = req.FullName.Trim(),
                Email = email,
                Phone = phoneDigits,
                CompanyName = string.IsNullOrWhiteSpace(req.CompanyName) ? null : req.CompanyName.Trim(),
                PasswordHash = hash,
                PasswordSalt = salt,
                EmailVerifiedAt = now,
                PhoneVerifiedAt = now,
                ConsentAccepted = true,
                ConsentAcceptedAt = now,
                CreatedAt = now,
                UpdatedAt = now,
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            await TryPushRegistrationLeadAsync(user.UserId, req.RegistrationSource, req.AdvisorCode, ct);
            await _registrationWelcome.TrySendAfterRegistrationAsync(user.UserId, ct);

            var token = _jwt.CreateToken(user.UserId, memberRole, user.Email);
            _logger.LogInformation("User registered successfully {UserId} ({Email})", user.UserId, email);
            return Ok(new RegisterResponse(
                true,
                "Account created.",
                user.UserId,
                user.MemberId,
                memberRole,
                token));
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            _logger.LogError(ex, "Registration failed for email {Email}", email);
            throw;
        }
    }

    private async Task TryPushRegistrationLeadAsync(
        Guid userId,
        string? registrationSource,
        string? advisorCode,
        CancellationToken ct)
    {
        try
        {
            await _leadPush.CreateLeadAfterRegistrationAsync(userId, registrationSource, advisorCode, ct);
        }
        catch (Exception leadEx)
        {
            _logger.LogError(leadEx, "Registration lead push failed for user {UserId}", userId);
        }
    }

    /// <summary>
    /// Treats a duplicate registration attempt as success when credentials match an existing member account.
    /// </summary>
    private bool TryIdempotentRegisterResponse(
        User user,
        string phoneDigits,
        string password,
        out RegisterResponse response)
    {
        response = null!;
        if (user.Phone != phoneDigits) return false;
        if (user.IsActive != true) return false;

        var role = (user.Role ?? "").Trim().ToLowerInvariant();
        if (role is not ("member" or "partner")) return false;

        if (!PasswordHasher.Verify(password, user.PasswordSalt, user.PasswordHash)) return false;

        var token = _jwt.CreateToken(user.UserId, role, user.Email);
        response = new RegisterResponse(
            true,
            "You are already registered. Signed you in.",
            user.UserId,
            user.MemberId,
            role,
            token);
        return true;
    }

    [HttpPost("password/forgot")]
    public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword([FromBody] ForgotPasswordRequest? req, CancellationToken ct)
    {
        if (req is null || string.IsNullOrWhiteSpace(req.Identifier))
            return BadRequest(new ForgotPasswordResponse(false, "Please enter your email address or mobile number."));

        if (!UserIdentifier.TryParse(req.Identifier, out var channel, out var normalized, out var validationError))
            return BadRequest(new ForgotPasswordResponse(false, validationError));

        var user = await FindEligiblePasswordResetUserAsync(channel, normalized, ct);
        if (user is null)
        {
            return Ok(new ForgotPasswordResponse(
                false,
                PasswordResetMessages.AccountNotFound(channel),
                channel,
                PasswordResetMessages.AccountNotFoundReason));
        }

        try
        {
            if (channel == "email")
                await _otp.SendPasswordResetEmailOtpAsync(user.Email, user.FullName ?? "Customer", ct);
            else
                await _otp.SendPasswordResetSmsOtpAsync(normalized, ct);
        }
        catch (OtpRateLimitExceededException ex)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests,
                new ForgotPasswordResponse(false, UserFriendlyErrorMapper.GetUserMessage(ex, "forgot_password")));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Forgot password OTP send failed for {Channel} lookup", channel);
            return StatusCode(StatusCodes.Status502BadGateway,
                new ForgotPasswordResponse(false, UserFriendlyErrorMapper.GetUserMessage(ex, "forgot_password")));
        }

        var message = channel == "email"
            ? "We've sent a verification code to your registered email address. Please check your inbox."
            : "We've sent a verification code to your registered mobile number. Delivery may take up to 1–2 minutes.";

        return Ok(new ForgotPasswordResponse(true, message, channel));
    }

    [HttpPost("password/otp/verify")]
    public async Task<ActionResult<VerifyPasswordResetOtpResponse>> VerifyPasswordResetOtp(
        [FromBody] VerifyPasswordResetOtpRequest? req,
        CancellationToken ct)
    {
        if (req is null || string.IsNullOrWhiteSpace(req.Identifier))
            return BadRequest(new VerifyPasswordResetOtpResponse(false, "Please enter your email address or mobile number."));
        if (string.IsNullOrWhiteSpace(req.Code))
            return BadRequest(new VerifyPasswordResetOtpResponse(false, "OTP code is required."));

        if (!UserIdentifier.TryParse(req.Identifier, out var channel, out var normalized, out var validationError))
            return BadRequest(new VerifyPasswordResetOtpResponse(false, validationError));

        var user = await FindEligiblePasswordResetUserAsync(channel, normalized, ct);
        if (user is null)
        {
            return BadRequest(new VerifyPasswordResetOtpResponse(
                false,
                PasswordResetMessages.AccountNotFound(channel)));
        }

        try
        {
            if (channel == "email")
                await _otp.VerifyPasswordResetEmailOtpAsync(user.Email, req.Code.Trim(), ct);
            else
                await _otp.VerifyPasswordResetSmsOtpAsync(user.Phone, req.Code.Trim(), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Password reset OTP verify failed for {Channel}", channel);
            return BadRequest(new VerifyPasswordResetOtpResponse(false, UserFriendlyErrorMapper.GetUserMessage(ex, "verify_password_reset_otp")));
        }

        return Ok(new VerifyPasswordResetOtpResponse(true, "Verification successful."));
    }

    [HttpPost("password/reset")]
    public async Task<ActionResult<ResetPasswordResponse>> ResetPassword([FromBody] ResetPasswordRequest? req, CancellationToken ct)
    {
        if (req is null)
            return BadRequest(new ResetPasswordResponse(false, "Request is required."));
        if (string.IsNullOrWhiteSpace(req.Identifier))
            return BadRequest(new ResetPasswordResponse(false, "Please enter your email address or mobile number."));
        if (string.IsNullOrWhiteSpace(req.NewPassword))
            return BadRequest(new ResetPasswordResponse(false, "New password is required."));
        if (string.IsNullOrWhiteSpace(req.ConfirmPassword))
            return BadRequest(new ResetPasswordResponse(false, "Confirm password is required."));
        if (!string.Equals(req.NewPassword, req.ConfirmPassword, StringComparison.Ordinal))
            return BadRequest(new ResetPasswordResponse(false, "Passwords do not match."));

        if (!UserIdentifier.TryParse(req.Identifier, out var channel, out var normalized, out var validationError))
            return BadRequest(new ResetPasswordResponse(false, validationError));

        var passwordError = PasswordPolicy.Validate(req.NewPassword);
        if (passwordError is not null)
            return BadRequest(new ResetPasswordResponse(false, passwordError));

        var user = await _db.Users.FirstOrDefaultAsync(
            u => channel == "email" ? u.Email == normalized : u.Phone == normalized,
            ct);
        if (user is null || user.IsDeleted)
            return BadRequest(new ResetPasswordResponse(false, "Unable to reset your password. Please try again."));

        var role = (user.Role ?? "").Trim().ToLowerInvariant();
        if (role is not ("member" or "partner") || user.IsActive != true)
            return BadRequest(new ResetPasswordResponse(false, "Unable to reset your password. Please try again."));

        try
        {
            if (channel == "email")
                await _otp.EnsurePasswordResetEmailVerifiedAsync(user.Email, ct);
            else
                await _otp.EnsurePasswordResetSmsVerifiedAsync(user.Phone, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Password reset blocked: OTP not verified for user {UserId}", user.UserId);
            return BadRequest(new ResetPasswordResponse(false, UserFriendlyErrorMapper.GetUserMessage(ex, "reset_password")));
        }

        var (hash, salt) = PasswordHasher.Hash(req.NewPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        user.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync(ct);

        if (channel == "email")
            _otp.InvalidatePasswordResetEmailOtp(user.Email);
        else
            _otp.InvalidatePasswordResetSmsOtp(user.Phone);

        return Ok(new ResetPasswordResponse(true, "Your password has been updated successfully. You can now sign in using your new password."));
    }

    private async Task<User?> FindEligiblePasswordResetUserAsync(string channel, string normalized, CancellationToken ct)
    {
        var user = channel == "email"
            ? await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == normalized, ct)
            : await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Phone == normalized, ct);

        if (user is null || user.IsDeleted)
            return null;

        var role = (user.Role ?? "").Trim().ToLowerInvariant();
        if (role is not ("member" or "partner"))
            return null;
        if (user.IsActive != true)
            return null;

        return user;
    }
}

