using Microsoft.EntityFrameworkCore;
using RB_Website_API.Auth;
using RB_Website_API.Data;
using RB_Website_API.DTO;
using RB_Website_API.Models;

namespace RB_Website_API.Services;

public sealed class SchemeDiscoveryService : ISchemeDiscoveryService
{
    private readonly AppDbContext _db;
    private readonly IMemberIdGeneratorService _memberIds;
    private readonly ISdrReportService _sdrReports;
    private readonly IReportEmailService _reportEmail;
    private readonly ILogger<SchemeDiscoveryService> _logger;

    public SchemeDiscoveryService(
        AppDbContext db,
        IMemberIdGeneratorService memberIds,
        ISdrReportService sdrReports,
        IReportEmailService reportEmail,
        ILogger<SchemeDiscoveryService> logger)
    {
        _db = db;
        _memberIds = memberIds;
        _sdrReports = sdrReports;
        _reportEmail = reportEmail;
        _logger = logger;
    }

    public async Task<SchemeDiscoveryStatusDto> GetStatusAsync(Guid userId, CancellationToken ct)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId, ct);
        if (user is null)
            return Fail("not_found", "User not found. Please login again.");

        var profile = new SchemeDiscoveryProfileDto(
            user.FullName,
            user.Email,
            user.Phone,
            _memberIds.GetDisplayMemberId(user));

        var awaitingDraft = await _db.SchemeDiscoveryRequests.AsNoTracking()
            .Where(r => r.UserId == userId && r.Status == SchemeDiscoveryCatalog.StatusAwaitingPayment)
            .OrderByDescending(r => r.UpdatedAt)
            .FirstOrDefaultAsync(ct);
        if (awaitingDraft is not null)
        {
            return new SchemeDiscoveryStatusDto(
                true,
                "awaiting_payment",
                "Your Udyam number is saved. Complete referral and payment to generate your report.",
                profile,
                null,
                false,
                false,
                null,
                null,
                awaitingDraft.Id,
                awaitingDraft.UdyamNumber);
        }

        var now = DateTime.Now;
        var activePlan = await (
            from up in _db.UserPlans.AsNoTracking()
            join p in _db.Plans.AsNoTracking() on up.PlanId equals p.PlanId
            where up.UserId == userId
                  && up.Status == "Active"
                  && (up.ActiveTo == null || up.ActiveTo > now)
            orderby up.ActiveFrom descending
            select new { up, p }
        ).FirstOrDefaultAsync(ct);

        if (activePlan is null)
        {
            var standaloneOneTime = await FindUnusedStandaloneOneTimePaymentIdAsync(userId, ct);
            if (standaloneOneTime is not null)
            {
                return new SchemeDiscoveryStatusDto(
                    true,
                    "udyam_form",
                    null,
                    profile,
                    null,
                    false,
                    true,
                    null,
                    null);
            }

            return new SchemeDiscoveryStatusDto(
                true,
                "no_membership",
                "Purchase a one-time Government Scheme Discovery Report or upgrade to Premium/Pro.",
                profile,
                null,
                false,
                false,
                null,
                null);
        }

        if (SchemeDiscoveryCatalog.IsOneTimeReportPlan(activePlan.up.PlanCode))
        {
            var oneTimeMembership = new SchemeDiscoveryMembershipDto(
                activePlan.up.UserPlanId,
                activePlan.up.PlanCode,
                activePlan.p.Name,
                activePlan.up.ActiveFrom,
                activePlan.up.ActiveTo);

            var oneTimeReport = await FindExistingReportAsync(userId, activePlan.up.UserPlanId, ct);
            if (oneTimeReport is not null)
            {
                return new SchemeDiscoveryStatusDto(
                    true,
                    "report_exists",
                    "Your Government Scheme Discovery Report has already been generated.",
                    profile,
                    oneTimeMembership,
                    false,
                    false,
                    oneTimeReport,
                    null);
            }

            var oneTimePending = await _db.SchemeDiscoveryRequests.AsNoTracking()
                .Where(r => r.UserId == userId
                            && r.UserPlanId == activePlan.up.UserPlanId
                            && (r.Status == SchemeDiscoveryCatalog.StatusPending
                                || r.Status == SchemeDiscoveryCatalog.StatusProcessing
                                || r.Status == SchemeDiscoveryCatalog.StatusCompleted))
                .OrderByDescending(r => r.RequestedAt)
                .FirstOrDefaultAsync(ct);
            if (oneTimePending is not null
                && oneTimePending.Status is SchemeDiscoveryCatalog.StatusPending or SchemeDiscoveryCatalog.StatusProcessing)
            {
                return new SchemeDiscoveryStatusDto(
                    true,
                    "request_pending",
                    "Your scheme discovery report request is being processed.",
                    profile,
                    oneTimeMembership,
                    false,
                    false,
                    null,
                    oneTimePending.Status,
                    PendingRequestId: oneTimePending.Id);
            }

            var oneTimePaid = await HasUnusedOneTimePaymentAsync(userId, activePlan.up.UserPlanId, activePlan.up.ActiveFrom, ct);
            if (oneTimePaid)
            {
                return new SchemeDiscoveryStatusDto(
                    true,
                    "udyam_form",
                    null,
                    profile,
                    oneTimeMembership,
                    false,
                    true,
                    null,
                    null);
            }

            return new SchemeDiscoveryStatusDto(
                true,
                "no_membership",
                "Purchase a one-time Government Scheme Discovery Report to continue.",
                profile,
                oneTimeMembership,
                false,
                false,
                null,
                null);
        }

        var membership = new SchemeDiscoveryMembershipDto(
            activePlan.up.UserPlanId,
            activePlan.up.PlanCode,
            activePlan.p.Name,
            activePlan.up.ActiveFrom,
            activePlan.up.ActiveTo);

        var existingReport = await FindExistingReportAsync(userId, activePlan.up.UserPlanId, ct);
        if (existingReport is not null)
        {
            return new SchemeDiscoveryStatusDto(
                true,
                "report_exists",
                "Your Government Scheme Discovery Report has already been generated for your current membership period.",
                profile,
                membership,
                SchemeDiscoveryCatalog.IsIncludedPlan(activePlan.up.PlanCode),
                false,
                existingReport,
                null);
        }

        var activeRequest = await _db.SchemeDiscoveryRequests.AsNoTracking()
            .Where(r => r.UserId == userId
                        && r.UserPlanId == activePlan.up.UserPlanId
                        && (r.Status == SchemeDiscoveryCatalog.StatusPending
                            || r.Status == SchemeDiscoveryCatalog.StatusProcessing
                            || r.Status == SchemeDiscoveryCatalog.StatusCompleted))
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(ct);

        if (activeRequest is not null)
        {
            if (activeRequest.Status is SchemeDiscoveryCatalog.StatusPending or SchemeDiscoveryCatalog.StatusProcessing)
            {
                return new SchemeDiscoveryStatusDto(
                    true,
                    "request_pending",
                    "Your scheme discovery report request is being processed.",
                    profile,
                    membership,
                    SchemeDiscoveryCatalog.IsIncludedPlan(activePlan.up.PlanCode),
                    false,
                    null,
                    activeRequest.Status,
                    PendingRequestId: activeRequest.Id);
            }
        }

        var isPremiumOrPro = SchemeDiscoveryCatalog.IsIncludedPlan(activePlan.up.PlanCode);
        var hasOneTime = await HasUnusedOneTimePaymentAsync(userId, activePlan.up.UserPlanId, activePlan.up.ActiveFrom, ct);

        if (isPremiumOrPro || hasOneTime)
        {
            return new SchemeDiscoveryStatusDto(
                true,
                "udyam_form",
                null,
                profile,
                membership,
                isPremiumOrPro,
                hasOneTime,
                null,
                null);
        }

        return new SchemeDiscoveryStatusDto(
            true,
            "plan_choice",
            "Upgrade to Premium/Pro or purchase a one-time scheme discovery report.",
            profile,
            membership,
            false,
            false,
            null,
            null);
    }

    public async Task<SchemeDiscoverySubmitResponse> SaveDraftAsync(
        Guid userId,
        string udyamNumber,
        CancellationToken ct)
    {
        var udyamErr = ValidateUdyam(udyamNumber);
        if (udyamErr is not null)
            return new SchemeDiscoverySubmitResponse(false, udyamErr, null, null);

        var user = await _db.Users.AsNoTracking().FirstAsync(u => u.UserId == userId, ct);
        var normalized = udyamNumber.Trim().ToUpperInvariant();
        var now = DateTime.Now;

        var drafts = await _db.SchemeDiscoveryRequests
            .Where(r => r.UserId == userId && r.Status == SchemeDiscoveryCatalog.StatusAwaitingPayment)
            .ToListAsync(ct);

        var draft = drafts.OrderByDescending(r => r.UpdatedAt).FirstOrDefault();
        if (draft is null)
        {
            draft = new SchemeDiscoveryRequest
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                MemberId = _memberIds.GetDisplayMemberId(user),
                UserPlanId = Guid.Empty,
                UdyamNumber = normalized,
                PaymentId = null,
                EntitlementType = SchemeDiscoveryCatalog.EntitlementOneTime,
                Status = SchemeDiscoveryCatalog.StatusAwaitingPayment,
                RequestedAt = now,
                CreatedAt = now,
                UpdatedAt = now,
            };
            _db.SchemeDiscoveryRequests.Add(draft);
        }
        else
        {
            draft.UdyamNumber = normalized;
            draft.UpdatedAt = now;
            foreach (var extra in drafts.Where(d => d.Id != draft.Id))
            {
                extra.Status = SchemeDiscoveryCatalog.StatusFailed;
                extra.ErrorMessage = "Superseded by a newer draft.";
                extra.UpdatedAt = now;
            }
        }

        await _db.SaveChangesAsync(ct);

        return new SchemeDiscoverySubmitResponse(
            true,
            "Udyam number saved. Continue to payment.",
            draft.Id,
            draft.Status);
    }

    public async Task<SchemeDiscoverySubmitResponse> FinalizeRequestAsync(
        Guid userId,
        Guid requestId,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "[SchemeDiscovery] Finalize START UserId={UserId} RequestId={RequestId}",
            userId,
            requestId);

        var request = await _db.SchemeDiscoveryRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.UserId == userId, ct);

        if (request is null)
        {
            _logger.LogWarning(
                "[SchemeDiscovery] Finalize ABORT — request not found UserId={UserId} RequestId={RequestId}",
                userId,
                requestId);
            return new SchemeDiscoverySubmitResponse(false, "Saved report request not found.", null, null);
        }

        _logger.LogInformation(
            "[SchemeDiscovery] Finalize loaded RequestId={RequestId} Status={Status} PaymentId={PaymentId} Udyam={UdyamMasked}",
            request.Id,
            request.Status,
            request.PaymentId,
            SaarthiApiLogHelper.MaskUdyam(request.UdyamNumber));

        if (request.Status == SchemeDiscoveryCatalog.StatusCompleted && request.CustomerReportId is not null)
        {
            var existingReport = await _sdrReports.HasValidGeneratedReportAsync(userId, ct);
            return MapGenerationResult(
                new SdrGenerationResult(
                    true,
                    existingReport is not null ? SdrReportCatalog.OutcomeDuplicate : SdrReportCatalog.OutcomeGenerated,
                    existingReport is not null
                        ? "Your Government Scheme Discovery Report has already been generated and remains valid for one year from the date of generation. Please contact support if you need assistance."
                        : "Your Government Scheme Discovery Report has been generated successfully.",
                    request.CustomerReportId,
                    existingReport?.ExpiryDate,
                    request.Status),
                request.Id);
        }

        if (request.Status is not SchemeDiscoveryCatalog.StatusAwaitingPayment
            and not SchemeDiscoveryCatalog.StatusPending
            and not SchemeDiscoveryCatalog.StatusProcessing
            and not SchemeDiscoveryCatalog.StatusFailed)
        {
            return new SchemeDiscoverySubmitResponse(false, "This report request cannot be finalized.", null, request.Status);
        }

        if (request.Status == SchemeDiscoveryCatalog.StatusAwaitingPayment)
        {
            var activeOneTime = await (
                from up in _db.UserPlans.AsNoTracking()
                where up.UserId == userId
                      && up.Status == "Active"
                      && up.PlanCode == SchemeDiscoveryCatalog.OneTimePlanCode
                orderby up.ActiveFrom descending
                select up
            ).FirstOrDefaultAsync(ct);

            Guid? paymentId = null;
            if (activeOneTime is not null)
            {
                paymentId = await FindUnusedOneTimePaymentIdAsync(
                    userId,
                    activeOneTime.UserPlanId,
                    activeOneTime.ActiveFrom,
                    ct);
            }

            paymentId ??= await FindUnusedStandaloneOneTimePaymentIdAsync(userId, ct);
            if (paymentId is null)
                return new SchemeDiscoverySubmitResponse(false, "Payment not found. Please complete payment and try again.", null, null);

            var now = DateTime.Now;
            request.PaymentId = paymentId;
            request.UserPlanId = activeOneTime?.UserPlanId ?? Guid.Empty;
            request.Status = SchemeDiscoveryCatalog.StatusPending;
            request.RequestedAt = now;
            request.UpdatedAt = now;
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "[SchemeDiscovery] Payment linked UserId={UserId} RequestId={RequestId} PaymentId={PaymentId} UserPlanId={UserPlanId}",
                userId,
                request.Id,
                paymentId,
                request.UserPlanId);
        }

        _logger.LogInformation(
            "[SchemeDiscovery] Invoking SDR generation UserId={UserId} RequestId={RequestId}",
            userId,
            request.Id);

        var generation = await _sdrReports.GenerateAfterOneTimePaymentAsync(userId, request, ct);

        _logger.LogInformation(
            "[SchemeDiscovery] Finalize COMPLETE UserId={UserId} RequestId={RequestId} Outcome={Outcome} ReportId={ReportId} Status={Status}",
            userId,
            request.Id,
            generation.Outcome,
            generation.ReportId,
            generation.RequestStatus);

        return MapGenerationResult(generation, request.Id);
    }

    private static SchemeDiscoverySubmitResponse MapGenerationResult(SdrGenerationResult generation, Guid requestId) =>
        new(
            generation.Success,
            generation.UserMessage,
            requestId,
            generation.RequestStatus,
            generation.Outcome,
            generation.ReportId,
            generation.ExpiryDate);

    public async Task<SchemeDiscoverySubmitResponse> SubmitRequestAsync(
        Guid userId,
        string udyamNumber,
        CancellationToken ct)
    {
        var udyamErr = ValidateUdyam(udyamNumber);
        if (udyamErr is not null)
            return new SchemeDiscoverySubmitResponse(false, udyamErr, null, null);

        var status = await GetStatusAsync(userId, ct);
        if (!status.Success)
            return new SchemeDiscoverySubmitResponse(false, status.Message ?? "Unable to submit request.", null, null);

        if (status.Phase is not "udyam_form")
        {
            return new SchemeDiscoverySubmitResponse(
                false,
                status.Message ?? "You are not eligible to submit a scheme discovery request at this time.",
                null,
                null);
        }

        var user = await _db.Users.AsNoTracking().FirstAsync(u => u.UserId == userId, ct);
        var userPlanId = status.Membership?.UserPlanId ?? Guid.Empty;
        var now = DateTime.Now;

        Guid? paymentId = null;
        var entitlement = SchemeDiscoveryCatalog.EntitlementMembership;
        if (!status.IsPremiumOrPro && status.HasOneTimeEntitlement)
        {
            entitlement = SchemeDiscoveryCatalog.EntitlementOneTime;
            paymentId = status.Membership is not null
                ? await FindUnusedOneTimePaymentIdAsync(userId, userPlanId, status.Membership.ActiveFrom, ct)
                : await FindUnusedStandaloneOneTimePaymentIdAsync(userId, ct);
        }

        var request = new SchemeDiscoveryRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MemberId = _memberIds.GetDisplayMemberId(user),
            UserPlanId = userPlanId,
            UdyamNumber = udyamNumber.Trim().ToUpperInvariant(),
            PaymentId = paymentId,
            EntitlementType = entitlement,
            Status = SchemeDiscoveryCatalog.StatusPending,
            RequestedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.SchemeDiscoveryRequests.Add(request);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "[SchemeDiscovery] Invoking SDR generation from SubmitRequest UserId={UserId} RequestId={RequestId}",
            userId,
            request.Id);

        var generation = await _sdrReports.GenerateAfterOneTimePaymentAsync(userId, request, ct);

        _logger.LogInformation(
            "[SchemeDiscovery] SubmitRequest COMPLETE UserId={UserId} RequestId={RequestId} Outcome={Outcome} ReportId={ReportId} Status={Status}",
            userId,
            request.Id,
            generation.Outcome,
            generation.ReportId,
            generation.RequestStatus);

        return MapGenerationResult(generation, request.Id);
    }

    public async Task<(bool Success, string? Message)> EmailReportAsync(
        Guid userId,
        Guid reportId,
        CancellationToken ct)
    {
        var report = await _db.CustomerReports.AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.Id == reportId && r.CustomerId == userId && r.IsActive,
                ct);

        if (report is null)
            return (false, "Report not found.");

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId, ct);
        if (user is null)
            return (false, "User not found. Please login again.");

        await _reportEmail.SendReportReadyEmailAsync(user, report, ct);
        return (true, "Report sent to your registered email address.");
    }

    private async Task<SchemeDiscoveryExistingReportDto?> FindExistingReportAsync(
        Guid userId,
        Guid userPlanId,
        CancellationToken ct)
    {
        var now = DateTime.Now;
        var report = await _db.CustomerReports.AsNoTracking()
            .Where(r => r.CustomerId == userId
                        && r.SubscriptionId == userPlanId
                        && r.IsActive
                        && (r.ReportType == SchemeDiscoveryCatalog.ReportType
                            || r.ReportType == SdrReportCatalog.ReportType)
                        && (r.ExpiryDate == null || r.ExpiryDate >= now))
            .OrderByDescending(r => r.UploadDate)
            .FirstOrDefaultAsync(ct);

        if (report is not null)
        {
            return new SchemeDiscoveryExistingReportDto(
                report.Id,
                report.OriginalFileName,
                report.UploadDate);
        }

        var completed = await _db.SchemeDiscoveryRequests.AsNoTracking()
            .Where(r => r.UserId == userId
                        && r.UserPlanId == userPlanId
                        && r.Status == SchemeDiscoveryCatalog.StatusCompleted
                        && r.CustomerReportId != null)
            .OrderByDescending(r => r.CompletedAt)
            .FirstOrDefaultAsync(ct);

        if (completed?.CustomerReportId is null) return null;

        var linked = await _db.CustomerReports.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == completed.CustomerReportId && r.IsActive, ct);

        return linked is null
            ? null
            : new SchemeDiscoveryExistingReportDto(linked.Id, linked.OriginalFileName, linked.UploadDate);
    }

    private async Task<bool> HasUnusedOneTimePaymentAsync(
        Guid userId,
        Guid userPlanId,
        DateTime activeFrom,
        CancellationToken ct)
        => await FindUnusedOneTimePaymentIdAsync(userId, userPlanId, activeFrom, ct) is not null;

    private async Task<Guid?> FindUnusedOneTimePaymentIdAsync(
        Guid userId,
        Guid userPlanId,
        DateTime activeFrom,
        CancellationToken ct)
    {
        var paidPayments = await (
            from pay in _db.Payments.AsNoTracking()
            join po in _db.PaymentOrders.AsNoTracking() on pay.PaymentOrderId equals po.PaymentOrderId
            where po.UserId == userId
                  && po.PlanCode == SchemeDiscoveryCatalog.OneTimePlanCode
                  && po.Status == "Paid"
                  && pay.PaidAt >= activeFrom
            orderby pay.PaidAt descending
            select pay.PaymentId
        ).ToListAsync(ct);

        if (paidPayments.Count == 0) return null;

        var used = await _db.SchemeDiscoveryRequests.AsNoTracking()
            .Where(r => r.UserId == userId
                        && r.UserPlanId == userPlanId
                        && r.PaymentId != null
                        && paidPayments.Contains(r.PaymentId.Value))
            .Select(r => r.PaymentId!.Value)
            .ToListAsync(ct);

        return paidPayments.FirstOrDefault(id => !used.Contains(id));
    }

    private async Task<Guid?> FindUnusedStandaloneOneTimePaymentIdAsync(Guid userId, CancellationToken ct)
    {
        var paidPayments = await (
            from pay in _db.Payments.AsNoTracking()
            join po in _db.PaymentOrders.AsNoTracking() on pay.PaymentOrderId equals po.PaymentOrderId
            where po.UserId == userId
                  && po.PlanCode == SchemeDiscoveryCatalog.OneTimePlanCode
                  && po.Status == "Paid"
            orderby pay.PaidAt descending
            select pay.PaymentId
        ).ToListAsync(ct);

        if (paidPayments.Count == 0) return null;

        var used = await _db.SchemeDiscoveryRequests.AsNoTracking()
            .Where(r => r.UserId == userId
                        && r.PaymentId != null
                        && paidPayments.Contains(r.PaymentId.Value))
            .Select(r => r.PaymentId!.Value)
            .ToListAsync(ct);

        return paidPayments.FirstOrDefault(id => !used.Contains(id));
    }

    private static string? ValidateUdyam(string? raw)
    {
        var u = (raw ?? "").Trim();
        if (string.IsNullOrEmpty(u)) return "Udyam Registration Number is required.";
        if (!System.Text.RegularExpressions.Regex.IsMatch(u, "^UDYAM-[A-Z]{2}-\\d{2}-\\d{7}$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            return "Enter a valid Udyam number (e.g. UDYAM-XX-00-0000000).";
        return null;
    }

    private static SchemeDiscoveryStatusDto Fail(string phase, string message)
        => new(false, phase, message, null, null, false, false, null, null);
}
