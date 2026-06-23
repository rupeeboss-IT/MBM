using Microsoft.Extensions.Options;
using RB_Website_API.Auth;
using RB_Website_API.DTO;
using RB_Website_API.Models;
using RB_Website_API.Services.IRepository;

namespace RB_Website_API.Services;

public sealed class ConnectService : IConnectService
{
    private readonly IConnectRepository _repo;
    private readonly PaymentActivationService _activation;
    private readonly ConnectSettings _settings;

    public ConnectService(
        IConnectRepository repo,
        PaymentActivationService activation,
        IOptions<ConnectSettings> settings)
    {
        _repo = repo;
        _activation = activation;
        _settings = settings.Value;
    }

    public async Task<ConnectSearchResponse> SearchAsync(
        Guid? viewerUserId,
        int page,
        int pageSize,
        string? search,
        string? sector,
        string? state,
        string? turnover,
        CancellationToken ct)
    {
        if (!_settings.Enabled)
            return new ConnectSearchResponse(false, "Connect is temporarily unavailable.");

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var access = await BuildAccessAsync(viewerUserId, ct);
        var (rows, total) = await _repo.SearchListedAsync(
            page, pageSize, search, sector, state, turnover, viewerUserId, ct);
        var stats = await _repo.GetStatsAsync(ct);

        var profiles = new List<ConnectProfileCardDto>();
        foreach (var row in rows)
        {
            var connection = await ResolveConnectionStateAsync(viewerUserId, row.User.UserId, ct);
            profiles.Add(BuildCard(row, access, connection));
        }

        return new ConnectSearchResponse(true, "OK", profiles, total, page, pageSize, stats, access);
    }

    public async Task<ConnectProfileDetailResponse> GetProfileAsync(
        Guid? viewerUserId, Guid targetUserId, CancellationToken ct)
    {
        if (!_settings.Enabled)
            return new ConnectProfileDetailResponse(false, "Connect is temporarily unavailable.");

        var row = await _repo.GetListedByUserIdAsync(targetUserId, ct);
        if (row is null)
            return new ConnectProfileDetailResponse(false, "Profile not found.");

        var access = await BuildAccessAsync(viewerUserId, ct);
        var connection = await ResolveConnectionStateAsync(viewerUserId, targetUserId, ct);
        var contactVisible = await CanViewContactAsync(viewerUserId, targetUserId, access, connection, ct);
        var profile = BuildDetail(row, access, connection, contactVisible);

        return new ConnectProfileDetailResponse(true, "OK", profile, access);
    }

    public async Task<ConnectAccessSummaryResponse> GetAccessSummaryAsync(Guid userId, CancellationToken ct)
    {
        if (!_settings.Enabled)
            return new ConnectAccessSummaryResponse(false, "Connect is temporarily unavailable.");

        var access = await BuildAccessAsync(userId, ct);
        return new ConnectAccessSummaryResponse(true, "OK", access);
    }

    public async Task<ConnectRequestActionResponse> SendRequestAsync(
        Guid fromUserId, Guid toUserId, string? message, CancellationToken ct)
    {
        if (!_settings.Enabled)
            return new ConnectRequestActionResponse(false, "Connect is temporarily unavailable.");

        if (fromUserId == toUserId)
            return new ConnectRequestActionResponse(false, "You cannot connect with yourself.");

        var access = await BuildAccessAsync(fromUserId, ct);
        if (access.Tier == ConnectAccessTier.None.ToString().ToLowerInvariant())
            return new ConnectRequestActionResponse(false, "An active membership plan is required to connect.");

        var target = await _repo.GetListedByUserIdAsync(toUserId, ct);
        if (target is null)
            return new ConnectRequestActionResponse(false, "Member is not available on Connect.");

        var existing = await _repo.GetRequestBetweenAsync(fromUserId, toUserId, track: false, ct);
        if (existing is not null)
        {
            var state = MapConnectionState(existing, fromUserId);
            if (state == ConnectConnectionStates.Pending)
                return new ConnectRequestActionResponse(false, "Connection request already pending.", state);
            if (state == ConnectConnectionStates.Connected)
                return new ConnectRequestActionResponse(false, "You are already connected.", state);
        }

        var now = DateTime.Now;
        var request = new ConnectRequest
        {
            RequestId = Guid.NewGuid(),
            FromUserId = fromUserId,
            ToUserId = toUserId,
            Status = ConnectRequestStatuses.Pending,
            Message = string.IsNullOrWhiteSpace(message) ? null : message.Trim(),
            CreatedAt = now,
        };

        await _repo.AddRequestAsync(request, ct);
        return new ConnectRequestActionResponse(true, "Connection request sent.", ConnectConnectionStates.Pending);
    }

    public async Task<ConnectRequestActionResponse> AcceptRequestAsync(
        Guid actorUserId, Guid requestId, CancellationToken ct)
    {
        if (!_settings.Enabled)
            return new ConnectRequestActionResponse(false, "Connect is temporarily unavailable.");

        var request = await _repo.GetRequestByIdAsync(requestId, track: true, ct);
        if (request is null)
            return new ConnectRequestActionResponse(false, "Request not found.");

        if (request.ToUserId != actorUserId)
            return new ConnectRequestActionResponse(false, "Not authorized.");

        if (request.Status != ConnectRequestStatuses.Pending)
            return new ConnectRequestActionResponse(false, $"Request is already {request.Status.ToLowerInvariant()}.");

        request.Status = ConnectRequestStatuses.Connected;
        request.RespondedAt = DateTime.Now;

        var fromAccess = await BuildAccessAsync(request.FromUserId, ct);
        if (fromAccess.Tier == ConnectAccessTier.Limited.ToString().ToLowerInvariant())
        {
            var hasUnlock = await _repo.HasUnlockAsync(request.FromUserId, request.ToUserId, ct);
            if (!hasUnlock)
            {
                var used = await _repo.CountUnlocksAsync(request.FromUserId, ct);
                if (used < _settings.BasicStandardContactLimit)
                {
                    await _repo.AddUnlockAsync(new ConnectContactUnlock
                    {
                        ViewerUserId = request.FromUserId,
                        TargetUserId = request.ToUserId,
                        UnlockedAt = DateTime.Now,
                    }, ct);
                }
            }
        }

        await _repo.SaveChangesAsync(ct);
        return new ConnectRequestActionResponse(true, "Connection accepted.", ConnectConnectionStates.Connected);
    }

    public async Task<ConnectRequestActionResponse> RejectRequestAsync(
        Guid actorUserId, Guid requestId, CancellationToken ct)
    {
        if (!_settings.Enabled)
            return new ConnectRequestActionResponse(false, "Connect is temporarily unavailable.");

        var request = await _repo.GetRequestByIdAsync(requestId, track: true, ct);
        if (request is null)
            return new ConnectRequestActionResponse(false, "Request not found.");

        if (request.ToUserId != actorUserId)
            return new ConnectRequestActionResponse(false, "Not authorized.");

        if (request.Status != ConnectRequestStatuses.Pending)
            return new ConnectRequestActionResponse(false, $"Request is already {request.Status.ToLowerInvariant()}.");

        request.Status = ConnectRequestStatuses.Rejected;
        request.RespondedAt = DateTime.Now;
        await _repo.SaveChangesAsync(ct);

        return new ConnectRequestActionResponse(true, "Connection request rejected.", ConnectConnectionStates.Rejected);
    }

    public async Task<ConnectIncomingListResponse> ListIncomingAsync(Guid userId, CancellationToken ct)
    {
        if (!_settings.Enabled)
            return new ConnectIncomingListResponse(false, "Connect is temporarily unavailable.");

        var items = await _repo.ListIncomingPendingAsync(userId, ct);
        return new ConnectIncomingListResponse(true, "OK", items);
    }

    public async Task<ConnectMemberProfileResponse> GetMyProfileAsync(Guid userId, CancellationToken ct)
    {
        if (!_settings.Enabled)
            return new ConnectMemberProfileResponse(false, "Connect is temporarily unavailable.");

        var row = await _repo.GetListedByUserIdAsync(userId, ct);
        if (row is null)
            return new ConnectMemberProfileResponse(false, "Your profile is not listed on Connect yet.");

        var access = await BuildAccessAsync(userId, ct);
        var profile = BuildDetail(row, access, ConnectConnectionStates.None, contactVisible: true);
        return new ConnectMemberProfileResponse(true, "OK", profile);
    }

    public async Task<ConnectMemberProfileResponse> UpdateMyProfileAsync(
        Guid userId, UpdateConnectMemberProfileRequest req, CancellationToken ct)
    {
        if (!_settings.Enabled)
            return new ConnectMemberProfileResponse(false, "Connect is temporarily unavailable.");

        var listing = await _repo.GetAdminListingByUserIdAsync(userId, track: false, ct);
        if (listing is null || !listing.IsListed)
            return new ConnectMemberProfileResponse(false, "Your profile is not listed on Connect yet.");

        var now = DateTime.Now;
        var profile = await _repo.GetMemberProfileAsync(userId, track: true, ct);
        if (profile is null)
        {
            profile = new ConnectMemberProfile { UserId = userId, CreatedAt = now, UpdatedAt = now };
            ApplyMemberProfile(profile, req);
            await _repo.AddMemberProfileAsync(profile, ct);
        }
        else
        {
            ApplyMemberProfile(profile, req);
            profile.UpdatedAt = now;
            await _repo.SaveChangesAsync(ct);
        }

        return await GetMyProfileAsync(userId, ct);
    }

    public async Task<ConnectAdminListingListResponse> AdminListAsync(
        string actorRole, int page, int pageSize, string? search, string? role, string? status, CancellationToken ct)
    {
        if (!UserRoles.IsAdminRole(actorRole))
            return new ConnectAdminListingListResponse(false, "Not authorized.");

        (page, pageSize) = AdminListQuery.Normalize(page, pageSize, export: false);
        search = AdminListQuery.NormalizeSearch(search);
        var (rows, total) = await _repo.ListAdminListingsAsync(page, pageSize, search, role, status, ct);
        return new ConnectAdminListingListResponse(true, "OK", rows, total, page, pageSize);
    }

    public async Task<ConnectAdminListingDetailResponse> AdminGetAsync(
        string actorRole, Guid listingId, CancellationToken ct)
    {
        if (!UserRoles.IsAdminRole(actorRole))
            return new ConnectAdminListingDetailResponse(false, "Not authorized.");

        var listing = await _repo.GetAdminListingByIdAsync(listingId, track: false, ct);
        if (listing is null)
            return new ConnectAdminListingDetailResponse(false, "Listing not found.");

        return new ConnectAdminListingDetailResponse(true, "OK", await BuildAdminDetailAsync(listing, ct));
    }

    public async Task<ConnectAdminActionResponse> AdminCreateAsync(
        Guid actorId, string actorRole, CreateConnectAdminListingRequest req, CancellationToken ct)
    {
        if (!UserRoles.IsAdminRole(actorRole))
            return new ConnectAdminActionResponse(false, "Not authorized.");

        if (req.UserId == Guid.Empty)
            return new ConnectAdminActionResponse(false, "User is required.");

        var user = await _repo.GetMemberUserAsync(req.UserId, ct);
        if (user is null)
            return new ConnectAdminActionResponse(false, "User not found or is not a member/partner.");

        var existingListing = await _repo.GetAdminListingByUserIdAsync(req.UserId, track: false, ct);
        if (existingListing is not null)
            return new ConnectAdminActionResponse(false, "Connect listing already exists for this user.");

        var now = DateTime.Now;
        var listing = new ConnectAdminListing
        {
            ListingId = Guid.NewGuid(),
            UserId = req.UserId,
            IsListed = req.IsListed,
            IsVerified = req.IsVerified,
            IsActive = req.IsActive,
            BusinessType = Normalize(req.BusinessType),
            Sector = Normalize(req.Sector),
            State = Normalize(req.State),
            City = Normalize(req.City),
            Turnover = Normalize(req.Turnover),
            Udyam = Normalize(req.Udyam),
            Employees = Normalize(req.Employees),
            Description = Normalize(req.Description),
            Website = Normalize(req.Website),
            Established = Normalize(req.Established),
            SocialLinksJson = Normalize(req.SocialLinksJson),
            Remarks = Normalize(req.Remarks),
            CreatedByUserId = actorId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await ApplyAdminFieldsRespectingMemberLocks(listing, req.UserId, isCreate: true, ct);
        await _repo.AddAdminListingAsync(listing, ct);
        return new ConnectAdminActionResponse(true, "Connect listing created.", listing.ListingId);
    }

    public async Task<ConnectAdminActionResponse> AdminUpdateAsync(
        Guid actorId, string actorRole, Guid listingId, UpdateConnectAdminListingRequest req, CancellationToken ct)
    {
        if (!UserRoles.IsAdminRole(actorRole))
            return new ConnectAdminActionResponse(false, "Not authorized.");

        var listing = await _repo.GetAdminListingByIdAsync(listingId, track: true, ct);
        if (listing is null)
            return new ConnectAdminActionResponse(false, "Listing not found.");

        listing.IsListed = req.IsListed;
        listing.IsVerified = req.IsVerified;
        listing.IsActive = req.IsActive;
        listing.Remarks = Normalize(req.Remarks);
        listing.UpdatedAt = DateTime.Now;

        var member = await _repo.GetMemberProfileAsync(listing.UserId, track: false, ct);
        listing.BusinessType = SetAdminField(member, nameof(ConnectMemberProfile.BusinessType), req.BusinessType, listing.BusinessType);
        listing.Sector = SetAdminField(member, nameof(ConnectMemberProfile.Sector), req.Sector, listing.Sector);
        listing.State = SetAdminField(member, nameof(ConnectMemberProfile.State), req.State, listing.State);
        listing.City = SetAdminField(member, nameof(ConnectMemberProfile.City), req.City, listing.City);
        listing.Turnover = SetAdminField(member, nameof(ConnectMemberProfile.Turnover), req.Turnover, listing.Turnover);
        listing.Udyam = SetAdminField(member, nameof(ConnectMemberProfile.Udyam), req.Udyam, listing.Udyam);
        listing.Employees = SetAdminField(member, nameof(ConnectMemberProfile.Employees), req.Employees, listing.Employees);
        listing.Description = SetAdminField(member, nameof(ConnectMemberProfile.Description), req.Description, listing.Description);
        listing.Website = SetAdminField(member, nameof(ConnectMemberProfile.Website), req.Website, listing.Website);
        listing.Established = SetAdminField(member, nameof(ConnectMemberProfile.Established), req.Established, listing.Established);
        listing.SocialLinksJson = SetAdminField(member, nameof(ConnectMemberProfile.SocialLinksJson), req.SocialLinksJson, listing.SocialLinksJson);

        await _repo.SaveChangesAsync(ct);
        return new ConnectAdminActionResponse(true, "Connect listing updated.", listing.ListingId);
    }

    public async Task<ConnectUserSearchResponse> AdminSearchUsersAsync(
        string actorRole, string? search, string? role, int limit, CancellationToken ct)
    {
        if (!UserRoles.IsAdminRole(actorRole))
            return new ConnectUserSearchResponse(false, "Not authorized.");

        limit = Math.Clamp(limit, 1, 50);
        var (rows, _) = await _repo.SearchMembersForAdminAsync(search, role, limit, ct);
        return new ConnectUserSearchResponse(true, "OK", rows);
    }

    private async Task ApplyAdminFieldsRespectingMemberLocks(
        ConnectAdminListing listing, Guid userId, bool isCreate, CancellationToken ct)
    {
        var member = await _repo.GetMemberProfileAsync(userId, track: false, ct);
        if (member is null) return;

        listing.BusinessType = SetAdminField(member, nameof(ConnectMemberProfile.BusinessType), listing.BusinessType, null);
        listing.Sector = SetAdminField(member, nameof(ConnectMemberProfile.Sector), listing.Sector, null);
        listing.State = SetAdminField(member, nameof(ConnectMemberProfile.State), listing.State, null);
        listing.City = SetAdminField(member, nameof(ConnectMemberProfile.City), listing.City, null);
        listing.Turnover = SetAdminField(member, nameof(ConnectMemberProfile.Turnover), listing.Turnover, null);
        listing.Udyam = SetAdminField(member, nameof(ConnectMemberProfile.Udyam), listing.Udyam, null);
        listing.Employees = SetAdminField(member, nameof(ConnectMemberProfile.Employees), listing.Employees, null);
        listing.Description = SetAdminField(member, nameof(ConnectMemberProfile.Description), listing.Description, null);
        listing.Website = SetAdminField(member, nameof(ConnectMemberProfile.Website), listing.Website, null);
        listing.Established = SetAdminField(member, nameof(ConnectMemberProfile.Established), listing.Established, null);
        listing.SocialLinksJson = SetAdminField(member, nameof(ConnectMemberProfile.SocialLinksJson), listing.SocialLinksJson, null);
    }

    private static string? SetAdminField(
        ConnectMemberProfile? member, string fieldName, string? newValue, string? currentAdminValue)
    {
        if (ConnectProfileMerger.HasCustomerValue(member, fieldName))
            return currentAdminValue;
        return Normalize(newValue);
    }

    private async Task<ConnectAdminListingDetailDto> BuildAdminDetailAsync(
        ConnectAdminListing listing, CancellationToken ct)
    {
        var user = await _repo.GetMemberUserAsync(listing.UserId, ct);
        if (user is null)
            throw new InvalidOperationException("User not found for listing.");

        var member = await _repo.GetMemberProfileAsync(listing.UserId, track: false, ct);
        var locked = ConnectProfileMerger.GetCustomerLockedFields(member);

        ConnectMemberProfileReadOnlyDto? memberDto = member is null ? null : new(
            member.Designation,
            member.BusinessType,
            member.Sector,
            member.State,
            member.City,
            member.Turnover,
            member.Udyam,
            member.Employees,
            member.Description,
            member.Website,
            member.Established,
            member.SocialLinksJson,
            member.UpdatedAt);

        var u = user;

        return new ConnectAdminListingDetailDto(
            listing.ListingId,
            listing.UserId,
            u.Role ?? "",
            u.FullName,
            u.CompanyName,
            u.Phone,
            u.Email,
            listing.IsListed,
            listing.IsVerified,
            listing.IsActive,
            listing.BusinessType,
            listing.Sector,
            listing.State,
            listing.City,
            listing.Turnover,
            listing.Udyam,
            listing.Employees,
            listing.Description,
            listing.Website,
            listing.Established,
            listing.SocialLinksJson,
            listing.Remarks,
            memberDto,
            locked,
            listing.CreatedAt,
            listing.UpdatedAt);
    }

    private static void ApplyMemberProfile(ConnectMemberProfile profile, UpdateConnectMemberProfileRequest req)
    {
        profile.Designation = Normalize(req.Designation);
        profile.BusinessType = Normalize(req.BusinessType);
        profile.Sector = Normalize(req.Sector);
        profile.State = Normalize(req.State);
        profile.City = Normalize(req.City);
        profile.Turnover = Normalize(req.Turnover);
        profile.Udyam = Normalize(req.Udyam);
        profile.Employees = Normalize(req.Employees);
        profile.Description = Normalize(req.Description);
        profile.Website = Normalize(req.Website);
        profile.Established = Normalize(req.Established);
        profile.SocialLinksJson = Normalize(req.SocialLinksJson);
    }

    private async Task<ConnectAccessSummaryDto> BuildAccessAsync(Guid? userId, CancellationToken ct)
    {
        if (!userId.HasValue || userId.Value == Guid.Empty)
        {
            return new ConnectAccessSummaryDto(
                ConnectAccessTier.None.ToString().ToLowerInvariant(),
                0, null, false, null, null);
        }

        var plan = await _activation.GetActivePlanRowAsync(userId.Value, ct);
        var tier = ConnectAccessRules.GetTier(plan?.PlanCode);
        var tierStr = tier.ToString().ToLowerInvariant();

        if (tier == ConnectAccessTier.Limited)
        {
            var used = await _repo.CountUnlocksAsync(userId.Value, ct);
            var limit = _settings.BasicStandardContactLimit;
            return new ConnectAccessSummaryDto(
                tierStr, used, limit, used < limit, plan?.PlanCode, plan?.PlanName);
        }

        if (tier == ConnectAccessTier.Unlimited)
        {
            return new ConnectAccessSummaryDto(
                tierStr, 0, null, true, plan?.PlanCode, plan?.PlanName);
        }

        return new ConnectAccessSummaryDto(tierStr, 0, null, false, plan?.PlanCode, plan?.PlanName);
    }

    private async Task<bool> CanViewContactAsync(
        Guid? viewerUserId,
        Guid targetUserId,
        ConnectAccessSummaryDto access,
        string connection,
        CancellationToken ct)
    {
        if (!viewerUserId.HasValue || viewerUserId.Value == Guid.Empty) return false;
        if (viewerUserId.Value == targetUserId) return true;
        if (access.Tier == ConnectAccessTier.None.ToString().ToLowerInvariant()) return false;
        if (connection != ConnectConnectionStates.Connected) return false;

        if (access.Tier == ConnectAccessTier.Unlimited.ToString().ToLowerInvariant())
            return true;

        return await _repo.HasUnlockAsync(viewerUserId.Value, targetUserId, ct);
    }

    private ConnectProfileCardDto BuildCard(
        ConnectListingRow row,
        ConnectAccessSummaryDto access,
        string connection)
    {
        var locked = access.Tier == ConnectAccessTier.None.ToString().ToLowerInvariant();
        if (locked)
        {
            return new ConnectProfileCardDto(
                row.User.UserId,
                true,
                null, null, null, null, null, null, null, null, null,
                false,
                connection,
                CanConnect: false,
                ContactVisible: false);
        }

        var member = row.MemberProfile;
        var listing = row.Listing;
        var name = ConnectProfileMerger.DisplayOrDash(row.User.CompanyName ?? row.User.FullName);
        var initials = ConnectProfileMerger.BuildInitials(row.User.FullName, row.User.CompanyName);

        return new ConnectProfileCardDto(
            row.User.UserId,
            false,
            name,
            ConnectProfileMerger.DisplayOrDash(row.User.CompanyName),
            initials,
            ConnectProfileMerger.MergeFieldDash(member?.BusinessType, listing.BusinessType),
            ConnectProfileMerger.MergeFieldDash(member?.Sector, listing.Sector),
            ConnectProfileMerger.MergeFieldDash(member?.State, listing.State),
            ConnectProfileMerger.MergeFieldDash(member?.City, listing.City),
            ConnectProfileMerger.MergeFieldDash(member?.Turnover, listing.Turnover),
            ConnectProfileMerger.MergeFieldDash(member?.Description, listing.Description),
            listing.IsVerified,
            connection,
            CanConnect: connection is ConnectConnectionStates.None or ConnectConnectionStates.Rejected,
            ContactVisible: false);
    }

    private ConnectProfileDetailDto BuildDetail(
        ConnectListingRow row,
        ConnectAccessSummaryDto access,
        string connection,
        bool contactVisible)
    {
        var locked = access.Tier == ConnectAccessTier.None.ToString().ToLowerInvariant();
        if (locked)
        {
            return new ConnectProfileDetailDto(
                row.User.UserId, true,
                null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
                false, connection, false, false, null);
        }

        var member = row.MemberProfile;
        var listing = row.Listing;
        var name = ConnectProfileMerger.DisplayOrDash(row.User.CompanyName ?? row.User.FullName);

        return new ConnectProfileDetailDto(
            row.User.UserId,
            false,
            name,
            ConnectProfileMerger.DisplayOrDash(row.User.CompanyName),
            ConnectProfileMerger.MergeFieldDash(member?.Designation, null),
            ConnectProfileMerger.BuildInitials(row.User.FullName, row.User.CompanyName),
            ConnectProfileMerger.MergeFieldDash(member?.BusinessType, listing.BusinessType),
            ConnectProfileMerger.MergeFieldDash(member?.Sector, listing.Sector),
            ConnectProfileMerger.MergeFieldDash(member?.State, listing.State),
            ConnectProfileMerger.MergeFieldDash(member?.City, listing.City),
            ConnectProfileMerger.MergeFieldDash(member?.Turnover, listing.Turnover),
            ConnectProfileMerger.MergeFieldDash(member?.Udyam, listing.Udyam),
            ConnectProfileMerger.MergeFieldDash(member?.Employees, listing.Employees),
            ConnectProfileMerger.MergeFieldDash(member?.Description, listing.Description),
            contactVisible ? ConnectProfileMerger.MergeFieldDash(member?.Website, listing.Website) : "-",
            ConnectProfileMerger.MergeFieldDash(member?.Established, listing.Established),
            contactVisible ? ConnectProfileMerger.DisplayOrDash(member?.SocialLinksJson) : "-",
            contactVisible ? ConnectProfileMerger.DisplayOrDash(row.User.Phone) : "-",
            contactVisible ? ConnectProfileMerger.DisplayOrDash(row.User.Email) : "-",
            listing.IsVerified,
            connection,
            CanConnect: connection is ConnectConnectionStates.None or ConnectConnectionStates.Rejected,
            contactVisible,
            null);
    }

    private async Task<string> ResolveConnectionStateAsync(
        Guid? viewerUserId, Guid targetUserId, CancellationToken ct)
    {
        if (!viewerUserId.HasValue || viewerUserId.Value == Guid.Empty)
            return ConnectConnectionStates.None;

        if (viewerUserId.Value == targetUserId)
            return ConnectConnectionStates.Connected;

        var request = await _repo.GetRequestBetweenAsync(viewerUserId.Value, targetUserId, track: false, ct);
        if (request is null) return ConnectConnectionStates.None;
        return MapConnectionState(request, viewerUserId.Value);
    }

    private static string MapConnectionState(ConnectRequest request, Guid viewerUserId)
    {
        if (request.Status == ConnectRequestStatuses.Connected)
            return ConnectConnectionStates.Connected;
        if (request.Status == ConnectRequestStatuses.Rejected)
            return ConnectConnectionStates.Rejected;
        if (request.Status == ConnectRequestStatuses.Pending)
        {
            if (request.FromUserId == viewerUserId)
                return ConnectConnectionStates.Pending;
            return ConnectConnectionStates.Pending;
        }
        return ConnectConnectionStates.None;
    }

    private static string? Normalize(string? v) =>
        string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}
