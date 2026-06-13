using System.Text.Json;
using RB_Website_API.Auth;
using RB_Website_API.DTO;
using RB_Website_API.Models;
using RB_Website_API.Services.IRepository;

namespace RB_Website_API.Services;

public sealed class UserManagementService : IUserManagementService
{
    private readonly IUserManagementRepository _repo;
    private readonly IMemberIdGeneratorService _memberIds;

    public UserManagementService(IUserManagementRepository repo, IMemberIdGeneratorService memberIds)
    {
        _repo = repo;
        _memberIds = memberIds;
    }

    public async Task<UserManagementStatsResponse> GetStatsAsync(Guid actorId, string actorRole, CancellationToken ct)
    {
        if (!UserRoles.IsAdminRole(actorRole))
            return new UserManagementStatsResponse(false, "Not authorized.");

        var stats = await _repo.GetStatsAsync(UserRoles.IsSuperAdmin(actorRole), actorId, ct);
        return new UserManagementStatsResponse(true, "OK", stats);
    }

    public Task<UserManagementListResponse> ListAdminsAsync(
        Guid actorId, string actorRole, int page, int pageSize, string? search, string? status,
        string? dateFrom, string? dateTo, string? sortBy, string? sortDir, bool export, CancellationToken ct)
    {
        if (!UserRoles.IsSuperAdmin(actorRole))
            return Task.FromResult(new UserManagementListResponse(false, "Only Super Admin can view admin users."));

        return ListByRoleAsync(UserRoles.Admin, actorId, actorRole, page, pageSize, search, status, dateFrom, dateTo, sortBy, sortDir, export, ct);
    }

    public Task<UserManagementListResponse> ListPartnersAsync(
        Guid actorId, string actorRole, int page, int pageSize, string? search, string? status,
        string? dateFrom, string? dateTo, string? sortBy, string? sortDir, bool export, CancellationToken ct) =>
        ListByRoleAsync(UserRoles.Partner, actorId, actorRole, page, pageSize, search, status, dateFrom, dateTo, sortBy, sortDir, export, ct);

    public Task<UserManagementListResponse> ListMembersAsync(
        Guid actorId, string actorRole, int page, int pageSize, string? search, string? status,
        string? dateFrom, string? dateTo, string? sortBy, string? sortDir, bool export, CancellationToken ct) =>
        ListByRoleAsync(UserRoles.Member, actorId, actorRole, page, pageSize, search, status, dateFrom, dateTo, sortBy, sortDir, export, ct);

    private async Task<UserManagementListResponse> ListByRoleAsync(
        string role, Guid actorId, string actorRole, int page, int pageSize, string? search, string? status,
        string? dateFrom, string? dateTo, string? sortBy, string? sortDir, bool export, CancellationToken ct)
    {
        if (!UserRoles.IsAdminRole(actorRole))
            return new UserManagementListResponse(false, "Not authorized.");

        (page, pageSize) = AdminListQuery.Normalize(page, pageSize, export);
        search = AdminListQuery.NormalizeSearch(search);
        var from = AdminListQuery.ParseDateFrom(dateFrom);
        var toEx = AdminListQuery.ParseDateToExclusive(dateTo);
        var (sortField, sortAsc) = AdminListQuery.NormalizeSort(sortBy, sortDir);

        var (rows, total) = await _repo.ListUsersAsync(
            role,
            UserRoles.IsSuperAdmin(actorRole),
            actorId,
            page,
            pageSize,
            search,
            status,
            from,
            toEx,
            sortField,
            sortAsc,
            ct);

        return new UserManagementListResponse(true, "OK", rows, total, page, pageSize);
    }

    public async Task<UserManagementDetailResponse> GetUserAsync(Guid actorId, string actorRole, Guid userId, CancellationToken ct)
    {
        if (!UserRoles.IsAdminRole(actorRole))
            return new UserManagementDetailResponse(false, "Not authorized.");

        var detail = await _repo.GetDetailAsync(userId, ct);
        if (detail is null)
            return new UserManagementDetailResponse(false, "User not found.");

        if (!CanManage(actorRole, actorId, detail.Role, detail.CreatedByUserId))
            return new UserManagementDetailResponse(false, "Not authorized to view this user.");

        return new UserManagementDetailResponse(true, "OK", detail);
    }

    public Task<UserActionResponse> CreateAdminAsync(Guid actorId, string actorRole, CreateManagedUserRequest req, CancellationToken ct)
    {
        if (!UserRoles.IsSuperAdmin(actorRole))
            return Task.FromResult(new UserActionResponse(false, "Only Super Admin can create admin users."));

        return CreateUserInternalAsync(actorId, UserRoles.Admin, req, ct);
    }

    public async Task<UserActionResponse> CreatePartnerAsync(Guid actorId, string actorRole, CreateManagedUserRequest req, CancellationToken ct)
    {
        if (!UserRoles.IsAdminRole(actorRole))
            return new UserActionResponse(false, "Not authorized.");

        return await CreateUserInternalAsync(actorId, UserRoles.Partner, req, ct);
    }

    public async Task<UserActionResponse> CreateMemberAsync(Guid actorId, string actorRole, CreateManagedUserRequest req, CancellationToken ct)
    {
        if (!UserRoles.IsAdminRole(actorRole))
            return new UserActionResponse(false, "Not authorized.");

        return await CreateUserInternalAsync(actorId, UserRoles.Member, req, ct);
    }

    public async Task<UserActionResponse> UpdateUserAsync(
        Guid actorId, string actorRole, Guid userId, UpdateManagedUserRequest req, CancellationToken ct)
    {
        if (!UserRoles.IsAdminRole(actorRole))
            return new UserActionResponse(false, "Not authorized.");

        var user = await _repo.GetByIdAsync(userId, track: true, ct);
        if (user is null)
            return new UserActionResponse(false, "User not found.");

        var targetRole = UserRoles.Normalize(user.Role) ?? "";
        if (!CanManage(actorRole, actorId, targetRole, user.CreatedByUserId))
            return new UserActionResponse(false, "Not authorized to edit this user.");

        if (targetRole is UserRoles.SuperAdmin)
            return new UserActionResponse(false, "Super Admin profile cannot be edited here.");

        if (targetRole is UserRoles.Admin && !UserRoles.IsSuperAdmin(actorRole))
            return new UserActionResponse(false, "Only Super Admin can edit admin users.");

        var validation = ValidateProfile(req.FullName, req.Email, req.Phone, req.Password, requirePassword: false);
        if (validation is not null)
            return new UserActionResponse(false, validation);

        var email = req.Email.Trim().ToLowerInvariant();
        var phone = IndianPhone.Digits(req.Phone);
        if (await _repo.EmailExistsAsync(email, userId, ct))
            return new UserActionResponse(false, "Email already exists.");
        if (await _repo.PhoneExistsAsync(phone, userId, ct))
            return new UserActionResponse(false, "Phone already exists.");

        var previous = SnapshotUser(user);
        user.FullName = req.FullName.Trim();
        user.Email = email;
        user.Phone = phone;
        user.CompanyName = string.IsNullOrWhiteSpace(req.CompanyName) ? null : req.CompanyName.Trim();
        user.UpdatedAt = DateTime.Now;

        if (!string.IsNullOrWhiteSpace(req.Password))
        {
            if (req.Password.Trim().Length < 8)
                return new UserActionResponse(false, "Password must be at least 8 characters.");
            var (hash, salt) = PasswordHasher.Hash(req.Password.Trim());
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
        }

        await _repo.SaveChangesAsync(ct);
        await WriteAuditAsync(user, UserAuditActions.Updated, actorId, previous, SnapshotUser(user), null, ct);
        return new UserActionResponse(true, "User updated.", user.UserId);
    }

    public async Task<UserActionResponse> SetActiveAsync(
        Guid actorId, string actorRole, Guid userId, SetManagedUserActiveRequest req, CancellationToken ct)
    {
        if (!UserRoles.IsAdminRole(actorRole))
            return new UserActionResponse(false, "Not authorized.");

        var user = await _repo.GetByIdAsync(userId, track: true, ct);
        if (user is null)
            return new UserActionResponse(false, "User not found.");

        var targetRole = UserRoles.Normalize(user.Role) ?? "";
        if (!CanManage(actorRole, actorId, targetRole, user.CreatedByUserId))
            return new UserActionResponse(false, "Not authorized to change status for this user.");

        if (targetRole is UserRoles.SuperAdmin)
            return new UserActionResponse(false, "Super Admin status cannot be changed.");

        if (targetRole is UserRoles.Admin && !UserRoles.IsSuperAdmin(actorRole))
            return new UserActionResponse(false, "Only Super Admin can activate/deactivate admin users.");

        if (req.IsActive == false && string.IsNullOrWhiteSpace(req.Remarks))
            return new UserActionResponse(false, "Remarks are required to deactivate a user.");

        if (user.IsActive == req.IsActive)
            return new UserActionResponse(true, req.IsActive ? "User is already active." : "User is already inactive.", user.UserId);

        var oldStatus = UserStatusLabels.FromBool(user.IsActive);
        user.IsActive = req.IsActive;
        user.UpdatedAt = DateTime.Now;
        await _repo.SaveChangesAsync(ct);

        var action = req.IsActive ? UserAuditActions.Activated : UserAuditActions.Deactivated;
        var remarks = (req.Remarks ?? "").Trim();
        await WriteStatusHistoryAsync(user, action, actorId, oldStatus, UserStatusLabels.FromBool(req.IsActive), remarks, ct);
        await WriteAuditAsync(
            user,
            action,
            actorId,
            new { IsActive = oldStatus },
            new { IsActive = UserStatusLabels.FromBool(req.IsActive) },
            remarks,
            ct);

        return new UserActionResponse(true, req.IsActive ? "User activated." : "User deactivated.", user.UserId);
    }

    public async Task<UserActionResponse> SoftDeleteAsync(
        Guid actorId, string actorRole, Guid userId, DeleteManagedUserRequest req, CancellationToken ct)
    {
        if (!UserRoles.IsSuperAdmin(actorRole))
            return new UserActionResponse(false, "Only Super Admin can delete users.");

        if (actorId == userId)
            return new UserActionResponse(false, "You cannot delete your own account.");

        var user = await _repo.GetByIdAsync(userId, track: true, ct);
        if (user is null)
            return new UserActionResponse(false, "User not found.");

        var targetRole = UserRoles.Normalize(user.Role) ?? "";
        if (targetRole is UserRoles.SuperAdmin)
            return new UserActionResponse(false, "Cannot delete Super Admin.");

        if (targetRole is UserRoles.Admin && user.IsActive)
            return new UserActionResponse(false, "Deactivate admin before deleting.");

        var previous = SnapshotUser(user);
        user.IsDeleted = true;
        user.DeletedAt = DateTime.Now;
        user.DeletedByUserId = actorId;
        user.IsActive = false;
        user.UpdatedAt = DateTime.Now;
        await _repo.SaveChangesAsync(ct);

        var remarks = (req.Remarks ?? "").Trim();
        await WriteAuditAsync(user, UserAuditActions.Deleted, actorId, previous, new { IsDeleted = true }, remarks, ct);
        return new UserActionResponse(true, "User deleted.", user.UserId);
    }

    public async Task<UserAuditListResponse> ListAuditAsync(
        Guid actorId, string actorRole, Guid userId, int page, int pageSize, CancellationToken ct)
    {
        var access = await EnsureAuditAccessAsync(actorId, actorRole, userId, ct);
        if (access is not null)
            return access;

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, AdminListQuery.MaxPageSize);
        var (rows, total) = await _repo.ListAuditLogsAsync(userId, page, pageSize, ct);
        return new UserAuditListResponse(true, "OK", rows, total, page, pageSize);
    }

    public async Task<UserStatusHistoryListResponse> ListStatusHistoryAsync(
        Guid actorId, string actorRole, Guid userId, int page, int pageSize, CancellationToken ct)
    {
        var access = await EnsureStatusHistoryAccessAsync(actorId, actorRole, userId, ct);
        if (access is not null)
            return access;

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, AdminListQuery.MaxPageSize);
        var (rows, total) = await _repo.ListStatusHistoryAsync(userId, page, pageSize, ct);
        return new UserStatusHistoryListResponse(true, "OK", rows, total, page, pageSize);
    }

    private async Task<UserAuditListResponse?> EnsureAuditAccessAsync(
        Guid actorId, string actorRole, Guid userId, CancellationToken ct)
    {
        if (!UserRoles.IsAdminRole(actorRole))
            return new UserAuditListResponse(false, "Not authorized.");

        var detail = await _repo.GetDetailAsync(userId, ct);
        if (detail is null)
            return new UserAuditListResponse(false, "User not found.");

        if (!CanManage(actorRole, actorId, detail.Role, detail.CreatedByUserId))
            return new UserAuditListResponse(false, "Not authorized to view audit history.");

        return null;
    }

    private async Task<UserStatusHistoryListResponse?> EnsureStatusHistoryAccessAsync(
        Guid actorId, string actorRole, Guid userId, CancellationToken ct)
    {
        if (!UserRoles.IsAdminRole(actorRole))
            return new UserStatusHistoryListResponse(false, "Not authorized.");

        var detail = await _repo.GetDetailAsync(userId, ct);
        if (detail is null)
            return new UserStatusHistoryListResponse(false, "User not found.");

        if (!CanManage(actorRole, actorId, detail.Role, detail.CreatedByUserId))
            return new UserStatusHistoryListResponse(false, "Not authorized to view status history.");

        return null;
    }

    private async Task<UserActionResponse> CreateUserInternalAsync(
        Guid actorId, string role, CreateManagedUserRequest req, CancellationToken ct)
    {
        var validation = ValidateProfile(req.FullName, req.Email, req.Phone, req.Password, requirePassword: true);
        if (validation is not null)
            return new UserActionResponse(false, validation);

        var email = req.Email.Trim().ToLowerInvariant();
        var phone = IndianPhone.Digits(req.Phone);
        if (await _repo.EmailExistsAsync(email, null, ct))
            return new UserActionResponse(false, "Email already exists.");
        if (await _repo.PhoneExistsAsync(phone, null, ct))
            return new UserActionResponse(false, "Phone already exists.");

        var (hash, salt) = PasswordHasher.Hash(req.Password.Trim());
        var now = DateTime.Now;
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Role = role,
            FullName = req.FullName.Trim(),
            Email = email,
            Phone = phone,
            CompanyName = string.IsNullOrWhiteSpace(req.CompanyName) ? null : req.CompanyName.Trim(),
            PasswordHash = hash,
            PasswordSalt = salt,
            EmailVerifiedAt = now,
            PhoneVerifiedAt = now,
            ConsentAccepted = true,
            ConsentAcceptedAt = now,
            IsActive = true,
            IsDeleted = false,
            CreatedByUserId = actorId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        if (UserRoles.IsMemberRole(role))
            user.MemberId = await _memberIds.AllocateNextMemberIdAsync(ct);

        await _repo.AddUserAsync(user, ct);
        await WriteAuditAsync(user, UserAuditActions.Created, actorId, null, SnapshotUser(user), null, ct);
        return new UserActionResponse(true, $"{UserRoles.DisplayName(role)} created.", user.UserId);
    }

    private static bool CanManage(string actorRole, Guid actorId, string targetRole, Guid? createdByUserId)
    {
        targetRole = UserRoles.Normalize(targetRole) ?? "";
        if (UserRoles.IsSuperAdmin(actorRole))
            return targetRole is not UserRoles.SuperAdmin;

        if (targetRole is UserRoles.Admin or UserRoles.SuperAdmin)
            return false;

        return createdByUserId is null || createdByUserId == actorId;
    }

    private static string? ValidateProfile(
        string fullName, string email, string phone, string? password, bool requirePassword)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "Full name is required.";
        if (string.IsNullOrWhiteSpace(email)) return "Email is required.";
        if (string.IsNullOrWhiteSpace(phone)) return "Phone is required.";
        if (IndianPhone.Digits(phone).Length != 10) return "Invalid phone number.";
        if (requirePassword && string.IsNullOrWhiteSpace(password)) return "Password is required.";
        if (requirePassword && password!.Trim().Length < 8) return "Password must be at least 8 characters.";
        return null;
    }

    private static object SnapshotUser(User u) => new
    {
        u.FullName,
        u.Email,
        u.Phone,
        u.CompanyName,
        u.Role,
        IsActive = UserStatusLabels.FromBool(u.IsActive),
        u.MemberId,
    };

    private Task WriteAuditAsync(
        User user,
        string action,
        Guid actorId,
        object? previous,
        object? current,
        string? remarks,
        CancellationToken ct) =>
        _repo.AddAuditLogAsync(new UserAuditLog
        {
            Id = Guid.NewGuid(),
            UserId = user.UserId,
            UserType = user.Role,
            Action = action,
            PerformedByUserId = actorId,
            PerformedOn = DateTime.Now,
            PreviousValues = previous is null ? null : JsonSerializer.Serialize(previous),
            NewValues = current is null ? null : JsonSerializer.Serialize(current),
            Remarks = remarks,
        }, ct);

    private Task WriteStatusHistoryAsync(
        User user,
        string actionType,
        Guid actorId,
        string oldStatus,
        string newStatus,
        string? remarks,
        CancellationToken ct) =>
        _repo.AddStatusHistoryAsync(new UserStatusHistory
        {
            Id = Guid.NewGuid(),
            UserId = user.UserId,
            UserType = user.Role,
            ActionType = actionType,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            Remarks = remarks,
            PerformedByUserId = actorId,
            PerformedOn = DateTime.Now,
        }, ct);
}
