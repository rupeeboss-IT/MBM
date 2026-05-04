using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RB_Website_API.Auth;
using RB_Website_API.Data;
using RB_Website_API.Models;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/user")]
public sealed class UserController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IOtpService _otp;

    public UserController(AppDbContext db, IOtpService otp)
    {
        _db = db;
        _otp = otp;
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

    public sealed record RegisterResponse(bool Success, string? Message = null, Guid? UserId = null);

    public sealed record LoginRequest(string Identifier, string Password);
    public sealed record LoginResponse(bool Success, string? Message = null, Guid? UserId = null);
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
        DateTime? CreatedAt = null
    );

    [HttpGet("me")]
    public async Task<ActionResult<MeResponse>> Me([FromQuery] Guid userId, CancellationToken ct)
    {
        if (userId == Guid.Empty) return BadRequest(new MeResponse(false, "userId is required."));

        // Projection avoids fetching varbinary PasswordHash/PasswordSalt which are large and not needed for profile.
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
                u.CreatedAt
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
            dto.CreatedAt
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

        var ok = PasswordHasher.Verify(req.Password, user.PasswordSalt, user.PasswordHash);
        if (!ok)
            return Unauthorized(new LoginResponse(false, "Invalid email/phone or password."));

        return Ok(new LoginResponse(true, "Login successful.", user.UserId));
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

        var (hash, salt) = PasswordHasher.Hash(req.Password);
        var now = DateTime.UtcNow;

        var user = new User
        {
            UserId = Guid.NewGuid(),
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

        return Ok(new RegisterResponse(true, "Account created.", user.UserId));
    }
}

