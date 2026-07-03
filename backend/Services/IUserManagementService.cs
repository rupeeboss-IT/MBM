using RB_Website_API.DTO;

namespace RB_Website_API.Services;

public interface IUserManagementService
{
    Task<UserManagementStatsResponse> GetStatsAsync(Guid actorId, string actorRole, CancellationToken ct);

    Task<UserManagementListResponse> ListAdminsAsync(
        Guid actorId,
        string actorRole,
        int page,
        int pageSize,
        string? search,
        string? status,
        string? dateFrom,
        string? dateTo,
        string? sortBy,
        string? sortDir,
        bool export,
        CancellationToken ct);

    Task<UserManagementListResponse> ListPartnersAsync(
        Guid actorId,
        string actorRole,
        int page,
        int pageSize,
        string? search,
        string? status,
        string? dateFrom,
        string? dateTo,
        string? sortBy,
        string? sortDir,
        bool export,
        CancellationToken ct);

    Task<UserManagementListResponse> ListMembersAsync(
        Guid actorId,
        string actorRole,
        int page,
        int pageSize,
        string? search,
        string? status,
        string? dateFrom,
        string? dateTo,
        string? sortBy,
        string? sortDir,
        bool export,
        CancellationToken ct);

    Task<UserManagementDetailResponse> GetUserAsync(Guid actorId, string actorRole, Guid userId, CancellationToken ct);

    Task<UserActionResponse> CreateAdminAsync(Guid actorId, string actorRole, CreateManagedUserRequest req, CancellationToken ct);
    Task<UserActionResponse> CreatePartnerAsync(Guid actorId, string actorRole, CreateManagedUserRequest req, CancellationToken ct);
    Task<UserActionResponse> CreateMemberAsync(Guid actorId, string actorRole, CreateManagedUserRequest req, CancellationToken ct);

    Task<ImportedMemberCreateResponse> CreateImportedMemberAsync(
        Guid actorId,
        string actorRole,
        CreateImportedMemberRequest req,
        CancellationToken ct);

    Task<UserActionResponse> UpdateUserAsync(Guid actorId, string actorRole, Guid userId, UpdateManagedUserRequest req, CancellationToken ct);
    Task<UserActionResponse> SetActiveAsync(Guid actorId, string actorRole, Guid userId, SetManagedUserActiveRequest req, CancellationToken ct);
    Task<UserActionResponse> SoftDeleteAsync(Guid actorId, string actorRole, Guid userId, DeleteManagedUserRequest req, CancellationToken ct);

    Task<UserAuditListResponse> ListAuditAsync(Guid actorId, string actorRole, Guid userId, int page, int pageSize, CancellationToken ct);
    Task<UserStatusHistoryListResponse> ListStatusHistoryAsync(Guid actorId, string actorRole, Guid userId, int page, int pageSize, CancellationToken ct);
}
