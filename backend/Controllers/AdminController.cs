using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RB_Website_API.Auth;
using RB_Website_API.Data;
using RB_Website_API.Models;
using System.Security.Claims;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IJwtTokenService _jwt;

    public AdminController(AppDbContext db, IJwtTokenService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    public sealed record AdminLoginRequest(string Identifier, string Password);

    public sealed record AdminLoginResponse(
        bool Success,
        string? Message = null,
        Guid? UserId = null,
        string? Role = null,
        string? Token = null
    );

    public sealed record CreateAdminUserRequest(
        string FullName,
        string Email,
        string Phone,
        string Password
    );

    public sealed record CreateAdminUserResponse(bool Success, string? Message = null, Guid? UserId = null);

    public sealed record DeleteUserResponse(bool Success, string? Message = null);

    public sealed record AdminUserDto(
        Guid UserId,
        string Role,
        string FullName,
        string Email,
        string Phone,
        bool IsActive,
        DateTime CreatedAt
    );

    public sealed record AdminUsersResponse(bool Success, string? Message = null, List<AdminUserDto>? Users = null);

    public sealed record DashboardCountsResponse(
        bool Success,
        string? Message = null,
        int Users = 0,
        int Members = 0,
        int Plans = 0,
        int PaymentOrders = 0,
        int Payments = 0,
        int UserPlans = 0,
        int ActiveSubscriptions = 0,
        int ExpiringSoon = 0,
        int ExpiredSubscriptions = 0,
        int Blogs = 0,
        int Events = 0,
        int Schemes = 0,
        int SchemeNews = 0,
        int SuccessStories = 0,
        int Offers = 0,
        int Pricing = 0
    );

    public sealed record SubscriptionRowDto(
        Guid UserPlanId,
        Guid UserId,
        string FullName,
        string Email,
        string Phone,
        string PlanCode,
        string PlanName,
        DateTime ActiveFrom,
        DateTime? ActiveTo,
        string Status,
        int? DaysRemaining
    );

    public sealed record PlanRowDto(
        int PlanId,
        string Code,
        string Name,
        long TotalAmountPaise,
        int DurationDays,
        bool IsActive
    );

    public sealed record PaymentOrderRowDto(
        Guid PaymentOrderId,
        Guid UserId,
        string FullName,
        string Email,
        string PlanCode,
        string PlanName,
        long TotalAmountPaise,
        string Status,
        DateTime CreatedAt
    );

    public sealed record PaymentRowDto(
        Guid PaymentId,
        Guid PaymentOrderId,
        string FullName,
        string Email,
        string PlanCode,
        long AmountPaise,
        string Status,
        DateTime PaidAt
    );

    public sealed record DashboardDetailResponse(
        bool Success,
        string? Message = null,
        string? Category = null,
        string? Title = null,
        List<AdminUserDto>? Users = null,
        List<MemberDto>? Members = null,
        List<PlanRowDto>? Plans = null,
        List<PaymentOrderRowDto>? PaymentOrders = null,
        List<PaymentRowDto>? Payments = null,
        List<SubscriptionRowDto>? Subscriptions = null
    );

    public sealed record SetAdminActiveRequest(bool IsActive);
    public sealed record SetAdminActiveResponse(bool Success, string? Message = null);

    public sealed record MemberDto(
        Guid UserId,
        string Role,
        string FullName,
        string Email,
        string Phone,
        bool IsActive,
        DateTime CreatedAt
    );

    public sealed record MembersResponse(bool Success, string? Message = null, List<MemberDto>? Users = null);

    public sealed record SetMemberActiveRequest(bool IsActive, string? Reason);
    public sealed record SetMemberActiveResponse(bool Success, string? Message = null);

    [HttpPost("login")]
    public async Task<ActionResult<AdminLoginResponse>> Login([FromBody] AdminLoginRequest? req, CancellationToken ct)
    {
        if (req is null) return BadRequest(new AdminLoginResponse(false, "Request is required."));
        if (string.IsNullOrWhiteSpace(req.Identifier)) return BadRequest(new AdminLoginResponse(false, "Email or phone is required."));
        if (string.IsNullOrWhiteSpace(req.Password)) return BadRequest(new AdminLoginResponse(false, "Password is required."));

        var ident = req.Identifier.Trim();
        var email = ident.Contains('@') ? ident.ToLowerInvariant() : null;
        var phone = email is null ? IndianPhone.Digits(ident) : null;

        User? user = null;
        if (email is not null)
            user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);
        else if (!string.IsNullOrEmpty(phone))
            user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Phone == phone, ct);

        if (user is null)
            return Unauthorized(new AdminLoginResponse(false, "Invalid email/phone or password."));

        var role = (user.Role ?? "").Trim().ToLowerInvariant();
        if (role != "admin" && role != "superadmin")
            return Unauthorized(new AdminLoginResponse(false, "You are not authorized."));

        if (role == "admin" && user.IsActive != true)
            return Unauthorized(new AdminLoginResponse(false, "Admin account is inactive."));

        var ok = PasswordHasher.Verify(req.Password, user.PasswordSalt, user.PasswordHash);
        if (!ok)
            return Unauthorized(new AdminLoginResponse(false, "Invalid email/phone or password."));

        var token = _jwt.CreateToken(user.UserId, role, user.Email);
        return Ok(new AdminLoginResponse(true, "Login successful.", user.UserId, role, token));
    }

    [Authorize(Policy = "SuperAdminOnly")]
    [HttpPost("users")]
    public async Task<ActionResult<CreateAdminUserResponse>> CreateAdminUser([FromBody] CreateAdminUserRequest? req, CancellationToken ct)
    {
        if (req is null) return BadRequest(new CreateAdminUserResponse(false, "Request is required."));
        if (string.IsNullOrWhiteSpace(req.FullName)) return BadRequest(new CreateAdminUserResponse(false, "Full name is required."));
        if (string.IsNullOrWhiteSpace(req.Email)) return BadRequest(new CreateAdminUserResponse(false, "Email is required."));
        if (string.IsNullOrWhiteSpace(req.Phone)) return BadRequest(new CreateAdminUserResponse(false, "Phone is required."));
        if (string.IsNullOrWhiteSpace(req.Password)) return BadRequest(new CreateAdminUserResponse(false, "Password is required."));
        if (req.Password.Trim().Length < 8) return BadRequest(new CreateAdminUserResponse(false, "Password must be at least 8 characters."));

        var email = req.Email.Trim().ToLowerInvariant();
        var phoneDigits = IndianPhone.Digits(req.Phone);
        if (phoneDigits.Length != 10) return BadRequest(new CreateAdminUserResponse(false, "Invalid phone number."));

        var emailExists = await _db.Users.AnyAsync(u => u.Email == email, ct);
        if (emailExists) return Conflict(new CreateAdminUserResponse(false, "Email already exists."));

        var phoneExists = await _db.Users.AnyAsync(u => u.Phone == phoneDigits, ct);
        if (phoneExists) return Conflict(new CreateAdminUserResponse(false, "Phone already exists."));

        var (hash, salt) = PasswordHasher.Hash(req.Password.Trim());
        var now = DateTime.UtcNow;

        var user = new User
        {
            UserId = Guid.NewGuid(),
            Role = "admin",
            FullName = req.FullName.Trim(),
            Email = email,
            Phone = phoneDigits,
            CompanyName = null,
            PasswordHash = hash,
            PasswordSalt = salt,
            EmailVerifiedAt = now,
            PhoneVerifiedAt = now,
            ConsentAccepted = true,
            ConsentAcceptedAt = now,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return Ok(new CreateAdminUserResponse(true, "Admin user created.", user.UserId));
    }

    [Authorize(Policy = "SuperAdminOnly")]
    [HttpDelete("users/{userId:guid}")]
    public async Task<ActionResult<DeleteUserResponse>> DeleteUser(Guid userId, CancellationToken ct)
    {
        if (userId == Guid.Empty) return BadRequest(new DeleteUserResponse(false, "userId is required."));

        var myId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(myId, out var me) && me == userId)
            return BadRequest(new DeleteUserResponse(false, "You cannot delete your own account."));

        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId, ct);
        if (user is null) return NotFound(new DeleteUserResponse(false, "User not found."));

        var targetRole = (user.Role ?? "").Trim().ToLowerInvariant();
        if (targetRole == "superadmin")
            return BadRequest(new DeleteUserResponse(false, "Cannot delete superadmin."));

        if (targetRole == "admin" && user.IsActive == true)
            return BadRequest(new DeleteUserResponse(false, "Deactivate admin before deleting."));

        _db.Users.Remove(user);
        await _db.SaveChangesAsync(ct);
        return Ok(new DeleteUserResponse(true, "User deleted."));
    }

    [Authorize(Policy = "SuperAdminOnly")]
    [HttpPatch("users/{userId:guid}/active")]
    public async Task<ActionResult<SetAdminActiveResponse>> SetAdminActive(Guid userId, [FromBody] SetAdminActiveRequest? req, CancellationToken ct)
    {
        if (userId == Guid.Empty) return BadRequest(new SetAdminActiveResponse(false, "userId is required."));
        if (req is null) return BadRequest(new SetAdminActiveResponse(false, "Request is required."));

        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId, ct);
        if (user is null) return NotFound(new SetAdminActiveResponse(false, "User not found."));

        var role = (user.Role ?? "").Trim().ToLowerInvariant();
        if (role != "admin")
            return BadRequest(new SetAdminActiveResponse(false, "Only admin users can be activated/deactivated."));

        user.IsActive = req.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(new SetAdminActiveResponse(true, req.IsActive ? "Admin activated." : "Admin deactivated."));
    }

    [Authorize(Policy = "SuperAdminOnly")]
    [HttpGet("users")]
    public async Task<ActionResult<AdminUsersResponse>> ListUsers([FromQuery] string? role, CancellationToken ct)
    {
        var roleFilter = (role ?? "").Trim().ToLowerInvariant();
        var q = _db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(roleFilter))
            q = q.Where(u => (u.Role ?? "").ToLower() == roleFilter);

        var users = await q
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new AdminUserDto(u.UserId, u.Role, u.FullName, u.Email, u.Phone, u.IsActive, u.CreatedAt))
            .ToListAsync(ct);

        return Ok(new AdminUsersResponse(true, "OK", users));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpGet("dashboard/counts")]
    public async Task<ActionResult<DashboardCountsResponse>> DashboardCounts(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var expiringCutoff = now.AddDays(30);

        var users = await _db.Users.AsNoTracking().CountAsync(ct);
        var members = await _db.Users.AsNoTracking()
            .CountAsync(u => u.Role != "admin" && u.Role != "superadmin", ct);
        var plans = await _db.Plans.AsNoTracking().CountAsync(ct);
        var paymentOrders = await _db.PaymentOrders.AsNoTracking().CountAsync(ct);
        var payments = await _db.Payments.AsNoTracking().CountAsync(ct);
        var userPlans = await _db.UserPlans.AsNoTracking().CountAsync(ct);

        var activeSubscriptions = await _db.UserPlans.AsNoTracking()
            .CountAsync(up => up.Status == "Active" && (up.ActiveTo == null || up.ActiveTo > now), ct);

        var expiringSoon = await _db.UserPlans.AsNoTracking()
            .CountAsync(up =>
                up.Status == "Active"
                && up.ActiveTo != null
                && up.ActiveTo > now
                && up.ActiveTo <= expiringCutoff, ct);

        var expiredSubscriptions = await _db.UserPlans.AsNoTracking()
            .CountAsync(up =>
                up.Status != "Active"
                || (up.ActiveTo != null && up.ActiveTo <= now), ct);

        return Ok(new DashboardCountsResponse(
            true,
            "OK",
            Users: users,
            Members: members,
            Plans: plans,
            PaymentOrders: paymentOrders,
            Payments: payments,
            UserPlans: userPlans,
            ActiveSubscriptions: activeSubscriptions,
            ExpiringSoon: expiringSoon,
            ExpiredSubscriptions: expiredSubscriptions,
            Blogs: ContentCatalog.Blogs,
            Events: ContentCatalog.Events,
            Schemes: ContentCatalog.Schemes,
            SchemeNews: 0,
            SuccessStories: 0,
            Offers: 0,
            Pricing: plans
        ));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpGet("dashboard/details/{category}")]
    public async Task<ActionResult<DashboardDetailResponse>> DashboardDetails(
        string category,
        [FromQuery] int days = 30,
        CancellationToken ct = default)
    {
        var key = (category ?? "").Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;
        days = Math.Clamp(days, 1, 365);
        var expiringCutoff = now.AddDays(days);

        switch (key)
        {
            case "users":
            {
                var users = await _db.Users.AsNoTracking()
                    .OrderByDescending(u => u.CreatedAt)
                    .Select(u => new AdminUserDto(u.UserId, u.Role, u.FullName, u.Email, u.Phone, u.IsActive, u.CreatedAt))
                    .ToListAsync(ct);
                return Ok(new DashboardDetailResponse(true, "OK", key, "All Users", Users: users));
            }
            case "members":
            {
                var members = await _db.Users.AsNoTracking()
                    .Where(u => u.Role != "admin" && u.Role != "superadmin")
                    .OrderByDescending(u => u.CreatedAt)
                    .Select(u => new MemberDto(u.UserId, u.Role, u.FullName, u.Email, u.Phone, u.IsActive, u.CreatedAt))
                    .ToListAsync(ct);
                return Ok(new DashboardDetailResponse(true, "OK", key, "Members", Members: members));
            }
            case "plans":
            case "pricing":
            {
                var planRows = await _db.Plans.AsNoTracking()
                    .OrderBy(p => p.TotalAmountPaise)
                    .Select(p => new PlanRowDto(p.PlanId, p.Code, p.Name, p.TotalAmountPaise, p.DurationDays, p.IsActive))
                    .ToListAsync(ct);
                return Ok(new DashboardDetailResponse(true, "OK", key, "Membership Plans", Plans: planRows));
            }
            case "payment-orders":
            {
                var orders = await (
                    from po in _db.PaymentOrders.AsNoTracking()
                    join u in _db.Users.AsNoTracking() on po.UserId equals u.UserId
                    join p in _db.Plans.AsNoTracking() on po.PlanId equals p.PlanId
                    orderby po.CreatedAt descending
                    select new PaymentOrderRowDto(
                        po.PaymentOrderId,
                        po.UserId,
                        u.FullName,
                        u.Email,
                        po.PlanCode,
                        p.Name,
                        po.TotalAmountPaise,
                        po.Status,
                        po.CreatedAt)
                ).ToListAsync(ct);
                return Ok(new DashboardDetailResponse(true, "OK", key, "Payment Orders", PaymentOrders: orders));
            }
            case "payments":
            {
                var rows = await (
                    from pay in _db.Payments.AsNoTracking()
                    join po in _db.PaymentOrders.AsNoTracking() on pay.PaymentOrderId equals po.PaymentOrderId
                    join u in _db.Users.AsNoTracking() on po.UserId equals u.UserId
                    orderby pay.PaidAt descending
                    select new PaymentRowDto(
                        pay.PaymentId,
                        pay.PaymentOrderId,
                        u.FullName,
                        u.Email,
                        po.PlanCode,
                        pay.AmountPaise,
                        pay.Status,
                        pay.PaidAt)
                ).ToListAsync(ct);
                return Ok(new DashboardDetailResponse(true, "OK", key, "Payments", Payments: rows));
            }
            case "subscriptions":
            case "user-plans":
            {
                var subs = await LoadSubscriptionRows(
                    _db.UserPlans.AsNoTracking()
                        .Where(up => up.Status == "Active" && (up.ActiveTo == null || up.ActiveTo > now)),
                    now,
                    ct);
                return Ok(new DashboardDetailResponse(true, "OK", key, "Active Subscriptions", Subscriptions: subs));
            }
            case "expiring":
            {
                var subs = await LoadSubscriptionRows(
                    _db.UserPlans.AsNoTracking()
                        .Where(up =>
                            up.Status == "Active"
                            && up.ActiveTo != null
                            && up.ActiveTo > now
                            && up.ActiveTo <= expiringCutoff),
                    now,
                    ct);
                return Ok(new DashboardDetailResponse(
                    true,
                    "OK",
                    key,
                    $"Subscriptions Expiring (next {days} days)",
                    Subscriptions: subs));
            }
            case "expired":
            {
                var subs = await LoadSubscriptionRows(
                    _db.UserPlans.AsNoTracking()
                        .Where(up => up.Status != "Active" || (up.ActiveTo != null && up.ActiveTo <= now)),
                    now,
                    ct);
                return Ok(new DashboardDetailResponse(true, "OK", key, "Expired / Inactive Subscriptions", Subscriptions: subs));
            }
            default:
                return NotFound(new DashboardDetailResponse(false, "Unknown category."));
        }
    }

    private async Task<List<SubscriptionRowDto>> LoadSubscriptionRows(
        IQueryable<UserPlan> query,
        DateTime now,
        CancellationToken ct)
    {
        return await (
            from up in query
            join u in _db.Users.AsNoTracking() on up.UserId equals u.UserId
            join p in _db.Plans.AsNoTracking() on up.PlanId equals p.PlanId
            orderby up.ActiveTo ?? DateTime.MaxValue
            select new SubscriptionRowDto(
                up.UserPlanId,
                up.UserId,
                u.FullName,
                u.Email,
                u.Phone,
                up.PlanCode,
                p.Name,
                up.ActiveFrom,
                up.ActiveTo,
                up.Status,
                up.ActiveTo == null
                    ? (int?)null
                    : (int)Math.Ceiling((up.ActiveTo.Value - now).TotalDays))
        ).ToListAsync(ct);
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpGet("members")]
    public async Task<ActionResult<MembersResponse>> ListMembers([FromQuery] string? role, CancellationToken ct)
    {
        var roleFilter = (role ?? "").Trim().ToLowerInvariant();

        var q = _db.Users.AsNoTracking()
            .Where(u => u.Role != "admin" && u.Role != "superadmin");

        if (!string.IsNullOrWhiteSpace(roleFilter))
            q = q.Where(u => (u.Role ?? "").ToLower() == roleFilter);

        var users = await q
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new MemberDto(u.UserId, u.Role, u.FullName, u.Email, u.Phone, u.IsActive, u.CreatedAt))
            .ToListAsync(ct);

        return Ok(new MembersResponse(true, "OK", users));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpPatch("members/{userId:guid}/active")]
    public async Task<ActionResult<SetMemberActiveResponse>> SetMemberActive(Guid userId, [FromBody] SetMemberActiveRequest? req, CancellationToken ct)
    {
        if (userId == Guid.Empty) return BadRequest(new SetMemberActiveResponse(false, "userId is required."));
        if (req is null) return BadRequest(new SetMemberActiveResponse(false, "Request is required."));

        var actorIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(actorIdRaw, out var actorId) || actorId == Guid.Empty)
            return Unauthorized(new SetMemberActiveResponse(false, "Invalid session."));

        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId, ct);
        if (user is null) return NotFound(new SetMemberActiveResponse(false, "User not found."));

        var targetRole = (user.Role ?? "").Trim().ToLowerInvariant();
        if (targetRole == "admin" || targetRole == "superadmin")
            return BadRequest(new SetMemberActiveResponse(false, "Use admin management for admin users."));

        if (req.IsActive == false && string.IsNullOrWhiteSpace(req.Reason))
            return BadRequest(new SetMemberActiveResponse(false, "Reason is required to deactivate a member."));

        user.IsActive = req.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        _db.UserStatusAudit.Add(new UserStatusAudit
        {
            AuditId = Guid.NewGuid(),
            TargetUserId = user.UserId,
            ActorUserId = actorId,
            NewIsActive = req.IsActive,
            Reason = (req.Reason ?? "").Trim(),
            CreatedAt = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync(ct);

        return Ok(new SetMemberActiveResponse(true, req.IsActive ? "Member activated." : "Member deactivated."));
    }
}

