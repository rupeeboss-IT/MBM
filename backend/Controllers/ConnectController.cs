using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RB_Website_API.Auth;
using RB_Website_API.DTO;
using RB_Website_API.Services;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/connect")]
public sealed class ConnectController : ControllerBase
{
    private readonly IConnectService _connect;

    public ConnectController(IConnectService connect) => _connect = connect;

    [HttpGet("profiles")]
    public Task<ConnectSearchResponse> Search(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        [FromQuery] string? search = null,
        [FromQuery] string? sector = null,
        [FromQuery] string? state = null,
        [FromQuery] string? turnover = null,
        CancellationToken ct = default) =>
        _connect.SearchAsync(CurrentUser.GetUserId(User), page, pageSize, search, sector, state, turnover, ct);

    [HttpGet("profiles/{userId:guid}")]
    public Task<ConnectProfileDetailResponse> GetProfile(Guid userId, CancellationToken ct) =>
        _connect.GetProfileAsync(CurrentUser.GetUserId(User), userId, ct);

    [HttpGet("access")]
    [Authorize(Policy = "MemberAccess")]
    public Task<ConnectAccessSummaryResponse> Access(CancellationToken ct) =>
        _connect.GetAccessSummaryAsync(CurrentUser.RequireUserId(User), ct);

    [Authorize(Policy = "MemberAccess")]
    [HttpPost("profiles/{userId:guid}/request")]
    public Task<ConnectRequestActionResponse> SendRequest(
        Guid userId, [FromBody] SendConnectRequestBody? body, CancellationToken ct) =>
        _connect.SendRequestAsync(CurrentUser.RequireUserId(User), userId, body?.Message, ct);

    [Authorize(Policy = "MemberAccess")]
    [HttpPost("requests/{requestId:guid}/accept")]
    public Task<ConnectRequestActionResponse> Accept(Guid requestId, CancellationToken ct) =>
        _connect.AcceptRequestAsync(CurrentUser.RequireUserId(User), requestId, ct);

    [Authorize(Policy = "MemberAccess")]
    [HttpPost("requests/{requestId:guid}/reject")]
    public Task<ConnectRequestActionResponse> Reject(Guid requestId, CancellationToken ct) =>
        _connect.RejectRequestAsync(CurrentUser.RequireUserId(User), requestId, ct);

    [Authorize(Policy = "MemberAccess")]
    [HttpGet("requests/incoming")]
    public Task<ConnectIncomingListResponse> Incoming(CancellationToken ct) =>
        _connect.ListIncomingAsync(CurrentUser.RequireUserId(User), ct);

    [Authorize(Policy = "MemberAccess")]
    [HttpGet("my-profile")]
    public Task<ConnectMemberProfileResponse> MyProfile(CancellationToken ct) =>
        _connect.GetMyProfileAsync(CurrentUser.RequireUserId(User), ct);

    [Authorize(Policy = "MemberAccess")]
    [HttpPut("my-profile")]
    public Task<ConnectMemberProfileResponse> UpdateMyProfile(
        [FromBody] UpdateConnectMemberProfileRequest? body, CancellationToken ct) =>
        body is null
            ? Task.FromResult(new ConnectMemberProfileResponse(false, "Request is required."))
            : _connect.UpdateMyProfileAsync(CurrentUser.RequireUserId(User), body, ct);
}
