using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RB_Website_API.Auth;
using RB_Website_API.Data;
using RB_Website_API.Models;
using RB_Website_API.Services;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/user")]
public sealed class UserController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IOtpService _otp;
    private readonly IPasswordResetService _pwReset;
    private readonly IJwtTokenService _jwt;
    private readonly IMemberIdGeneratorService _memberIds;

    public UserController(
        AppDbContext db,
        IOtpService otp,
        IPasswordResetService pwReset,
        IJwtTokenService jwt,
        IMemberIdGeneratorService memberIds)
    {
        _db = db;
        _otp = otp;
        _pwReset = pwReset;
        _jwt = jwt;
        _memberIds = memberIds;
    }

    public sealed record RegisterRequest(
        string Role,
        string FullName,
        string Email,
        string Phone,
        string? CompanyName,
        string Password,
        bool ConsentAccepted
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
    public sealed record ForgotPasswordRequest(string Email);
    public sealed record ForgotPasswordResponse(bool Success, string? Message = null);
    public sealed record ResetPasswordRequest(string Email, string Code, string NewPassword);
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
        string? MemberId = null
    );

    [Authorize(Policy = "MemberAccess")]
    [HttpGet("me")]
    public async Task<ActionResult<MeResponse>> Me(CancellationToken ct)
    {
        var userId = CurrentUser.RequireUserId(User);

        var dto = await _db.Users
            .AsNoTracking()
            .Where(u => u.UserId == userId)
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
                u.MemberId
            })
            .FirstOrDefaultAsync(ct);

        if (dto is null) return NotFound(new MeResponse(false, "User not found."));

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
            dto.MemberId
        ));
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

        var emailExists = await _db.Users.AnyAsync(u => u.Email == email, ct);
        if (emailExists) return Conflict(new RegisterResponse(false, "Email already exists."));

        var phoneExists = await _db.Users.AnyAsync(u => u.Phone == phoneDigits, ct);
        if (phoneExists) return Conflict(new RegisterResponse(false, "Phone already exists."));

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

            var token = _jwt.CreateToken(user.UserId, memberRole, user.Email);
            return Ok(new RegisterResponse(
                true,
                "Account created.",
                user.UserId,
                user.MemberId,
                memberRole,
                token));
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    [HttpPost("password/forgot")]
    public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword([FromBody] ForgotPasswordRequest? req, CancellationToken ct)
    {
        if (req is null || string.IsNullOrWhiteSpace(req.Email))
            return BadRequest(new ForgotPasswordResponse(false, "Email is required."));

        var email = req.Email.Trim().ToLowerInvariant();

        // Do not reveal whether user exists.
        try
        {
            var exists = await _db.Users.AsNoTracking().AnyAsync(u => u.Email == email, ct);
            if (exists)
            {
                await _pwReset.RequestResetAsync(email, ct);
            }
        }
        catch (OtpRateLimitExceededException ex)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests,
                new ForgotPasswordResponse(false, UserFriendlyErrorMapper.GetUserMessage(ex, "forgot_password")));
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway,
                new ForgotPasswordResponse(false, UserFriendlyErrorMapper.GetUserMessage(ex, "forgot_password")));
        }

        return Ok(new ForgotPasswordResponse(true, "If this email is registered, an OTP has been sent."));
    }

    [HttpPost("password/reset")]
    public async Task<ActionResult<ResetPasswordResponse>> ResetPassword([FromBody] ResetPasswordRequest? req, CancellationToken ct)
    {
        if (req is null) return BadRequest(new ResetPasswordResponse(false, "Request is required."));
        if (string.IsNullOrWhiteSpace(req.Email)) return BadRequest(new ResetPasswordResponse(false, "Email is required."));
        if (string.IsNullOrWhiteSpace(req.Code)) return BadRequest(new ResetPasswordResponse(false, "OTP is required."));
        if (string.IsNullOrWhiteSpace(req.NewPassword)) return BadRequest(new ResetPasswordResponse(false, "New password is required."));
        if (req.NewPassword.Length < 8) return BadRequest(new ResetPasswordResponse(false, "Password must be at least 8 characters."));

        var email = req.Email.Trim().ToLowerInvariant();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null)
            return Ok(new ResetPasswordResponse(true, "Password updated.")); // do not reveal

        try
        {
            await _pwReset.ResetAsync(email, req.Code.Trim(), req.NewPassword, ct);
        }
        catch (Exception ex)
        {
            return BadRequest(new ResetPasswordResponse(false, UserFriendlyErrorMapper.GetUserMessage(ex, "reset_password")));
        }

        var (hash, salt) = PasswordHasher.Hash(req.NewPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        user.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync(ct);
        return Ok(new ResetPasswordResponse(true, "Password updated."));
    }
}

