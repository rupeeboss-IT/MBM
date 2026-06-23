using RB_Website_API.DTO;
using RB_Website_API.Models;

namespace RB_Website_API.Services.IRepository;

public interface IConnectRepository
{
    Task<(IReadOnlyList<ConnectListingRow> Rows, int Total)> SearchListedAsync(
        int page,
        int pageSize,
        string? search,
        string? sector,
        string? state,
        string? turnover,
        Guid? excludeUserId,
        CancellationToken ct);

    Task<ConnectStatsDto> GetStatsAsync(CancellationToken ct);

    Task<ConnectListingRow?> GetListedByUserIdAsync(Guid userId, CancellationToken ct);

    Task<ConnectAdminListing?> GetAdminListingByUserIdAsync(Guid userId, bool track, CancellationToken ct);

    Task<ConnectAdminListing?> GetAdminListingByIdAsync(Guid listingId, bool track, CancellationToken ct);

    Task<ConnectMemberProfile?> GetMemberProfileAsync(Guid userId, bool track, CancellationToken ct);

    Task<(IReadOnlyList<ConnectAdminListingListItemDto> Rows, int Total)> ListAdminListingsAsync(
        int page, int pageSize, string? search, string? role, string? status, CancellationToken ct);

    Task AddAdminListingAsync(ConnectAdminListing listing, CancellationToken ct);

    Task AddMemberProfileAsync(ConnectMemberProfile profile, CancellationToken ct);

    Task<(IReadOnlyList<ConnectUserSearchItemDto> Rows, int Total)> SearchMembersForAdminAsync(
        string? search, string? role, int limit, CancellationToken ct);

    Task<ConnectRequest?> GetRequestBetweenAsync(Guid fromUserId, Guid toUserId, bool track, CancellationToken ct);

    Task<ConnectRequest?> GetRequestByIdAsync(Guid requestId, bool track, CancellationToken ct);

    Task AddRequestAsync(ConnectRequest request, CancellationToken ct);

    Task<IReadOnlyList<ConnectIncomingRequestDto>> ListIncomingPendingAsync(Guid toUserId, CancellationToken ct);

    Task<int> CountUnlocksAsync(Guid viewerUserId, CancellationToken ct);

    Task<bool> HasUnlockAsync(Guid viewerUserId, Guid targetUserId, CancellationToken ct);

    Task AddUnlockAsync(ConnectContactUnlock unlock, CancellationToken ct);

    Task<User?> GetMemberUserAsync(Guid userId, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}

public sealed record ConnectListingRow(
    ConnectAdminListing Listing,
    User User,
    ConnectMemberProfile? MemberProfile);
