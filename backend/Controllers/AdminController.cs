using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RB_Website_API.Auth;
using RB_Website_API.Data;
using RB_Website_API.Models;
using RB_Website_API.Services;
using System.Security.Claims;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IJwtTokenService _jwt;
    private readonly IRecaptchaService _recaptcha;

    public AdminController(AppDbContext db, IJwtTokenService jwt, IRecaptchaService recaptcha)
    {
        _db = db;
        _jwt = jwt;
        _recaptcha = recaptcha;
    }

    public sealed record AdminLoginRequest(string Identifier, string Password, string? RecaptchaToken = null);

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

    public sealed record AdminUsersResponse(
        bool Success,
        string? Message = null,
        List<AdminUserDto>? Users = null,
        int Total = 0,
        int Page = 1,
        int PageSize = 10);

    public sealed record DashboardCountsResponse(
        bool Success,
        string? Message = null,
        int Users = 0,
        int Members = 0,
        int ActiveMembers = 0,
        int InactiveMembers = 0,
        int Plans = 0,
        int PaymentOrders = 0,
        int Payments = 0,
        int UserPlans = 0,
        int ActiveSubscriptions = 0,
        int ExpiringSoon = 0,
        int ExpiredSubscriptions = 0,
        int ReportsGenerated = 0,
        int Blogs = 0,
        int Events = 0,
        int Schemes = 0,
        int SchemeNews = 0,
        int SuccessStories = 0,
        int Offers = 0,
        int Pricing = 0,
        int TotalAdmins = 0,
        int ActiveAdmins = 0,
        int InactiveAdmins = 0,
        int TotalPartners = 0,
        int ActivePartners = 0,
        int InactivePartners = 0,
        int TotalMembersOnly = 0,
        int ActiveMembersOnly = 0,
        int InactiveMembersOnly = 0
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
        List<SubscriptionRowDto>? Subscriptions = null,
        int Total = 0,
        int Page = 1,
        int PageSize = 10
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
        DateTime CreatedAt,
        string? PlanCode = null,
        string? PlanName = null,
        string? AdvisorCode = null
    );

    public sealed record MembersResponse(
        bool Success,
        string? Message = null,
        List<MemberDto>? Users = null,
        int Total = 0,
        int Page = 1,
        int PageSize = 10);

    public sealed record SetMemberActiveRequest(bool IsActive, string? Reason);
    public sealed record SetMemberActiveResponse(bool Success, string? Message = null);

    [HttpPost("login")]
    public async Task<ActionResult<AdminLoginResponse>> Login([FromBody] AdminLoginRequest? req, CancellationToken ct)
    {
        if (req is null) return BadRequest(new AdminLoginResponse(false, "Request is required."));
        if (string.IsNullOrWhiteSpace(req.Identifier)) return BadRequest(new AdminLoginResponse(false, "Email or phone is required."));
        if (string.IsNullOrWhiteSpace(req.Password)) return BadRequest(new AdminLoginResponse(false, "Password is required."));

        var (rcOk, rcReason) = await _recaptcha.VerifyAsync(req.RecaptchaToken, "admin_login", ct);
        if (!rcOk) return BadRequest(new AdminLoginResponse(false, rcReason ?? "reCAPTCHA verification failed."));

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

        if (user.IsDeleted)
            return Unauthorized(new AdminLoginResponse(false, "Account is not available."));

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
        var now = DateTime.Now;

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

        user.IsDeleted = true;
        user.DeletedAt = DateTime.Now;
        user.DeletedByUserId = Guid.TryParse(myId, out var deleter) ? deleter : null;
        user.IsActive = false;
        user.UpdatedAt = DateTime.Now;
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
        user.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync(ct);

        return Ok(new SetAdminActiveResponse(true, req.IsActive ? "Admin activated." : "Admin deactivated."));
    }

    [Authorize(Policy = "SuperAdminOnly")]
    [HttpGet("users")]
    public async Task<ActionResult<AdminUsersResponse>> ListUsers(
        [FromQuery] string? role,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = AdminListQuery.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] string? dateFrom = null,
        [FromQuery] string? dateTo = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = null,
        [FromQuery] bool export = false,
        CancellationToken ct = default)
    {
        var roleFilter = (role ?? "").Trim().ToLowerInvariant();
        (page, pageSize) = AdminListQuery.Normalize(page, pageSize, export);
        search = AdminListQuery.NormalizeSearch(search);
        var from = AdminListQuery.ParseDateFrom(dateFrom);
        var toEx = AdminListQuery.ParseDateToExclusive(dateTo);
        var (sortField, sortAsc) = AdminListQuery.NormalizeSort(sortBy, sortDir);

        var q = _db.Users.AsNoTracking().Where(u => !u.IsDeleted).AsQueryable();
        if (!string.IsNullOrWhiteSpace(roleFilter))
            q = q.Where(u => (u.Role ?? "").ToLower() == roleFilter);

        if (from.HasValue)
            q = q.Where(u => u.CreatedAt >= from.Value);
        if (toEx.HasValue)
            q = q.Where(u => u.CreatedAt < toEx.Value);

        if (search is not null)
        {
            var s = search.ToLowerInvariant();
            q = q.Where(u =>
                u.FullName.ToLower().Contains(s)
                || u.Email.ToLower().Contains(s)
                || u.Phone.Contains(s)
                || (u.Role ?? "").ToLower().Contains(s));
        }

        var total = await q.CountAsync(ct);
        var users = await AdminUserSort.Apply(q, sortField, sortAsc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserDto(u.UserId, u.Role, u.FullName, u.Email, u.Phone, u.IsActive, u.CreatedAt))
            .ToListAsync(ct);

        return Ok(new AdminUsersResponse(true, "OK", users, total, page, pageSize));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpGet("dashboard/counts")]
    public async Task<ActionResult<DashboardCountsResponse>> DashboardCounts(
        [FromQuery] string? dateFrom = null,
        [FromQuery] string? dateTo = null,
        CancellationToken ct = default)
    {
        var now = DateTime.Now;
        var expiringCutoff = now.AddDays(30);
        var from = AdminListQuery.ParseDateFrom(dateFrom);
        var toEx = AdminListQuery.ParseDateToExclusive(dateTo);

        var usersQ = _db.Users.AsNoTracking().Where(u => !u.IsDeleted).AsQueryable();
        if (from.HasValue) usersQ = usersQ.Where(u => u.CreatedAt >= from.Value);
        if (toEx.HasValue) usersQ = usersQ.Where(u => u.CreatedAt < toEx.Value);
        var users = await usersQ.CountAsync(ct);

        var membersQ = usersQ.Where(u => u.Role != "admin" && u.Role != "superadmin");
        var members = await membersQ.CountAsync(ct);
        var activeMembers = await membersQ.CountAsync(u => u.IsActive, ct);
        var inactiveMembers = members - activeMembers;

        var adminsQ = usersQ.Where(u => (u.Role ?? "").ToLower() == "admin");
        var totalAdmins = await adminsQ.CountAsync(ct);
        var activeAdmins = await adminsQ.CountAsync(u => u.IsActive, ct);

        var partnersQ = usersQ.Where(u => (u.Role ?? "").ToLower() == "partner");
        var totalPartners = await partnersQ.CountAsync(ct);
        var activePartners = await partnersQ.CountAsync(u => u.IsActive, ct);

        var membersOnlyQ = usersQ.Where(u => (u.Role ?? "").ToLower() == "member");
        var totalMembersOnly = await membersOnlyQ.CountAsync(ct);
        var activeMembersOnly = await membersOnlyQ.CountAsync(u => u.IsActive, ct);

        var reportsQ = _db.CustomerReports.AsNoTracking().AsQueryable();
        if (from.HasValue) reportsQ = reportsQ.Where(r => r.UploadDate >= from.Value);
        if (toEx.HasValue) reportsQ = reportsQ.Where(r => r.UploadDate < toEx.Value);
        var reportsGenerated = await reportsQ.CountAsync(ct);

        var plans = await _db.Plans.AsNoTracking().CountAsync(p => p.IsActive, ct);

        var ordersQ = _db.PaymentOrders.AsNoTracking().AsQueryable();
        if (from.HasValue) ordersQ = ordersQ.Where(o => o.CreatedAt >= from.Value);
        if (toEx.HasValue) ordersQ = ordersQ.Where(o => o.CreatedAt < toEx.Value);
        var paymentOrders = await ordersQ.CountAsync(ct);

        var paymentsQ = _db.Payments.AsNoTracking().AsQueryable();
        if (from.HasValue) paymentsQ = paymentsQ.Where(p => p.PaidAt >= from.Value);
        if (toEx.HasValue) paymentsQ = paymentsQ.Where(p => p.PaidAt < toEx.Value);
        var payments = await paymentsQ.CountAsync(ct);

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
            ActiveMembers: activeMembers,
            InactiveMembers: inactiveMembers,
            Plans: plans,
            PaymentOrders: paymentOrders,
            Payments: payments,
            UserPlans: userPlans,
            ActiveSubscriptions: activeSubscriptions,
            ExpiringSoon: expiringSoon,
            ExpiredSubscriptions: expiredSubscriptions,
            ReportsGenerated: reportsGenerated,
            Blogs: ContentCatalog.Blogs,
            Events: ContentCatalog.Events,
            Schemes: ContentCatalog.Schemes,
            SchemeNews: 0,
            SuccessStories: 0,
            Offers: 0,
            Pricing: plans,
            TotalAdmins: totalAdmins,
            ActiveAdmins: activeAdmins,
            InactiveAdmins: totalAdmins - activeAdmins,
            TotalPartners: totalPartners,
            ActivePartners: activePartners,
            InactivePartners: totalPartners - activePartners,
            TotalMembersOnly: totalMembersOnly,
            ActiveMembersOnly: activeMembersOnly,
            InactiveMembersOnly: totalMembersOnly - activeMembersOnly
        ));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpGet("dashboard/details/{category}")]
    public async Task<ActionResult<DashboardDetailResponse>> DashboardDetails(
        string category,
        [FromQuery] int days = 30,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = AdminListQuery.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] string? dateFrom = null,
        [FromQuery] string? dateTo = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = null,
        [FromQuery] string? status = null,
        [FromQuery] bool export = false,
        CancellationToken ct = default)
    {
        var key = (category ?? "").Trim().ToLowerInvariant();
        var statusFilter = (status ?? "").Trim();
        var now = DateTime.Now;
        days = Math.Clamp(days, 1, 365);
        var expiringCutoff = now.AddDays(days);
        (page, pageSize) = AdminListQuery.Normalize(page, pageSize, export);
        search = AdminListQuery.NormalizeSearch(search);
        var from = AdminListQuery.ParseDateFrom(dateFrom);
        var toEx = AdminListQuery.ParseDateToExclusive(dateTo);
        var (sortField, sortAsc) = AdminListQuery.NormalizeSort(sortBy, sortDir);

        switch (key)
        {
            case "users":
            {
                var q = _db.Users.AsNoTracking().Where(u => !u.IsDeleted).AsQueryable();
                if (from.HasValue) q = q.Where(u => u.CreatedAt >= from.Value);
                if (toEx.HasValue) q = q.Where(u => u.CreatedAt < toEx.Value);
                if (search is not null)
                {
                    var s = search.ToLowerInvariant();
                    q = q.Where(u =>
                        u.FullName.ToLower().Contains(s)
                        || u.Email.ToLower().Contains(s)
                        || u.Phone.Contains(s)
                        || (u.Role ?? "").ToLower().Contains(s));
                }

                var total = await q.CountAsync(ct);
                var users = await AdminUserSort.Apply(q, sortField, sortAsc)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new AdminUserDto(u.UserId, u.Role, u.FullName, u.Email, u.Phone, u.IsActive, u.CreatedAt))
                    .ToListAsync(ct);
                return Ok(new DashboardDetailResponse(true, "OK", key, "All Users", Users: users, Total: total, Page: page, PageSize: pageSize));
            }
            case "members":
            {
                var (members, total) = await LoadMemberRowsPaged(
                    _db.Users.AsNoTracking().Where(u => !u.IsDeleted && u.Role != "admin" && u.Role != "superadmin"),
                    now,
                    page,
                    pageSize,
                    search,
                    status: null,
                    planCode: null,
                    from,
                    toEx,
                    sortField,
                    sortAsc,
                    ct);
                return Ok(new DashboardDetailResponse(true, "OK", key, "Members", Members: members, Total: total, Page: page, PageSize: pageSize));
            }
            case "plans":
            case "pricing":
            {
                var q = _db.Plans.AsNoTracking().AsQueryable();
                if (search is not null)
                {
                    var s = search.ToLowerInvariant();
                    q = q.Where(p => p.Code.ToLower().Contains(s) || p.Name.ToLower().Contains(s));
                }

                var total = await q.CountAsync(ct);
                var sortedPlans = ApplyPlanSort(q, sortField, sortAsc);
                var planRows = await sortedPlans
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new PlanRowDto(p.PlanId, p.Code, p.Name, p.TotalAmountPaise, p.DurationDays, p.IsActive))
                    .ToListAsync(ct);
                return Ok(new DashboardDetailResponse(true, "OK", key, "Membership Plans", Plans: planRows, Total: total, Page: page, PageSize: pageSize));
            }
            case "payment-orders":
            {
                var q =
                    from po in _db.PaymentOrders.AsNoTracking()
                    join u in _db.Users.AsNoTracking() on po.UserId equals u.UserId
                    join p in _db.Plans.AsNoTracking() on po.PlanId equals p.PlanId
                    select new { po, u, p };

                if (from.HasValue) q = q.Where(x => x.po.CreatedAt >= from.Value);
                if (toEx.HasValue) q = q.Where(x => x.po.CreatedAt < toEx.Value);
                if (!string.IsNullOrWhiteSpace(statusFilter))
                {
                    var st = statusFilter.ToLowerInvariant();
                    q = q.Where(x => x.po.Status.ToLower() == st);
                }
                if (search is not null)
                {
                    var s = search.ToLowerInvariant();
                    q = q.Where(x =>
                        x.u.FullName.ToLower().Contains(s)
                        || x.u.Email.ToLower().Contains(s)
                        || x.po.PlanCode.ToLower().Contains(s)
                        || x.p.Name.ToLower().Contains(s)
                        || x.po.Status.ToLower().Contains(s));
                }

                var total = await q.CountAsync(ct);
                var sortedOrders = sortField switch
                {
                    "name" or "member" or "fullname" => sortAsc ? q.OrderBy(x => x.u.FullName) : q.OrderByDescending(x => x.u.FullName),
                    "email" => sortAsc ? q.OrderBy(x => x.u.Email) : q.OrderByDescending(x => x.u.Email),
                    "plan" => sortAsc ? q.OrderBy(x => x.p.Name) : q.OrderByDescending(x => x.p.Name),
                    "amount" => sortAsc ? q.OrderBy(x => x.po.TotalAmountPaise) : q.OrderByDescending(x => x.po.TotalAmountPaise),
                    "status" => sortAsc ? q.OrderBy(x => x.po.Status) : q.OrderByDescending(x => x.po.Status),
                    _ => sortAsc ? q.OrderBy(x => x.po.CreatedAt) : q.OrderByDescending(x => x.po.CreatedAt),
                };
                var orders = await sortedOrders
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(x => new PaymentOrderRowDto(
                        x.po.PaymentOrderId,
                        x.po.UserId,
                        x.u.FullName,
                        x.u.Email,
                        x.po.PlanCode,
                        x.p.Name,
                        x.po.TotalAmountPaise,
                        x.po.Status,
                        x.po.CreatedAt))
                    .ToListAsync(ct);
                return Ok(new DashboardDetailResponse(true, "OK", key, "Payment Orders", PaymentOrders: orders, Total: total, Page: page, PageSize: pageSize));
            }
            case "payments":
            {
                var q =
                    from pay in _db.Payments.AsNoTracking()
                    join po in _db.PaymentOrders.AsNoTracking() on pay.PaymentOrderId equals po.PaymentOrderId
                    join u in _db.Users.AsNoTracking() on po.UserId equals u.UserId
                    select new { pay, po, u };

                if (from.HasValue) q = q.Where(x => x.pay.PaidAt >= from.Value);
                if (toEx.HasValue) q = q.Where(x => x.pay.PaidAt < toEx.Value);
                if (search is not null)
                {
                    var s = search.ToLowerInvariant();
                    q = q.Where(x =>
                        x.u.FullName.ToLower().Contains(s)
                        || x.u.Email.ToLower().Contains(s)
                        || x.po.PlanCode.ToLower().Contains(s)
                        || x.pay.Status.ToLower().Contains(s));
                }

                var total = await q.CountAsync(ct);
                var sortedPayments = sortField switch
                {
                    "name" or "member" or "fullname" => sortAsc ? q.OrderBy(x => x.u.FullName) : q.OrderByDescending(x => x.u.FullName),
                    "email" => sortAsc ? q.OrderBy(x => x.u.Email) : q.OrderByDescending(x => x.u.Email),
                    "plan" or "plancode" => sortAsc ? q.OrderBy(x => x.po.PlanCode) : q.OrderByDescending(x => x.po.PlanCode),
                    "amount" => sortAsc ? q.OrderBy(x => x.pay.AmountPaise) : q.OrderByDescending(x => x.pay.AmountPaise),
                    "status" => sortAsc ? q.OrderBy(x => x.pay.Status) : q.OrderByDescending(x => x.pay.Status),
                    "paid" or "paidat" or "date" => sortAsc ? q.OrderBy(x => x.pay.PaidAt) : q.OrderByDescending(x => x.pay.PaidAt),
                    _ => sortAsc ? q.OrderBy(x => x.pay.PaidAt) : q.OrderByDescending(x => x.pay.PaidAt),
                };
                var rows = await sortedPayments
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(x => new PaymentRowDto(
                        x.pay.PaymentId,
                        x.pay.PaymentOrderId,
                        x.u.FullName,
                        x.u.Email,
                        x.po.PlanCode,
                        x.pay.AmountPaise,
                        x.pay.Status,
                        x.pay.PaidAt))
                    .ToListAsync(ct);
                return Ok(new DashboardDetailResponse(true, "OK", key, "Payments", Payments: rows, Total: total, Page: page, PageSize: pageSize));
            }
            case "subscriptions":
            case "user-plans":
            {
                var (subs, total) = await LoadSubscriptionRowsPaged(
                    _db.UserPlans.AsNoTracking()
                        .Where(up => up.Status == "Active" && (up.ActiveTo == null || up.ActiveTo > now)),
                    now,
                    page,
                    pageSize,
                    search,
                    from,
                    toEx,
                    sortField,
                    sortAsc,
                    ct);
                return Ok(new DashboardDetailResponse(true, "OK", key, "Active Subscriptions", Subscriptions: subs, Total: total, Page: page, PageSize: pageSize));
            }
            case "expiring":
            {
                var (subs, total) = await LoadSubscriptionRowsPaged(
                    _db.UserPlans.AsNoTracking()
                        .Where(up =>
                            up.Status == "Active"
                            && up.ActiveTo != null
                            && up.ActiveTo > now
                            && up.ActiveTo <= expiringCutoff),
                    now,
                    page,
                    pageSize,
                    search,
                    from,
                    toEx,
                    sortField,
                    sortAsc,
                    ct);
                return Ok(new DashboardDetailResponse(
                    true,
                    "OK",
                    key,
                    $"Subscriptions Expiring (next {days} days)",
                    Subscriptions: subs,
                    Total: total,
                    Page: page,
                    PageSize: pageSize));
            }
            case "expired":
            {
                var (subs, total) = await LoadSubscriptionRowsPaged(
                    _db.UserPlans.AsNoTracking()
                        .Where(up => up.Status != "Active" || (up.ActiveTo != null && up.ActiveTo <= now)),
                    now,
                    page,
                    pageSize,
                    search,
                    from,
                    toEx,
                    sortField,
                    sortAsc,
                    ct);
                return Ok(new DashboardDetailResponse(true, "OK", key, "Expired / Inactive Subscriptions", Subscriptions: subs, Total: total, Page: page, PageSize: pageSize));
            }
            default:
                return NotFound(new DashboardDetailResponse(false, "Unknown category."));
        }
    }

    private async Task<(List<MemberDto> Rows, int Total)> LoadMemberRowsPaged(
        IQueryable<User> query,
        DateTime now,
        int page,
        int pageSize,
        string? search,
        string? status,
        string? planCode,
        DateTime? dateFrom,
        DateTime? dateToExclusive,
        string sortField,
        bool sortAsc,
        CancellationToken ct)
    {
        var q = query;
        var statusFilter = AdminListQuery.NormalizeStatus(status);
        if (statusFilter == "active") q = q.Where(u => u.IsActive);
        else if (statusFilter == "inactive") q = q.Where(u => !u.IsActive);

        if (!string.IsNullOrWhiteSpace(planCode))
        {
            var pc = planCode.Trim();
            q = q.Where(u => _db.UserPlans.Any(up =>
                up.UserId == u.UserId
                && up.PlanCode == pc
                && up.Status == "Active"
                && (up.ActiveTo == null || up.ActiveTo > now)));
        }

        if (dateFrom.HasValue) q = q.Where(u => u.CreatedAt >= dateFrom.Value);
        if (dateToExclusive.HasValue) q = q.Where(u => u.CreatedAt < dateToExclusive.Value);

        if (search is not null)
        {
            var s = search.ToLowerInvariant();
            q = q.Where(u =>
                u.FullName.ToLower().Contains(s)
                || u.Email.ToLower().Contains(s)
                || u.Phone.Contains(s)
                || (u.Role ?? "").ToLower().Contains(s)
                || _db.UserPlans.Any(up =>
                    up.UserId == u.UserId
                    && up.PlanCode.ToLower().Contains(s))
                || _db.UserRegistrationLeads.Any(r =>
                    r.UserId == u.UserId
                    && r.AdvisorCode != null
                    && r.AdvisorCode.ToLower().Contains(s)));
        }

        var total = await q.CountAsync(ct);
        var sorted = sortField is "plan" or "plancode"
            ? (sortAsc
                ? q.OrderBy(u => _db.UserPlans
                    .Where(up => up.UserId == u.UserId && up.Status == "Active" && (up.ActiveTo == null || up.ActiveTo > now))
                    .OrderByDescending(up => up.ActiveFrom)
                    .Select(up => up.PlanCode)
                    .FirstOrDefault())
                : q.OrderByDescending(u => _db.UserPlans
                    .Where(up => up.UserId == u.UserId && up.Status == "Active" && (up.ActiveTo == null || up.ActiveTo > now))
                    .OrderByDescending(up => up.ActiveFrom)
                    .Select(up => up.PlanCode)
                    .FirstOrDefault()))
            : AdminUserSort.Apply(q, sortField, sortAsc);

        var rows = await sorted
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new MemberDto(
                u.UserId,
                u.Role,
                u.FullName,
                u.Email,
                u.Phone,
                u.IsActive,
                u.CreatedAt,
                _db.UserPlans
                    .Where(up => up.UserId == u.UserId && up.Status == "Active" && (up.ActiveTo == null || up.ActiveTo > now))
                    .OrderByDescending(up => up.ActiveFrom)
                    .Select(up => up.PlanCode)
                    .FirstOrDefault(),
                (from up in _db.UserPlans
                 join p in _db.Plans on up.PlanId equals p.PlanId
                 where up.UserId == u.UserId && up.Status == "Active" && (up.ActiveTo == null || up.ActiveTo > now)
                 orderby up.ActiveFrom descending
                 select p.Name).FirstOrDefault(),
                _db.UserRegistrationLeads
                    .Where(r => r.UserId == u.UserId)
                    .Select(r => r.AdvisorCode)
                    .FirstOrDefault()))
            .ToListAsync(ct);

        return (rows, total);
    }

    private async Task<(List<SubscriptionRowDto> Rows, int Total)> LoadSubscriptionRowsPaged(
        IQueryable<UserPlan> query,
        DateTime now,
        int page,
        int pageSize,
        string? search,
        DateTime? dateFrom,
        DateTime? dateToExclusive,
        string sortField,
        bool sortAsc,
        CancellationToken ct)
    {
        var joined =
            from up in query
            join u in _db.Users.AsNoTracking() on up.UserId equals u.UserId
            join p in _db.Plans.AsNoTracking() on up.PlanId equals p.PlanId
            select new { up, u, p };

        if (dateFrom.HasValue) joined = joined.Where(x => x.up.ActiveFrom >= dateFrom.Value);
        if (dateToExclusive.HasValue) joined = joined.Where(x => x.up.ActiveFrom < dateToExclusive.Value);

        if (search is not null)
        {
            var s = search.ToLowerInvariant();
            joined = joined.Where(x =>
                x.u.FullName.ToLower().Contains(s)
                || x.u.Email.ToLower().Contains(s)
                || x.u.Phone.Contains(s)
                || x.up.PlanCode.ToLower().Contains(s)
                || x.p.Name.ToLower().Contains(s)
                || x.up.Status.ToLower().Contains(s));
        }

        var total = await joined.CountAsync(ct);
        var sortedSubs = sortField switch
        {
            "name" or "member" or "fullname" => sortAsc ? joined.OrderBy(x => x.u.FullName) : joined.OrderByDescending(x => x.u.FullName),
            "email" => sortAsc ? joined.OrderBy(x => x.u.Email) : joined.OrderByDescending(x => x.u.Email),
            "phone" => sortAsc ? joined.OrderBy(x => x.u.Phone) : joined.OrderByDescending(x => x.u.Phone),
            "plan" => sortAsc ? joined.OrderBy(x => x.p.Name) : joined.OrderByDescending(x => x.p.Name),
            "started" or "activefrom" => sortAsc ? joined.OrderBy(x => x.up.ActiveFrom) : joined.OrderByDescending(x => x.up.ActiveFrom),
            "ends" or "activeto" => sortAsc ? joined.OrderBy(x => x.up.ActiveTo) : joined.OrderByDescending(x => x.up.ActiveTo),
            "status" => sortAsc ? joined.OrderBy(x => x.up.Status) : joined.OrderByDescending(x => x.up.Status),
            _ => sortAsc ? joined.OrderBy(x => x.up.ActiveTo ?? DateTime.MaxValue) : joined.OrderByDescending(x => x.up.ActiveTo ?? DateTime.MaxValue),
        };
        var rows = await sortedSubs
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new SubscriptionRowDto(
                x.up.UserPlanId,
                x.up.UserId,
                x.u.FullName,
                x.u.Email,
                x.u.Phone,
                x.up.PlanCode,
                x.p.Name,
                x.up.ActiveFrom,
                x.up.ActiveTo,
                x.up.Status,
                x.up.ActiveTo == null
                    ? (int?)null
                    : (int)Math.Ceiling((x.up.ActiveTo.Value - now).TotalDays)))
            .ToListAsync(ct);

        return (rows, total);
    }

    private static IQueryable<Plan> ApplyPlanSort(IQueryable<Plan> q, string sortField, bool asc) =>
        sortField switch
        {
            "code" => asc ? q.OrderBy(p => p.Code) : q.OrderByDescending(p => p.Code),
            "name" => asc ? q.OrderBy(p => p.Name) : q.OrderByDescending(p => p.Name),
            "amount" or "price" => asc ? q.OrderBy(p => p.TotalAmountPaise) : q.OrderByDescending(p => p.TotalAmountPaise),
            "duration" => asc ? q.OrderBy(p => p.DurationDays) : q.OrderByDescending(p => p.DurationDays),
            "status" or "isactive" => asc ? q.OrderBy(p => p.IsActive) : q.OrderByDescending(p => p.IsActive),
            _ => asc ? q.OrderBy(p => p.TotalAmountPaise) : q.OrderByDescending(p => p.TotalAmountPaise),
        };

    [Authorize(Policy = "AdminAccess")]
    [HttpGet("members")]
    public async Task<ActionResult<MembersResponse>> ListMembers(
        [FromQuery] string? role,
        [FromQuery] string? status,
        [FromQuery] string? planCode,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = AdminListQuery.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] string? dateFrom = null,
        [FromQuery] string? dateTo = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = null,
        [FromQuery] bool export = false,
        CancellationToken ct = default)
    {
        var roleFilter = (role ?? "").Trim().ToLowerInvariant();
        (page, pageSize) = AdminListQuery.Normalize(page, pageSize, export);
        search = AdminListQuery.NormalizeSearch(search);
        var from = AdminListQuery.ParseDateFrom(dateFrom);
        var toEx = AdminListQuery.ParseDateToExclusive(dateTo);
        var (sortField, sortAsc) = AdminListQuery.NormalizeSort(sortBy, sortDir);
        var now = DateTime.Now;

        var q = _db.Users.AsNoTracking()
            .Where(u => !u.IsDeleted && u.Role != "admin" && u.Role != "superadmin");

        if (!string.IsNullOrWhiteSpace(roleFilter))
            q = q.Where(u => (u.Role ?? "").ToLower() == roleFilter);

        var (users, total) = await LoadMemberRowsPaged(
            q,
            now,
            page,
            pageSize,
            search,
            status,
            planCode,
            from,
            toEx,
            sortField,
            sortAsc,
            ct);

        return Ok(new MembersResponse(true, "OK", users, total, page, pageSize));
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
        user.UpdatedAt = DateTime.Now;

        _db.UserStatusAudit.Add(new UserStatusAudit
        {
            AuditId = Guid.NewGuid(),
            TargetUserId = user.UserId,
            ActorUserId = actorId,
            NewIsActive = req.IsActive,
            Reason = (req.Reason ?? "").Trim(),
            CreatedAt = DateTime.Now,
        });

        await _db.SaveChangesAsync(ct);

        return Ok(new SetMemberActiveResponse(true, req.IsActive ? "Member activated." : "Member deactivated."));
    }
}

