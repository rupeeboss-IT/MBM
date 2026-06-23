using RB_Website_API.DTO;

namespace RB_Website_API.Services;

public interface IConnectService
{
    Task<ConnectSearchResponse> SearchAsync(
        Guid? viewerUserId,
        int page,
        int pageSize,
        string? search,
        string? sector,
        string? state,
        string? turnover,
        CancellationToken ct);

    Task<ConnectProfileDetailResponse> GetProfileAsync(Guid? viewerUserId, Guid targetUserId, CancellationToken ct);

    Task<ConnectAccessSummaryResponse> GetAccessSummaryAsync(Guid userId, CancellationToken ct);

    Task<ConnectRequestActionResponse> SendRequestAsync(
        Guid fromUserId, Guid toUserId, string? message, CancellationToken ct);

    Task<ConnectRequestActionResponse> AcceptRequestAsync(Guid actorUserId, Guid requestId, CancellationToken ct);

    Task<ConnectRequestActionResponse> RejectRequestAsync(Guid actorUserId, Guid requestId, CancellationToken ct);

    Task<ConnectIncomingListResponse> ListIncomingAsync(Guid userId, CancellationToken ct);

    Task<ConnectMemberProfileResponse> GetMyProfileAsync(Guid userId, CancellationToken ct);

    Task<ConnectMemberProfileResponse> UpdateMyProfileAsync(
        Guid userId, UpdateConnectMemberProfileRequest req, CancellationToken ct);

    // Admin
    Task<ConnectAdminListingListResponse> AdminListAsync(
        string actorRole, int page, int pageSize, string? search, string? role, string? status, CancellationToken ct);

    Task<ConnectAdminListingDetailResponse> AdminGetAsync(string actorRole, Guid listingId, CancellationToken ct);

    Task<ConnectAdminActionResponse> AdminCreateAsync(
        Guid actorId, string actorRole, CreateConnectAdminListingRequest req, CancellationToken ct);

    Task<ConnectAdminActionResponse> AdminUpdateAsync(
        Guid actorId, string actorRole, Guid listingId, UpdateConnectAdminListingRequest req, CancellationToken ct);

    Task<ConnectUserSearchResponse> AdminSearchUsersAsync(
        string actorRole, string? search, string? role, int limit, CancellationToken ct);
}
