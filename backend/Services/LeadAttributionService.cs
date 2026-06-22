using RB_Website_API.Auth;
using RB_Website_API.DTO;
using RB_Website_API.Models;
using RB_Website_API.Referrals.Models;
using RB_Website_API.Referrals.Services;
using RB_Website_API.Services.IRepository;

namespace RB_Website_API.Services;

public sealed class LeadAttributionService : ILeadAttributionService
{
    private readonly ILeadAttributionRepository _repo;
    private readonly IEmployeeValidationService _referrals;

    public LeadAttributionService(ILeadAttributionRepository repo, IEmployeeValidationService referrals)
    {
        _repo = repo;
        _referrals = referrals;
    }

    public async Task<LeadAttributionStatsResponse> GetDashboardAsync(
        Guid actorId, string actorRole, string? dateFrom, string? dateTo, CancellationToken ct)
    {
        if (!UserRoles.IsAdminRole(actorRole))
            return new LeadAttributionStatsResponse(false, "Not authorized.");

        var rows = await BuildCustomerRowsAsync(actorId, actorRole, dateFrom, dateTo, ct);
        var revenueByPerformer = await BuildPerformerRevenueAsync(actorId, actorRole, dateFrom, dateTo, ct);
        var stats = BuildStats(rows);
        var breakdown = BuildSourceBreakdown(rows);
        var topEmployees = BuildTopPerformers(rows, LeadSourceTypes.Employee, revenueByPerformer, 10);
        var topPartners = BuildTopRbaPartners(rows, revenueByPerformer, 10);

        return new LeadAttributionStatsResponse(true, "OK", stats, breakdown, topEmployees, topPartners);
    }

    public async Task<LeadPerformerDetailResponse> GetPerformerDetailsAsync(
        Guid actorId,
        string actorRole,
        string performerType,
        string code,
        string? dateFrom,
        string? dateTo,
        CancellationToken ct)
    {
        if (!UserRoles.IsAdminRole(actorRole))
            return new LeadPerformerDetailResponse(false, "Not authorized.");

        var kind = NormalizePerformerKind(performerType);
        if (kind is null || string.IsNullOrWhiteSpace(code))
            return new LeadPerformerDetailResponse(false, "Invalid performer.");

        var scopedUserIds = await GetScopedMemberIdsAsync(actorId, actorRole, ct);
        if (scopedUserIds.Count == 0)
            return new LeadPerformerDetailResponse(true, "OK", code.Trim(), code.Trim(), 0, 0, Array.Empty<LeadPerformerPaymentDto>());

        var paidFrom = AdminListQuery.ParseDateFrom(dateFrom);
        var paidToEx = AdminListQuery.ParseDateToExclusive(dateTo);
        var payments = await _repo.GetReferralPaymentsForUsersAsync(scopedUserIds, paidFrom, paidToEx, ct);
        var cache = new Dictionary<string, PerformerKey?>(StringComparer.OrdinalIgnoreCase);
        var targetCode = code.Trim();
        var matched = new List<LeadPerformerPaymentDto>();
        string? performerName = null;

        foreach (var payment in payments)
        {
            var performer = await ResolvePerformerKeyAsync(payment.ReferralCode, cache, ct);
            if (performer is null || !string.Equals(performer.Kind, kind, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!string.Equals(performer.Code, targetCode, StringComparison.OrdinalIgnoreCase))
                continue;

            performerName ??= performer.Name;
            matched.Add(ToPerformerPayment(payment));
        }

        return new LeadPerformerDetailResponse(
            true,
            "OK",
            performerName ?? targetCode,
            targetCode,
            matched.Sum(p => p.TotalAmountPaise),
            matched.Count,
            matched);
    }

    public async Task<LeadCustomerListResponse> ListCustomersAsync(
        Guid actorId,
        string actorRole,
        int page,
        int pageSize,
        string? search,
        string? sourceType,
        string? sourceName,
        string? employeeName,
        string? partnerName,
        string? planCode,
        string? dateFrom,
        string? dateTo,
        string? sortBy,
        string? sortDir,
        bool export,
        CancellationToken ct)
    {
        if (!UserRoles.IsAdminRole(actorRole))
            return new LeadCustomerListResponse(false, "Not authorized.");

        (page, pageSize) = AdminListQuery.Normalize(page, pageSize, export);
        search = AdminListQuery.NormalizeSearch(search);
        var from = AdminListQuery.ParseDateFrom(dateFrom);
        var toEx = AdminListQuery.ParseDateToExclusive(dateTo);

        var rows = await BuildCustomerRowsAsync(actorId, actorRole, dateFrom, dateTo, ct);
        rows = ApplyFilters(rows, search, sourceType, sourceName, employeeName, partnerName, planCode, from, toEx);
        rows = ApplySort(rows, sortBy, sortDir);

        var total = rows.Count;
        var pageRows = export
            ? rows
            : rows.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var customers = pageRows.Select(ToListItem).ToList();
        return new LeadCustomerListResponse(true, "OK", customers, total, page, pageSize);
    }

    public async Task<LeadCustomerDetailResponse> GetCustomerAsync(
        Guid actorId, string actorRole, Guid userId, CancellationToken ct)
    {
        if (!UserRoles.IsAdminRole(actorRole))
            return new LeadCustomerDetailResponse(false, "Not authorized.");

        var member = await _repo.GetMemberAsync(userId, ct);
        if (member is null)
            return new LeadCustomerDetailResponse(false, "Customer not found.");

        if (!await IsInScopeAsync(actorId, actorRole, member, ct))
            return new LeadCustomerDetailResponse(false, "Not authorized to view this customer.");

        var row = await BuildCustomerRowAsync(member, ct);
        var paymentHistory = await _repo.GetPaymentHistoryAsync(userId, ct);
        return new LeadCustomerDetailResponse(true, "OK", ToDetail(row, paymentHistory));
    }

    public async Task<LeadFilterOptionsResponse> GetFilterOptionsAsync(
        Guid actorId, string actorRole, CancellationToken ct)
    {
        if (!UserRoles.IsAdminRole(actorRole))
            return new LeadFilterOptionsResponse(false, "Not authorized.");

        var rows = await BuildCustomerRowsAsync(actorId, actorRole, null, null, ct);
        return new LeadFilterOptionsResponse(
            true,
            "OK",
            rows.Select(r => r.SourceType).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList(),
            rows.Select(r => r.SourceName).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).Cast<string>().ToList(),
            rows.Where(r => r.SourceType == LeadSourceTypes.Employee)
                .Select(r => r.SourceName).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).Cast<string>().ToList(),
            rows.Where(r => r.SourceType == LeadSourceTypes.Partner)
                .Select(r => r.SourceName).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).Cast<string>().ToList(),
            rows.Select(r => r.PlanCode).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).Cast<string>().ToList());
    }

    private async Task<List<LeadCustomerRow>> BuildCustomerRowsAsync(
        Guid actorId, string actorRole, string? dateFrom, string? dateTo, CancellationToken ct)
    {
        var isSuperAdmin = UserRoles.IsSuperAdmin(actorRole);
        var partnerIds = isSuperAdmin
            ? Array.Empty<Guid>()
            : await _repo.GetScopedPartnerIdsAsync(actorId, ct);

        var members = await _repo.ListScopedMembersAsync(isSuperAdmin, actorId, partnerIds, ct);
        var from = AdminListQuery.ParseDateFrom(dateFrom);
        var toEx = AdminListQuery.ParseDateToExclusive(dateTo);

        if (from.HasValue)
            members = members.Where(m => m.CreatedAt >= from.Value).ToList();
        if (toEx.HasValue)
            members = members.Where(m => m.CreatedAt < toEx.Value).ToList();

        if (members.Count == 0) return new List<LeadCustomerRow>();

        var userIds = members.Select(m => m.UserId).ToList();
        var now = DateTime.Now;
        var reportCounts = await _repo.GetReportCountsAsync(userIds, ct);
        var reportPurchaseCounts = await _repo.GetReportPurchaseCountsAsync(userIds, ct);
        var activePlans = await _repo.GetActivePlansAsync(userIds, now, ct);
        var membershipCounts = await _repo.GetMembershipSalesCountsAsync(userIds, ct);
        var firstReferrals = await _repo.GetFirstPaidReferralsAsync(userIds, ct);
        var registrationAdvisors = await _repo.GetRegistrationAdvisorsAsync(userIds, ct);

        var creatorIds = members
            .Where(m => m.CreatedByUserId.HasValue)
            .Select(m => m.CreatedByUserId!.Value)
            .Distinct()
            .ToList();
        var creators = await _repo.GetUsersByIdsAsync(creatorIds, ct);

        var referralCache = new Dictionary<string, ResolvedReferral?>(StringComparer.OrdinalIgnoreCase);
        var rows = new List<LeadCustomerRow>(members.Count);

        foreach (var member in members)
        {
            rows.Add(await BuildRowAsync(
                member,
                creators,
                firstReferrals,
                registrationAdvisors,
                reportCounts,
                reportPurchaseCounts,
                activePlans,
                membershipCounts,
                referralCache,
                ct));
        }

        return rows;
    }

    private async Task<LeadCustomerRow> BuildCustomerRowAsync(User member, CancellationToken ct)
    {
        var userIds = new[] { member.UserId };
        var now = DateTime.Now;
        var reportCounts = await _repo.GetReportCountsAsync(userIds, ct);
        var reportPurchaseCounts = await _repo.GetReportPurchaseCountsAsync(userIds, ct);
        var activePlans = await _repo.GetActivePlansAsync(userIds, now, ct);
        var membershipCounts = await _repo.GetMembershipSalesCountsAsync(userIds, ct);
        var firstReferrals = await _repo.GetFirstPaidReferralsAsync(userIds, ct);
        var registrationAdvisors = await _repo.GetRegistrationAdvisorsAsync(userIds, ct);

        var creatorIds = member.CreatedByUserId.HasValue
            ? new List<Guid> { member.CreatedByUserId.Value }
            : new List<Guid>();
        var creators = await _repo.GetUsersByIdsAsync(creatorIds, ct);
        var referralCache = new Dictionary<string, ResolvedReferral?>(StringComparer.OrdinalIgnoreCase);

        return await BuildRowAsync(
            member,
            creators,
            firstReferrals,
            registrationAdvisors,
            reportCounts,
            reportPurchaseCounts,
            activePlans,
            membershipCounts,
            referralCache,
            ct);
    }

    private async Task<LeadCustomerRow> BuildRowAsync(
        User member,
        Dictionary<Guid, User> creators,
        Dictionary<Guid, FirstReferralRow> firstReferrals,
        Dictionary<Guid, RegistrationAdvisorRow> registrationAdvisors,
        Dictionary<Guid, int> reportCounts,
        Dictionary<Guid, int> reportPurchaseCounts,
        Dictionary<Guid, (string? PlanCode, string? PlanName, string Status)> activePlans,
        Dictionary<Guid, int> membershipCounts,
        Dictionary<string, ResolvedReferral?> referralCache,
        CancellationToken ct)
    {
        activePlans.TryGetValue(member.UserId, out var plan);
        reportCounts.TryGetValue(member.UserId, out var reportGenerated);
        reportPurchaseCounts.TryGetValue(member.UserId, out var reportPurchases);
        membershipCounts.TryGetValue(member.UserId, out var membershipSales);
        firstReferrals.TryGetValue(member.UserId, out var firstReferral);
        registrationAdvisors.TryGetValue(member.UserId, out var registrationAdvisor);

        var createdThrough = ResolveCreatedThrough(member, creators);
        var attribution = await ResolveAttributionAsync(member, creators, registrationAdvisor, firstReferral, referralCache, ct);

        return new LeadCustomerRow(
            member,
            MemberIdHelper.GetDisplayMemberId(member),
            attribution.IsRba,
            attribution.SourceType,
            attribution.SourceName,
            attribution.SourceCode,
            attribution.AssignedAdvisor,
            attribution.AssignedEmployee,
            createdThrough,
            plan.PlanCode,
            plan.PlanName,
            string.IsNullOrWhiteSpace(plan.PlanName) ? "None" : plan.Status,
            reportPurchases,
            reportGenerated,
            membershipSales,
            firstReferral?.PaidAt,
            firstReferral?.PlanCode,
            firstReferral?.PlanName,
            firstReferral?.ReferralCode,
            registrationAdvisor?.AdvisorCode);
    }

    private async Task<AttributionInfo> ResolveAttributionAsync(
        User member,
        Dictionary<Guid, User> creators,
        RegistrationAdvisorRow? registrationAdvisor,
        FirstReferralRow? firstReferral,
        Dictionary<string, ResolvedReferral?> referralCache,
        CancellationToken ct)
    {
        if (member.CreatedByUserId is Guid creatorId
            && creators.TryGetValue(creatorId, out var creator)
            && UserRoles.Normalize(creator.Role) == UserRoles.Partner)
        {
            return new AttributionInfo(
                IsRba: false,
                LeadSourceTypes.Partner,
                creator.FullName,
                creator.MemberId ?? creator.Phone,
                creator.FullName,
                null);
        }

        if (registrationAdvisor is not null
            && !registrationAdvisor.UsedDefaultEmployee
            && !string.IsNullOrWhiteSpace(registrationAdvisor.AdvisorCode))
        {
            var fromRegistration = await ResolveAttributionFromReferralCodeAsync(
                registrationAdvisor.AdvisorCode,
                referralCache,
                ct);
            if (fromRegistration is not null)
                return fromRegistration;
        }

        if (firstReferral is not null)
        {
            var fromPayment = await ResolveAttributionFromReferralCodeAsync(
                firstReferral.ReferralCode,
                referralCache,
                ct,
                treatEmptyAsDirect: true);
            if (fromPayment is not null)
                return fromPayment;
        }

        if (member.CreatedByUserId is null)
        {
            return new AttributionInfo(
                IsRba: false,
                LeadSourceTypes.Direct,
                "Direct Website",
                null,
                null,
                null);
        }

        return new AttributionInfo(
            IsRba: false,
            LeadSourceTypes.Unknown,
            "Unknown",
            null,
            null,
            null);
    }

    private async Task<AttributionInfo?> ResolveAttributionFromReferralCodeAsync(
        string? referralCode,
        Dictionary<string, ResolvedReferral?> referralCache,
        CancellationToken ct,
        bool treatEmptyAsDirect = false)
    {
        if (string.IsNullOrWhiteSpace(referralCode))
        {
            if (!treatEmptyAsDirect) return null;
            return new AttributionInfo(
                IsRba: false,
                LeadSourceTypes.Direct,
                "Direct Website",
                null,
                null,
                null);
        }

        var resolved = await ResolveReferralCachedAsync(referralCode, referralCache, ct);
        if (resolved is null) return null;

        if (resolved.ReferralType == ReferralType.RBA)
        {
            return new AttributionInfo(
                IsRba: true,
                LeadSourceTypes.Partner,
                resolved.DisplayName,
                resolved.ReferralCode,
                resolved.DisplayName,
                null);
        }

        if (resolved.UsedDefaultEmployee && string.IsNullOrWhiteSpace(referralCode))
        {
            return new AttributionInfo(
                IsRba: false,
                LeadSourceTypes.Direct,
                "Direct Website",
                null,
                null,
                null);
        }

        return new AttributionInfo(
            IsRba: false,
            LeadSourceTypes.Employee,
            resolved.DisplayName,
            resolved.EmpCode,
            resolved.DisplayName,
            resolved.DisplayName);
    }

    private async Task<ResolvedReferral?> ResolveReferralCachedAsync(
        string? referralCode,
        Dictionary<string, ResolvedReferral?> cache,
        CancellationToken ct)
    {
        var key = (referralCode ?? "").Trim();
        if (cache.TryGetValue(key, out var cached))
            return cached;

        var resolved = await _referrals.ResolveReferralForLeadAsync(
            string.IsNullOrWhiteSpace(key) ? null : key,
            ct);
        cache[key] = resolved;
        return resolved;
    }

    private static string ResolveCreatedThrough(User member, Dictionary<Guid, User> creators)
    {
        if (member.CreatedByUserId is null)
            return "Self Registration";

        if (creators.TryGetValue(member.CreatedByUserId.Value, out var creator))
        {
            return UserRoles.Normalize(creator.Role) switch
            {
                UserRoles.Partner => "Partner Onboarding",
                UserRoles.Admin or UserRoles.SuperAdmin => "Admin Created",
                _ => "Admin Created",
            };
        }

        return "Admin Created";
    }

    private async Task<bool> IsInScopeAsync(Guid actorId, string actorRole, User member, CancellationToken ct)
    {
        if (UserRoles.IsSuperAdmin(actorRole)) return true;

        if (member.CreatedByUserId == null || member.CreatedByUserId == actorId)
            return true;

        var partnerIds = await _repo.GetScopedPartnerIdsAsync(actorId, ct);
        return member.CreatedByUserId.HasValue && partnerIds.Contains(member.CreatedByUserId.Value);
    }

    private static List<LeadCustomerRow> ApplyFilters(
        List<LeadCustomerRow> rows,
        string? search,
        string? sourceType,
        string? sourceName,
        string? employeeName,
        string? partnerName,
        string? planCode,
        DateTime? dateFrom,
        DateTime? dateToExclusive)
    {
        IEnumerable<LeadCustomerRow> q = rows;

        if (!string.IsNullOrWhiteSpace(sourceType))
        {
            var st = sourceType.Trim();
            q = q.Where(r => string.Equals(r.SourceType, st, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(sourceName))
        {
            var sn = sourceName.Trim();
            q = q.Where(r => (r.SourceName ?? "").Contains(sn, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(employeeName))
        {
            var en = employeeName.Trim();
            q = q.Where(r =>
                r.SourceType == LeadSourceTypes.Employee
                && ((r.SourceName ?? "").Contains(en, StringComparison.OrdinalIgnoreCase)
                    || (r.AssignedEmployee ?? "").Contains(en, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(partnerName))
        {
            var pn = partnerName.Trim();
            q = q.Where(r =>
                r.SourceType == LeadSourceTypes.Partner
                && (r.SourceName ?? "").Contains(pn, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(planCode))
        {
            var pc = planCode.Trim();
            q = q.Where(r => string.Equals(r.PlanCode, pc, StringComparison.OrdinalIgnoreCase));
        }

        if (dateFrom.HasValue)
            q = q.Where(r => r.Member.CreatedAt >= dateFrom.Value);
        if (dateToExclusive.HasValue)
            q = q.Where(r => r.Member.CreatedAt < dateToExclusive.Value);

        if (search is not null)
        {
            var s = search.ToLowerInvariant();
            q = q.Where(r =>
                r.Member.FullName.ToLower().Contains(s)
                || r.Member.Email.ToLower().Contains(s)
                || r.Member.Phone.Contains(s)
                || r.MemberId.ToLower().Contains(s)
                || (r.SourceName ?? "").ToLower().Contains(s)
                || (r.SourceCode ?? "").ToLower().Contains(s)
                || (r.PlanName ?? "").ToLower().Contains(s));
        }

        return q.ToList();
    }

    private static List<LeadCustomerRow> ApplySort(List<LeadCustomerRow> rows, string? sortBy, string? sortDir)
    {
        var asc = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
        var field = (sortBy ?? "").Trim().ToLowerInvariant();

        return field switch
        {
            "name" => asc
                ? rows.OrderBy(r => r.Member.FullName).ToList()
                : rows.OrderByDescending(r => r.Member.FullName).ToList(),
            "source" => asc
                ? rows.OrderBy(r => r.SourceType).ThenBy(r => r.SourceName).ToList()
                : rows.OrderByDescending(r => r.SourceType).ThenByDescending(r => r.SourceName).ToList(),
            "reports" => asc
                ? rows.OrderBy(r => r.ReportPurchaseCount + r.ReportGeneratedCount).ToList()
                : rows.OrderByDescending(r => r.ReportPurchaseCount + r.ReportGeneratedCount).ToList(),
            "membership" => asc
                ? rows.OrderBy(r => r.MembershipSalesCount).ToList()
                : rows.OrderByDescending(r => r.MembershipSalesCount).ToList(),
            "oldest" => rows.OrderBy(r => r.Member.CreatedAt).ToList(),
            _ => rows.OrderByDescending(r => r.Member.CreatedAt).ToList(),
        };
    }

    private static LeadAttributionStatsDto BuildStats(List<LeadCustomerRow> rows)
    {
        return new LeadAttributionStatsDto(
            rows.Count,
            rows.Count(r => r.SourceType == LeadSourceTypes.Direct),
            rows.Count(r => r.SourceType == LeadSourceTypes.Employee),
            rows.Count(r => r.SourceType == LeadSourceTypes.Partner),
            rows.Count(r => r.SourceType == LeadSourceTypes.Organic),
            rows.Count(r => r.SourceType == LeadSourceTypes.Campaign),
            rows.Count(r => r.SourceType == LeadSourceTypes.Unknown),
            rows.Sum(r => r.MembershipSalesCount),
            rows.Sum(r => r.ReportPurchaseCount + r.ReportGeneratedCount));
    }

    private static IReadOnlyList<LeadSourceBreakdownDto> BuildSourceBreakdown(List<LeadCustomerRow> rows) =>
        new[]
        {
            LeadSourceTypes.Employee,
            LeadSourceTypes.Partner,
            LeadSourceTypes.Direct,
            LeadSourceTypes.Organic,
            LeadSourceTypes.Campaign,
            LeadSourceTypes.Unknown,
        }
        .Select(t => new LeadSourceBreakdownDto(t, rows.Count(r => r.SourceType == t)))
        .Where(x => x.Count > 0)
        .ToList();

    private static IReadOnlyList<LeadPerformerDto> BuildTopPerformers(
        List<LeadCustomerRow> rows,
        string sourceType,
        Dictionary<string, PerformerRevenue> revenueByPerformer,
        int take) =>
        BuildTopPerformersInternal(rows, sourceType, revenueByPerformer, PerformerKinds.Employee, isRba: false, take);

    private static IReadOnlyList<LeadPerformerDto> BuildTopRbaPartners(
        List<LeadCustomerRow> rows,
        Dictionary<string, PerformerRevenue> revenueByPerformer,
        int take) =>
        BuildTopPerformersInternal(rows, LeadSourceTypes.Partner, revenueByPerformer, PerformerKinds.Rba, isRba: true, take);

    private static IReadOnlyList<LeadPerformerDto> BuildTopPerformersInternal(
        List<LeadCustomerRow> rows,
        string sourceType,
        Dictionary<string, PerformerRevenue> revenueByPerformer,
        string performerKind,
        bool isRba,
        int take)
    {
        var leadGroups = rows
            .Where(r =>
                !string.IsNullOrWhiteSpace(r.SourceName)
                && (!isRba || r.IsRba)
                && string.Equals(r.SourceType, sourceType, StringComparison.OrdinalIgnoreCase))
            .GroupBy(r => new { Name = r.SourceName!, Code = r.SourceCode ?? "" })
            .ToDictionary(
                g => PerformerAggregateKey(performerKind, g.Key.Code),
                g => g,
                StringComparer.OrdinalIgnoreCase);

        var keys = new HashSet<string>(leadGroups.Keys, StringComparer.OrdinalIgnoreCase);
        foreach (var key in revenueByPerformer.Keys)
        {
            if (key.StartsWith($"{performerKind}|", StringComparison.OrdinalIgnoreCase))
                keys.Add(key);
        }

        return keys
            .Select(key =>
            {
                var code = key[(performerKind.Length + 1)..];
                leadGroups.TryGetValue(key, out var g);
                var leads = g?.Count() ?? 0;
                var memberships = g?.Sum(x => x.MembershipSalesCount) ?? 0;
                var reports = g?.Sum(x => x.ReportPurchaseCount + x.ReportGeneratedCount) ?? 0;
                var rate = leads == 0 ? 0m : Math.Round((decimal)memberships / leads * 100m, 1);
                var revenue = LookupRevenue(revenueByPerformer, performerKind, code);
                var name = g?.Key.Name ?? revenue.Name ?? code;
                return new LeadPerformerDto(
                    name,
                    string.IsNullOrWhiteSpace(code) ? null : code,
                    leads,
                    memberships,
                    reports,
                    rate,
                    revenue.RevenuePaise,
                    revenue.PaymentCount);
            })
            .Where(x => x.LeadsGenerated > 0 || x.RevenuePaise > 0)
            .OrderByDescending(x => x.RevenuePaise)
            .ThenByDescending(x => x.LeadsGenerated)
            .ThenByDescending(x => x.PaymentCount)
            .Take(take)
            .ToList();
    }

    private async Task<Dictionary<string, PerformerRevenue>> BuildPerformerRevenueAsync(
        Guid actorId,
        string actorRole,
        string? dateFrom,
        string? dateTo,
        CancellationToken ct)
    {
        var scopedUserIds = await GetScopedMemberIdsAsync(actorId, actorRole, ct);
        if (scopedUserIds.Count == 0) return new Dictionary<string, PerformerRevenue>(StringComparer.OrdinalIgnoreCase);

        var paidFrom = AdminListQuery.ParseDateFrom(dateFrom);
        var paidToEx = AdminListQuery.ParseDateToExclusive(dateTo);
        var payments = await _repo.GetReferralPaymentsForUsersAsync(scopedUserIds, paidFrom, paidToEx, ct);
        var cache = new Dictionary<string, PerformerKey?>(StringComparer.OrdinalIgnoreCase);
        var aggregates = new Dictionary<string, PerformerRevenue>(StringComparer.OrdinalIgnoreCase);

        foreach (var payment in payments)
        {
            var performer = await ResolvePerformerKeyAsync(payment.ReferralCode, cache, ct);
            if (performer is null) continue;

            var key = PerformerAggregateKey(performer.Kind, performer.Code);
            if (!aggregates.TryGetValue(key, out var current))
                current = new PerformerRevenue(0, 0, performer.Name);

            aggregates[key] = new PerformerRevenue(
                current.RevenuePaise + payment.TotalAmountPaise,
                current.PaymentCount + 1,
                current.Name ?? performer.Name);
        }

        return aggregates;
    }

    private async Task<List<Guid>> GetScopedMemberIdsAsync(Guid actorId, string actorRole, CancellationToken ct)
    {
        var isSuperAdmin = UserRoles.IsSuperAdmin(actorRole);
        var partnerIds = isSuperAdmin
            ? Array.Empty<Guid>()
            : await _repo.GetScopedPartnerIdsAsync(actorId, ct);

        var members = await _repo.ListScopedMembersAsync(isSuperAdmin, actorId, partnerIds, ct);
        return members.Select(m => m.UserId).ToList();
    }

    private static PerformerRevenue LookupRevenue(
        Dictionary<string, PerformerRevenue> revenueByPerformer,
        string kind,
        string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return new PerformerRevenue(0, 0, null);
        return revenueByPerformer.TryGetValue(PerformerAggregateKey(kind, code.Trim()), out var revenue)
            ? revenue
            : new PerformerRevenue(0, 0, null);
    }

    private static string PerformerAggregateKey(string kind, string code) => $"{kind}|{code}";

    private static string? NormalizePerformerKind(string? performerType)
    {
        var value = (performerType ?? "").Trim().ToLowerInvariant();
        return value switch
        {
            "employee" => PerformerKinds.Employee,
            "rba" or "partner" => PerformerKinds.Rba,
            _ => null,
        };
    }

    private async Task<PerformerKey?> ResolvePerformerKeyAsync(
        string? referralCode,
        Dictionary<string, PerformerKey?> cache,
        CancellationToken ct)
    {
        var key = (referralCode ?? "").Trim();
        if (string.IsNullOrEmpty(key)) return null;
        if (cache.TryGetValue(key, out var cached)) return cached;

        var validated = await _referrals.ValidateReferralCodeAsync(key, ct);
        if (!validated.IsValid || validated.ReferralType is null)
        {
            cache[key] = null;
            return null;
        }

        var performer = validated.ReferralType == ReferralType.RBA
            ? new PerformerKey(
                PerformerKinds.Rba,
                validated.ReferralCode ?? key,
                validated.DisplayName ?? validated.ReferralCode ?? key)
            : new PerformerKey(
                PerformerKinds.Employee,
                validated.EmpCode ?? validated.ReferralCode ?? key,
                validated.DisplayName ?? validated.EmpCode ?? key);

        cache[key] = performer;
        return performer;
    }

    private static LeadPerformerPaymentDto ToPerformerPayment(ReferralPaymentRow payment) =>
        new(
            payment.UserId,
            string.IsNullOrWhiteSpace(payment.MemberId)
                ? MemberIdHelper.FormatLegacy(payment.UserId)
                : payment.MemberId.Trim().ToUpperInvariant(),
            payment.FullName,
            payment.PlanCode,
            payment.PlanName,
            SchemeDiscoveryCatalog.IsOneTimeReportPlan(payment.PlanCode) ? "Report" : "Membership",
            payment.PaidAt,
            payment.TotalAmountPaise,
            payment.ReferralCode);

    private static class PerformerKinds
    {
        public const string Employee = "employee";
        public const string Rba = "rba";
    }

    private sealed record PerformerKey(string Kind, string Code, string Name);

    private sealed record PerformerRevenue(long RevenuePaise, int PaymentCount, string? Name);

    private static LeadCustomerListItemDto ToListItem(LeadCustomerRow r) =>
        new(
            r.Member.UserId,
            r.MemberId,
            r.Member.FullName,
            r.Member.Phone,
            r.Member.Email,
            r.Member.CreatedAt,
            r.SourceType,
            r.SourceName,
            r.SourceCode,
            r.AssignedEmployee,
            r.MembershipStatus,
            r.PlanName,
            r.MembershipSalesCount,
            r.ReportPurchaseCount,
            r.ReportGeneratedCount,
            r.ReportPurchaseCount + r.ReportGeneratedCount);

    private static LeadCustomerDetailDto ToDetail(
        LeadCustomerRow r,
        IReadOnlyList<PaymentHistoryRow> paymentHistory) =>
        new(
            r.Member.UserId,
            r.MemberId,
            r.Member.FullName,
            r.Member.Phone,
            r.Member.Email,
            r.Member.CreatedAt,
            r.SourceType,
            r.SourceName,
            r.SourceCode,
            r.AssignedAdvisor,
            r.AssignedEmployee,
            r.CreatedThrough,
            r.MembershipStatus,
            r.PlanCode,
            r.PlanName,
            r.ReportPurchaseCount,
            r.ReportGeneratedCount,
            r.ReportPurchaseCount + r.ReportGeneratedCount,
            r.MembershipSalesCount,
            r.FirstPaidAt,
            r.FirstPaidPlanCode,
            r.FirstPaidPlanName,
            r.FirstReferralCodeRaw,
            r.RegistrationAdvisorCodeRaw,
            paymentHistory.Select(ToPaymentHistoryItem).ToList());

    private static LeadPaymentHistoryItemDto ToPaymentHistoryItem(PaymentHistoryRow row) =>
        new(
            row.PaymentOrderId,
            row.PlanCode,
            row.PlanName,
            SchemeDiscoveryCatalog.IsOneTimeReportPlan(row.PlanCode) ? "Report" : "Membership",
            row.PaidAt,
            row.ReferralCode,
            row.TotalAmountPaise);

    private sealed record AttributionInfo(
        bool IsRba,
        string SourceType,
        string? SourceName,
        string? SourceCode,
        string? AssignedAdvisor,
        string? AssignedEmployee);

    private sealed record LeadCustomerRow(
        User Member,
        string MemberId,
        bool IsRba,
        string SourceType,
        string? SourceName,
        string? SourceCode,
        string? AssignedAdvisor,
        string? AssignedEmployee,
        string CreatedThrough,
        string? PlanCode,
        string? PlanName,
        string MembershipStatus,
        int ReportPurchaseCount,
        int ReportGeneratedCount,
        int MembershipSalesCount,
        DateTime? FirstPaidAt,
        string? FirstPaidPlanCode,
        string? FirstPaidPlanName,
        string? FirstReferralCodeRaw,
        string? RegistrationAdvisorCodeRaw);
}
