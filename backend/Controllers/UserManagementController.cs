using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RB_Website_API.Auth;
using RB_Website_API.DTO;
using RB_Website_API.Services;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/admin/user-management")]
[Authorize(Policy = "AdminAccess")]
public sealed class UserManagementController : ControllerBase
{
    private readonly IUserManagementService _users;

    public UserManagementController(IUserManagementService users) => _users = users;

    [HttpGet("stats")]
    public Task<UserManagementStatsResponse> Stats(CancellationToken ct) =>
        _users.GetStatsAsync(CurrentUser.RequireUserId(User), CurrentUser.GetRole(User) ?? "", ct);

    [HttpGet("admins")]
    public Task<UserManagementListResponse> ListAdmins(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = AdminListQuery.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] string? dateFrom = null,
        [FromQuery] string? dateTo = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = null,
        [FromQuery] bool export = false,
        CancellationToken ct = default) =>
        _users.ListAdminsAsync(
            CurrentUser.RequireUserId(User), CurrentUser.GetRole(User) ?? "",
            page, pageSize, search, status, dateFrom, dateTo, sortBy, sortDir, export, ct);

    [HttpGet("partners")]
    public Task<UserManagementListResponse> ListPartners(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = AdminListQuery.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] string? dateFrom = null,
        [FromQuery] string? dateTo = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = null,
        [FromQuery] bool export = false,
        CancellationToken ct = default) =>
        _users.ListPartnersAsync(
            CurrentUser.RequireUserId(User), CurrentUser.GetRole(User) ?? "",
            page, pageSize, search, status, dateFrom, dateTo, sortBy, sortDir, export, ct);

    [HttpGet("members")]
    public Task<UserManagementListResponse> ListMembers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = AdminListQuery.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] string? dateFrom = null,
        [FromQuery] string? dateTo = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = null,
        [FromQuery] bool export = false,
        CancellationToken ct = default) =>
        _users.ListMembersAsync(
            CurrentUser.RequireUserId(User), CurrentUser.GetRole(User) ?? "",
            page, pageSize, search, status, dateFrom, dateTo, sortBy, sortDir, export, ct);

    [HttpGet("users/{userId:guid}")]
    public Task<UserManagementDetailResponse> GetUser(Guid userId, CancellationToken ct) =>
        _users.GetUserAsync(CurrentUser.RequireUserId(User), CurrentUser.GetRole(User) ?? "", userId, ct);

    [HttpPost("admins")]
    [Authorize(Policy = "SuperAdminOnly")]
    public Task<UserActionResponse> CreateAdmin([FromBody] CreateManagedUserRequest? req, CancellationToken ct) =>
        req is null
            ? Task.FromResult(new UserActionResponse(false, "Request is required."))
            : _users.CreateAdminAsync(CurrentUser.RequireUserId(User), CurrentUser.GetRole(User) ?? "", req, ct);

    [HttpPost("partners")]
    public Task<UserActionResponse> CreatePartner([FromBody] CreateManagedUserRequest? req, CancellationToken ct) =>
        req is null
            ? Task.FromResult(new UserActionResponse(false, "Request is required."))
            : _users.CreatePartnerAsync(CurrentUser.RequireUserId(User), CurrentUser.GetRole(User) ?? "", req, ct);

    [HttpPost("members")]
    public Task<UserActionResponse> CreateMember([FromBody] CreateManagedUserRequest? req, CancellationToken ct) =>
        req is null
            ? Task.FromResult(new UserActionResponse(false, "Request is required."))
            : _users.CreateMemberAsync(CurrentUser.RequireUserId(User), CurrentUser.GetRole(User) ?? "", req, ct);

    [HttpPut("users/{userId:guid}")]
    public Task<UserActionResponse> UpdateUser(Guid userId, [FromBody] UpdateManagedUserRequest? req, CancellationToken ct) =>
        req is null
            ? Task.FromResult(new UserActionResponse(false, "Request is required."))
            : _users.UpdateUserAsync(CurrentUser.RequireUserId(User), CurrentUser.GetRole(User) ?? "", userId, req, ct);

    [HttpPatch("users/{userId:guid}/active")]
    public Task<UserActionResponse> SetActive(Guid userId, [FromBody] SetManagedUserActiveRequest? req, CancellationToken ct) =>
        req is null
            ? Task.FromResult(new UserActionResponse(false, "Request is required."))
            : _users.SetActiveAsync(CurrentUser.RequireUserId(User), CurrentUser.GetRole(User) ?? "", userId, req, ct);

    [HttpDelete("users/{userId:guid}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public Task<UserActionResponse> DeleteUser(Guid userId, [FromBody] DeleteManagedUserRequest? req, CancellationToken ct) =>
        _users.SoftDeleteAsync(
            CurrentUser.RequireUserId(User),
            CurrentUser.GetRole(User) ?? "",
            userId,
            req ?? new DeleteManagedUserRequest(null),
            ct);

    [HttpGet("users/{userId:guid}/audit")]
    public Task<UserAuditListResponse> Audit(
        Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = AdminListQuery.DefaultPageSize,
        CancellationToken ct = default) =>
        _users.ListAuditAsync(CurrentUser.RequireUserId(User), CurrentUser.GetRole(User) ?? "", userId, page, pageSize, ct);

    [HttpGet("users/{userId:guid}/status-history")]
    public Task<UserStatusHistoryListResponse> StatusHistory(
        Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = AdminListQuery.DefaultPageSize,
        CancellationToken ct = default) =>
        _users.ListStatusHistoryAsync(CurrentUser.RequireUserId(User), CurrentUser.GetRole(User) ?? "", userId, page, pageSize, ct);
}
