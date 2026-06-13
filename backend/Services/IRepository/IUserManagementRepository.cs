using RB_Website_API.DTO;
using RB_Website_API.Models;

namespace RB_Website_API.Services.IRepository;

public interface IUserManagementRepository
{
    Task<User?> GetByIdAsync(Guid userId, bool track, CancellationToken ct);
    Task<bool> EmailExistsAsync(string email, Guid? excludeUserId, CancellationToken ct);
    Task<bool> PhoneExistsAsync(string phone, Guid? excludeUserId, CancellationToken ct);
    Task AddUserAsync(User user, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);

    Task<(List<ManagedUserDto> Rows, int Total)> ListUsersAsync(
        string role,
        bool isSuperAdmin,
        Guid actorId,
        int page,
        int pageSize,
        string? search,
        string? status,
        DateTime? dateFrom,
        DateTime? dateToExclusive,
        string sortField,
        bool sortAsc,
        CancellationToken ct);

    Task<UserManagementStatsDto> GetStatsAsync(bool isSuperAdmin, Guid actorId, CancellationToken ct);

    Task<ManagedUserDetailDto?> GetDetailAsync(Guid userId, CancellationToken ct);

    Task AddAuditLogAsync(UserAuditLog entry, CancellationToken ct);
    Task AddStatusHistoryAsync(UserStatusHistory entry, CancellationToken ct);

    Task<(List<UserAuditLogDto> Rows, int Total)> ListAuditLogsAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken ct);

    Task<(List<UserStatusHistoryDto> Rows, int Total)> ListStatusHistoryAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken ct);

    Task<string?> GetUserDisplayNameAsync(Guid userId, CancellationToken ct);
}
