using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RB_Website_API.Auth;
using RB_Website_API.DTO;
using RB_Website_API.Services;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/admin/connect-management")]
[Authorize(Policy = "AdminAccess")]
public sealed class ConnectManagementController : ControllerBase
{
    private readonly IConnectService _connect;

    public ConnectManagementController(IConnectService connect) => _connect = connect;

    [HttpGet("listings")]
    public Task<ConnectAdminListingListResponse> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default) =>
        _connect.AdminListAsync(CurrentUser.GetRole(User) ?? "", page, pageSize, search, role, status, ct);

    [HttpGet("listings/{listingId:guid}")]
    public Task<ConnectAdminListingDetailResponse> Get(Guid listingId, CancellationToken ct) =>
        _connect.AdminGetAsync(CurrentUser.GetRole(User) ?? "", listingId, ct);

    [HttpPost("listings")]
    public Task<ConnectAdminActionResponse> Create(
        [FromBody] CreateConnectAdminListingRequest? req, CancellationToken ct) =>
        req is null
            ? Task.FromResult(new ConnectAdminActionResponse(false, "Request is required."))
            : _connect.AdminCreateAsync(
                CurrentUser.RequireUserId(User), CurrentUser.GetRole(User) ?? "", req, ct);

    [HttpPut("listings/{listingId:guid}")]
    public Task<ConnectAdminActionResponse> Update(
        Guid listingId, [FromBody] UpdateConnectAdminListingRequest? req, CancellationToken ct) =>
        req is null
            ? Task.FromResult(new ConnectAdminActionResponse(false, "Request is required."))
            : _connect.AdminUpdateAsync(
                CurrentUser.RequireUserId(User), CurrentUser.GetRole(User) ?? "", listingId, req, ct);

    [HttpGet("users/search")]
    public Task<ConnectUserSearchResponse> SearchUsers(
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        [FromQuery] int limit = 20,
        CancellationToken ct = default) =>
        _connect.AdminSearchUsersAsync(CurrentUser.GetRole(User) ?? "", search, role, limit, ct);
}
