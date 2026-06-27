using Microsoft.EntityFrameworkCore;
using RB_Website_API.Auth;
using RB_Website_API.Data;
using RB_Website_API.Models;

namespace RB_Website_API.Services;

public sealed class PaymentActivationService
{
    private readonly AppDbContext _db;
    private readonly MembershipEmailService _email;
    private readonly RegistrationWelcomeEmailService _registrationWelcome;

    public PaymentActivationService(
        AppDbContext db,
        MembershipEmailService email,
        RegistrationWelcomeEmailService registrationWelcome)
    {
        _db = db;
        _email = email;
        _registrationWelcome = registrationWelcome;
    }

    /// <summary>
    /// Marks order paid and activates or extends the user's plan. Idempotent per razorpayPaymentId.
    /// </summary>
    public async Task<ActivationResult> ActivatePaidOrderAsync(
        PaymentOrder po,
        string razorpayOrderId,
        string razorpayPaymentId,
        string? razorpaySignature,
        string? rawWebhookPayload,
        CancellationToken ct)
    {
        var alreadyPaid = await _db.Payments.AnyAsync(p => p.RazorpayPaymentId == razorpayPaymentId, ct);
        if (po.Status == "Paid" && alreadyPaid)
        {
            var existing = await GetActivePlanRowAsync(po.UserId, ct);
            return new ActivationResult(
                false,
                ActivationKind.None,
                null,
                existing?.PlanCode,
                existing?.PlanName,
                null,
                null,
                existing?.ActiveFrom,
                existing?.ActiveTo);
        }

        var plan = await _db.Plans.AsNoTracking().FirstOrDefaultAsync(p => p.PlanId == po.PlanId, ct);
        var now = DateTime.Now;

        if (string.Equals(plan?.Code, SchemeDiscoveryCatalog.OneTimePlanCode, StringComparison.OrdinalIgnoreCase))
        {
            return await ActivateOneTimeSchemeReportAsync(po, razorpayOrderId, razorpayPaymentId, razorpaySignature, rawWebhookPayload, alreadyPaid, ct);
        }

        Guid paymentId;

        if (!alreadyPaid)
        {
            paymentId = Guid.NewGuid();
            _db.Payments.Add(new Payment
            {
                PaymentId         = paymentId,
                PaymentOrderId    = po.PaymentOrderId,
                RazorpayOrderId   = razorpayOrderId,
                RazorpayPaymentId = razorpayPaymentId,
                RazorpaySignature = razorpaySignature,
                AmountPaise       = po.TotalAmountPaise,
                Currency          = po.Currency,
                Status            = "Captured",
                RawPayload        = rawWebhookPayload,
                PaidAt            = now,
                CreatedAt         = now,
            });
        }
        else
        {
            paymentId = await _db.Payments.AsNoTracking()
                .Where(p => p.RazorpayPaymentId == razorpayPaymentId)
                .Select(p => p.PaymentId)
                .FirstAsync(ct);
        }

        po.Status = "Paid";
        po.UpdatedAt = now;

        var durationDays = plan?.DurationDays ?? 365;
        var planName = plan?.Name ?? po.PlanCode;

        var renewSamePlan = await _db.UserPlans.FirstOrDefaultAsync(up =>
            up.UserId == po.UserId
            && up.Status == "Active"
            && up.PlanCode == po.PlanCode
            && up.ActiveTo != null
            && up.ActiveTo > now, ct);

        if (renewSamePlan is not null)
        {
            renewSamePlan.ActiveTo = renewSamePlan.ActiveTo!.Value.AddDays(durationDays);
            renewSamePlan.CancelAtPeriodEnd = false;
            renewSamePlan.CancelledAt = null;
            renewSamePlan.PaymentOrderId = po.PaymentOrderId;
            renewSamePlan.UpdatedAt = now;
            await _db.SaveChangesAsync(ct);

            var renewalResult = new ActivationResult(
                true,
                ActivationKind.Renewal,
                paymentId,
                po.PlanCode,
                planName,
                null,
                null,
                renewSamePlan.ActiveFrom,
                renewSamePlan.ActiveTo);
            await _email.SendActivationEmailAsync(renewalResult, ct);
            return renewalResult;
        }

        UserPlan? previousActive = null;
        Plan? previousPlan = null;
        var others = await _db.UserPlans.Where(up => up.UserId == po.UserId && up.Status == "Active").ToListAsync(ct);
        if (others.Count > 0)
        {
            previousActive = others[0];
            previousPlan = await _db.Plans.AsNoTracking()
                .FirstOrDefaultAsync(p => p.PlanId == previousActive.PlanId, ct);
        }

        foreach (var up in others)
        {
            up.Status = "Cancelled";
            up.ActiveTo = now;
            up.UpdatedAt = now;
        }

        var activeFrom = now;
        var activeTo = activeFrom.AddDays(durationDays);
        _db.UserPlans.Add(new UserPlan
        {
            UserPlanId         = Guid.NewGuid(),
            UserId             = po.UserId,
            PlanId             = po.PlanId,
            PlanCode           = po.PlanCode,
            PaymentOrderId     = po.PaymentOrderId,
            ActiveFrom         = activeFrom,
            ActiveTo           = activeTo,
            Status             = "Active",
            CancelAtPeriodEnd  = false,
            AutoRenewEnabled   = false,
            CreatedAt          = activeFrom,
            UpdatedAt          = activeFrom,
        });

        await _db.SaveChangesAsync(ct);

        var kind = previousActive is null ? ActivationKind.FirstPurchase : ActivationKind.Upgrade;
        var result = new ActivationResult(
            true,
            kind,
            paymentId,
            po.PlanCode,
            planName,
            previousActive?.PlanCode,
            previousPlan?.Name,
            activeFrom,
            activeTo);

        if (kind == ActivationKind.FirstPurchase)
            await _registrationWelcome.TrySendPaidWelcomeAfterFirstPurchaseAsync(result, ct);
        else
            await _email.SendActivationEmailAsync(result, ct);
        return result;
    }

    private async Task<ActivationResult> ActivateOneTimeSchemeReportAsync(
        PaymentOrder po,
        string razorpayOrderId,
        string razorpayPaymentId,
        string? razorpaySignature,
        string? rawWebhookPayload,
        bool alreadyPaid,
        CancellationToken ct)
    {
        var now = DateTime.Now;
        Guid paymentId;

        if (!alreadyPaid)
        {
            paymentId = Guid.NewGuid();
            _db.Payments.Add(new Payment
            {
                PaymentId         = paymentId,
                PaymentOrderId    = po.PaymentOrderId,
                RazorpayOrderId   = razorpayOrderId,
                RazorpayPaymentId = razorpayPaymentId,
                RazorpaySignature = razorpaySignature,
                AmountPaise       = po.TotalAmountPaise,
                Currency          = po.Currency,
                Status            = "Captured",
                RawPayload        = rawWebhookPayload,
                PaidAt            = now,
                CreatedAt         = now,
            });
        }
        else
        {
            paymentId = await _db.Payments.AsNoTracking()
                .Where(p => p.RazorpayPaymentId == razorpayPaymentId)
                .Select(p => p.PaymentId)
                .FirstAsync(ct);
        }

        po.Status = "Paid";
        po.UpdatedAt = now;

        var plan = await _db.Plans.AsNoTracking().FirstOrDefaultAsync(p => p.PlanId == po.PlanId, ct);
        var durationDays = plan?.DurationDays > 0 ? plan.DurationDays : 365;
        var activeFrom = now;
        var activeTo = activeFrom.AddDays(durationDays);

        // One-time scheme report is its own membership — no separate annual plan required.
        var priorOneTime = await _db.UserPlans
            .Where(up =>
                up.UserId == po.UserId
                && up.PlanCode == SchemeDiscoveryCatalog.OneTimePlanCode
                && up.Status == "Active")
            .ToListAsync(ct);
        foreach (var up in priorOneTime)
        {
            up.Status = "Cancelled";
            up.ActiveTo = now;
            up.UpdatedAt = now;
        }

        _db.UserPlans.Add(new UserPlan
        {
            UserPlanId = Guid.NewGuid(),
            UserId = po.UserId,
            PlanId = po.PlanId,
            PlanCode = po.PlanCode,
            PaymentOrderId = po.PaymentOrderId,
            ActiveFrom = activeFrom,
            ActiveTo = activeTo,
            Status = "Active",
            CancelAtPeriodEnd = false,
            AutoRenewEnabled = false,
            CreatedAt = activeFrom,
            UpdatedAt = activeFrom,
        });

        await _db.SaveChangesAsync(ct);

        return new ActivationResult(
            true,
            ActivationKind.OneTimeReport,
            paymentId,
            po.PlanCode,
            plan?.Name ?? po.PlanCode,
            null,
            null,
            activeFrom,
            activeTo);
    }

    public async Task<ActivePlanRow?> GetActivePlanRowAsync(Guid userId, CancellationToken ct)
    {
        var now = DateTime.Now;
        return await (
            from up in _db.UserPlans.AsNoTracking()
            join p in _db.Plans.AsNoTracking() on up.PlanId equals p.PlanId
            where up.UserId == userId
                  && up.Status == "Active"
                  && (up.ActiveTo == null || up.ActiveTo > now)
            orderby up.ActiveFrom descending
            select new ActivePlanRow(
                up.PlanCode,
                p.Name,
                up.ActiveFrom,
                up.ActiveTo,
                up.Status,
                up.CancelAtPeriodEnd,
                up.AutoRenewEnabled)
        ).FirstOrDefaultAsync(ct);
    }

    public sealed record ActivePlanRow(
        string PlanCode,
        string PlanName,
        DateTime ActiveFrom,
        DateTime? ActiveTo,
        string Status,
        bool CancelAtPeriodEnd,
        bool AutoRenewEnabled);
}
