using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RB_Website_API.Auth;
using RB_Website_API.Data;
using RB_Website_API.Models;
using RB_Website_API.Referrals.Services;
using RB_Website_API.Services;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/payment")]
public sealed class PaymentController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly RazorpaySettings _rzp;
    private readonly IHttpClientFactory _http;
    private readonly PaymentActivationService _activation;
    private readonly InvoicePdfService _invoices;
    private readonly IReferralService _referral;
    private readonly ILeadPushService _leadPush;
    private readonly IEmployeeValidationService _employeeValidation;
    private readonly ReferralSettings _referralSettings;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        AppDbContext db,
        IOptions<RazorpaySettings> rzp,
        IHttpClientFactory http,
        PaymentActivationService activation,
        InvoicePdfService invoices,
        IReferralService referral,
        ILeadPushService leadPush,
        IEmployeeValidationService employeeValidation,
        IOptions<ReferralSettings> referralSettings,
        ILogger<PaymentController> logger)
    {
        _db = db;
        _rzp = rzp.Value;
        _http = http;
        _activation = activation;
        _invoices = invoices;
        _referral = referral;
        _leadPush = leadPush;
        _employeeValidation = employeeValidation;
        _referralSettings = referralSettings.Value;
        _logger = logger;
    }

    public sealed record CreateOrderRequest(string PlanCode, string? ReferralCode = null);

    public sealed record CreateOrderResponse(
        bool Success,
        string? Message = null,
        string? KeyId = null,
        Guid? PaymentOrderId = null,
        string? RazorpayOrderId = null,
        long? AmountPaise = null,
        string? Currency = null,
        string? PlanCode = null,
        string? PlanName = null,
        string? PrefillName = null,
        string? PrefillEmail = null,
        string? PrefillContact = null
    );

    public sealed record VerifyRequest(
        Guid PaymentOrderId,
        string RazorpayOrderId,
        string RazorpayPaymentId,
        string RazorpaySignature
    );

    public sealed record VerifyResponse(
        bool Success,
        string? Message = null,
        string? PlanCode = null,
        DateTime? ActiveFrom = null,
        DateTime? ActiveTo = null,
        string? ActivationKind = null,
        string? PreviousPlanName = null
    );

    public sealed record ActivePlanDto(
        string PlanCode,
        string PlanName,
        DateTime ActiveFrom,
        DateTime? ActiveTo,
        string Status,
        bool CancelAtPeriodEnd,
        bool AutoRenewEnabled,
        int? DaysRemaining
    );

    public sealed record MyPlanResponse(bool Success, string? Message = null, ActivePlanDto? Plan = null);

    public sealed record CancelSubscriptionResponse(bool Success, string? Message = null, DateTime? ActiveTo = null);

    public sealed record PaymentHistoryItemDto(
        Guid PaymentOrderId,
        string PlanCode,
        string PlanName,
        long AmountPaise,
        string OrderStatus,
        string? PaymentStatus,
        DateTime CreatedAt,
        DateTime? PaidAt
    );

    public sealed record PaymentHistoryResponse(bool Success, string? Message = null, List<PaymentHistoryItemDto>? Items = null);

    public sealed record InvoiceListItemDto(
        Guid PaymentId,
        string InvoiceNumber,
        string PlanCode,
        string PlanName,
        long AmountPaise,
        DateTime PaidAt,
        DateTime? ActiveTo
    );

    public sealed record InvoiceListResponse(bool Success, string? Message = null, List<InvoiceListItemDto>? Items = null);

    [Authorize(Policy = "MemberAccess")]
    [HttpPost("razorpay/order")]
    public async Task<ActionResult<CreateOrderResponse>> CreateOrder([FromBody] CreateOrderRequest? req, CancellationToken ct)
    {
        var userId = CurrentUser.RequireUserId(User);
        if (req is null || string.IsNullOrWhiteSpace(req.PlanCode))
            return BadRequest(new CreateOrderResponse(false, "Plan code is required."));

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId, ct);
        if (user is null) return Unauthorized(new CreateOrderResponse(false, "User not found. Please login again."));

        var planCode = req.PlanCode.Trim().ToLowerInvariant();
        var plan = await _db.Plans.AsNoTracking().FirstOrDefaultAsync(p => p.Code == planCode && p.IsActive, ct);
        if (plan is null) return NotFound(new CreateOrderResponse(false, "Plan not found."));

        var now = DateTime.Now;
        var existingActive = await _db.UserPlans.AsNoTracking()
            .FirstOrDefaultAsync(up =>
                up.UserId == userId
                && up.Status == "Active"
                && (up.ActiveTo == null || up.ActiveTo > now), ct);
        if (existingActive is not null && string.Equals(existingActive.PlanCode, plan.Code, StringComparison.OrdinalIgnoreCase))
            return Conflict(new CreateOrderResponse(false, $"You already have {plan.Name} active."));

        if (!string.IsNullOrWhiteSpace(req.ReferralCode))
        {
            var validation = await _employeeValidation.ValidateReferralCodeAsync(req.ReferralCode, ct);
            if (!validation.IsValid)
            {
                if (_referralSettings.StrictReferralValidationOnOrderCreate)
                    return BadRequest(new CreateOrderResponse(false, validation.Message ?? "Invalid referral code."));
                _logger.LogWarning("Invalid referral code on order create (non-strict): {Code}", req.ReferralCode);
            }
        }

        if (string.IsNullOrWhiteSpace(_rzp.KeyId) || string.IsNullOrWhiteSpace(_rzp.KeySecret))
        {
            _logger.LogError("Razorpay credentials are missing in configuration.");
            return StatusCode(500, new CreateOrderResponse(false, "Payment is not configured. Please try later."));
        }

        var orderId = Guid.NewGuid();
        var receipt = $"mbm_{orderId.ToString("N")[..16]}";

        var po = new PaymentOrder
        {
            PaymentOrderId   = orderId,
            UserId           = userId,
            PlanId           = plan.PlanId,
            PlanCode         = plan.Code,
            BaseAmountPaise  = plan.BaseAmountPaise,
            GstPaise         = plan.GstPaise,
            TotalAmountPaise = plan.TotalAmountPaise,
            Currency         = plan.Currency,
            Provider         = "Razorpay",
            Receipt          = receipt,
            Status           = "Created",
            CreatedAt        = now,
            UpdatedAt        = now,
        };
        _db.PaymentOrders.Add(po);
        await _db.SaveChangesAsync(ct);

        await _referral.SaveReferralForOrderAsync(orderId, req.ReferralCode, ct);

        try
        {
            var client = _http.CreateClient("Razorpay");
            using var msg = new HttpRequestMessage(HttpMethod.Post, $"{_rzp.BaseUrl.TrimEnd('/')}/orders");
            var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_rzp.KeyId}:{_rzp.KeySecret}"));
            msg.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);

            var payload = new Dictionary<string, object?>
            {
                ["amount"]          = plan.TotalAmountPaise,
                ["currency"]        = plan.Currency,
                ["receipt"]         = receipt,
                ["payment_capture"] = 1,
                ["notes"] = new Dictionary<string, string>
                {
                    ["user_id"]          = userId.ToString(),
                    ["plan_code"]        = plan.Code,
                    ["payment_order_id"] = orderId.ToString(),
                }
            };
            msg.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var res = await client.SendAsync(msg, ct);
            var body = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError("Razorpay order create failed: {Status} {Body}", res.StatusCode, body);
                po.Status = "Failed";
                po.FailureReason = $"Razorpay {(int)res.StatusCode}: {Truncate(body, 480)}";
                po.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync(ct);
                var friendly = TryExtractRazorpayError(body) ?? "Could not initiate payment. Please try again.";
                return StatusCode(502, new CreateOrderResponse(false, friendly));
            }

            using var json = JsonDocument.Parse(body);
            var rzpOrderId = json.RootElement.GetProperty("id").GetString();
            if (string.IsNullOrWhiteSpace(rzpOrderId))
            {
                po.Status = "Failed";
                po.FailureReason = "Razorpay returned no order id.";
                po.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync(ct);
                return StatusCode(502, new CreateOrderResponse(false, "Could not initiate payment. Please try again."));
            }

            po.RazorpayOrderId = rzpOrderId;
            po.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync(ct);

            return Ok(new CreateOrderResponse(
                true,
                "Order created.",
                _rzp.KeyId,
                po.PaymentOrderId,
                po.RazorpayOrderId,
                po.TotalAmountPaise,
                po.Currency,
                plan.Code,
                plan.Name,
                user.FullName,
                user.Email,
                user.Phone
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating Razorpay order.");
            po.Status = "Failed";
            po.FailureReason = Truncate(ex.Message, 480);
            po.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync(ct);
            return StatusCode(500, new CreateOrderResponse(false, "Could not initiate payment. Please try again."));
        }
    }

    [Authorize(Policy = "MemberAccess")]
    [HttpPost("razorpay/verify")]
    public async Task<ActionResult<VerifyResponse>> Verify([FromBody] VerifyRequest? req, CancellationToken ct)
    {
        var userId = CurrentUser.RequireUserId(User);
        if (req is null
            || req.PaymentOrderId == Guid.Empty
            || string.IsNullOrWhiteSpace(req.RazorpayOrderId)
            || string.IsNullOrWhiteSpace(req.RazorpayPaymentId)
            || string.IsNullOrWhiteSpace(req.RazorpaySignature))
            return BadRequest(new VerifyResponse(false, "Missing required fields."));

        var po = await _db.PaymentOrders.FirstOrDefaultAsync(p => p.PaymentOrderId == req.PaymentOrderId, ct);
        if (po is null) return NotFound(new VerifyResponse(false, "Order not found."));
        if (po.UserId != userId) return Unauthorized(new VerifyResponse(false, "Order does not belong to this user."));
        if (!string.Equals(po.RazorpayOrderId, req.RazorpayOrderId, StringComparison.Ordinal))
            return BadRequest(new VerifyResponse(false, "Order id mismatch."));

        var expected = HmacSha256Hex($"{req.RazorpayOrderId}|{req.RazorpayPaymentId}", _rzp.KeySecret);
        if (!FixedTimeEqualsCaseInsensitive(expected, req.RazorpaySignature))
        {
            po.Status = "Failed";
            po.FailureReason = "Signature mismatch.";
            po.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync(ct);
            return BadRequest(new VerifyResponse(false, "Payment verification failed."));
        }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var result = await _activation.ActivatePaidOrderAsync(
                po,
                req.RazorpayOrderId,
                req.RazorpayPaymentId,
                req.RazorpaySignature,
                rawWebhookPayload: null,
                ct);
            await tx.CommitAsync(ct);

            await TryCreateReferralLeadAsync(po.PaymentOrderId, ct);

            return Ok(new VerifyResponse(
                true,
                result.Activated ? "Payment verified." : "Payment already verified.",
                result.PlanCode,
                result.ActiveFrom,
                result.ActiveTo,
                result.Kind == ActivationKind.None ? null : result.Kind.ToString(),
                result.PreviousPlanName));
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            _logger.LogError(ex, "Error finalizing verified payment.");
            return StatusCode(500, new VerifyResponse(false, "Payment captured but activation failed. We will reconcile shortly."));
        }
    }

    [Authorize(Policy = "MemberAccess")]
    [HttpGet("my-plan")]
    public async Task<ActionResult<MyPlanResponse>> MyPlan(CancellationToken ct)
    {
        var userId = CurrentUser.RequireUserId(User);
        var row = await _activation.GetActivePlanRowAsync(userId, ct);
        if (row is null) return Ok(new MyPlanResponse(true, "No active plan.", null));
        return Ok(new MyPlanResponse(true, "OK", ToDto(row)));
    }

    [Authorize(Policy = "MemberAccess")]
    [HttpPost("subscription/cancel")]
    public async Task<ActionResult<CancelSubscriptionResponse>> CancelSubscription(CancellationToken ct)
    {
        var userId = CurrentUser.RequireUserId(User);
        var now = DateTime.Now;

        var up = await _db.UserPlans.FirstOrDefaultAsync(p =>
            p.UserId == userId
            && p.Status == "Active"
            && (p.ActiveTo == null || p.ActiveTo > now), ct);

        if (up is null)
            return NotFound(new CancelSubscriptionResponse(false, "No active subscription to cancel."));

        if (up.CancelAtPeriodEnd)
            return Ok(new CancelSubscriptionResponse(true, "Renewal is already cancelled. Access continues until your plan end date.", up.ActiveTo));

        up.CancelAtPeriodEnd = true;
        up.CancelledAt = now;
        up.UpdatedAt = now;
        await _db.SaveChangesAsync(ct);

        return Ok(new CancelSubscriptionResponse(
            true,
            "Renewal cancelled. You keep access until your current period ends. No refund is issued for the remaining term.",
            up.ActiveTo));
    }

    [Authorize(Policy = "MemberAccess")]
    [HttpGet("subscription/history")]
    public async Task<ActionResult<PaymentHistoryResponse>> PaymentHistory(CancellationToken ct)
    {
        var userId = CurrentUser.RequireUserId(User);

        var items = await (
            from po in _db.PaymentOrders.AsNoTracking()
            join p in _db.Plans.AsNoTracking() on po.PlanId equals p.PlanId
            join pay in _db.Payments.AsNoTracking() on po.PaymentOrderId equals pay.PaymentOrderId into pays
            from pay in pays.DefaultIfEmpty()
            where po.UserId == userId
            orderby po.CreatedAt descending
            select new PaymentHistoryItemDto(
                po.PaymentOrderId,
                po.PlanCode,
                p.Name,
                po.TotalAmountPaise,
                po.Status,
                pay != null ? pay.Status : null,
                po.CreatedAt,
                pay != null ? pay.PaidAt : (DateTime?)null)
        ).Take(50).ToListAsync(ct);

        return Ok(new PaymentHistoryResponse(true, "OK", items));
    }

    [Authorize(Policy = "MemberAccess")]
    [HttpGet("invoices")]
    public async Task<ActionResult<InvoiceListResponse>> ListInvoices(CancellationToken ct)
    {
        var userId = CurrentUser.RequireUserId(User);

        var rows = await (
            from pay in _db.Payments.AsNoTracking()
            join po in _db.PaymentOrders.AsNoTracking() on pay.PaymentOrderId equals po.PaymentOrderId
            join p in _db.Plans.AsNoTracking() on po.PlanId equals p.PlanId
            join up in _db.UserPlans.AsNoTracking() on po.PaymentOrderId equals up.PaymentOrderId into ups
            from up in ups.DefaultIfEmpty()
            where po.UserId == userId && pay.Status == "Captured"
            orderby pay.PaidAt descending
            select new
            {
                pay.PaymentId,
                pay.PaidAt,
                po.TotalAmountPaise,
                p.Name,
                po.PlanCode,
                ActiveTo = up != null ? up.ActiveTo : (DateTime?)null,
            }
        ).Take(50).ToListAsync(ct);

        var items = rows.Select(r => new InvoiceListItemDto(
            r.PaymentId,
            InvoiceNumber.ForPayment(r.PaymentId, r.PaidAt),
            r.PlanCode,
            r.Name,
            r.TotalAmountPaise,
            r.PaidAt,
            r.ActiveTo)).ToList();

        return Ok(new InvoiceListResponse(true, "OK", items));
    }

    [Authorize(Policy = "MemberAccess")]
    [HttpGet("invoices/{paymentId:guid}/download")]
    public async Task<IActionResult> DownloadInvoice(Guid paymentId, CancellationToken ct)
    {
        var userId = CurrentUser.RequireUserId(User);

        var row = await (
            from pay in _db.Payments.AsNoTracking()
            join po in _db.PaymentOrders.AsNoTracking() on pay.PaymentOrderId equals po.PaymentOrderId
            join p in _db.Plans.AsNoTracking() on po.PlanId equals p.PlanId
            join u in _db.Users.AsNoTracking() on po.UserId equals u.UserId
            join up in _db.UserPlans.AsNoTracking() on po.PaymentOrderId equals up.PaymentOrderId into ups
            from up in ups.DefaultIfEmpty()
            where pay.PaymentId == paymentId && po.UserId == userId && pay.Status == "Captured"
            select new
            {
                Payment = pay,
                Order = po,
                Plan = p,
                User = u,
                up.ActiveFrom,
                up.ActiveTo,
            }
        ).FirstOrDefaultAsync(ct);

        if (row is null) return NotFound();

        var activeFrom = row.ActiveFrom != default ? row.ActiveFrom : row.Payment.PaidAt;
        var pdf = _invoices.Generate(row.Payment, row.Order, row.Plan, row.User, activeFrom, row.ActiveTo);
        var fileName = $"{InvoiceNumber.ForPayment(row.Payment.PaymentId, row.Payment.PaidAt)}.pdf";
        return File(pdf, "application/pdf", fileName);
    }

    [Authorize(Policy = "MemberAccess")]
    [HttpPost("razorpay/cancel/{paymentOrderId:guid}")]
    public async Task<IActionResult> CancelCheckout(Guid paymentOrderId, CancellationToken ct)
    {
        var userId = CurrentUser.RequireUserId(User);
        var po = await _db.PaymentOrders.FirstOrDefaultAsync(p => p.PaymentOrderId == paymentOrderId, ct);
        if (po is null) return NotFound();
        if (po.UserId != userId) return Unauthorized();
        if (po.Status == "Created")
        {
            po.Status = "Cancelled";
            po.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync(ct);
        }
        return Ok();
    }

    [AllowAnonymous]
    [HttpPost("razorpay/webhook")]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync(ct);
        Request.Body.Position = 0;

        if (string.IsNullOrWhiteSpace(_rzp.WebhookSecret))
        {
            _logger.LogWarning("Razorpay webhook received but WebhookSecret is not configured.");
            return Ok(new { ignored = true, reason = "WebhookSecret not configured" });
        }

        var signature = Request.Headers["X-Razorpay-Signature"].ToString();
        var expected = HmacSha256Hex(body, _rzp.WebhookSecret);
        if (!FixedTimeEqualsCaseInsensitive(expected, signature))
        {
            _logger.LogWarning("Razorpay webhook signature mismatch.");
            return Unauthorized();
        }

        try
        {
            using var json = JsonDocument.Parse(body);
            var evt = json.RootElement.GetProperty("event").GetString();

            if (evt is "payment.captured" or "order.paid")
            {
                var paymentEntity = json.RootElement
                    .GetProperty("payload").GetProperty("payment").GetProperty("entity");
                var rzpOrderId   = paymentEntity.GetProperty("order_id").GetString() ?? "";
                var rzpPaymentId = paymentEntity.GetProperty("id").GetString() ?? "";

                var po = await _db.PaymentOrders.FirstOrDefaultAsync(p => p.RazorpayOrderId == rzpOrderId, ct);
                if (po is not null && po.Status != "Paid")
                {
                    ActivationResult activationResult;
                    await using (var tx = await _db.Database.BeginTransactionAsync(ct))
                    {
                        activationResult = await _activation.ActivatePaidOrderAsync(
                            po, rzpOrderId, rzpPaymentId, razorpaySignature: null,
                            rawWebhookPayload: Truncate(body, 4000), ct);
                        await tx.CommitAsync(ct);
                    }

                    await TryCreateReferralLeadAsync(po.PaymentOrderId, ct);
                    _logger.LogInformation("Webhook activated plan for order {OrderId}.", po.PaymentOrderId);
                }
                else if (po is not null && po.Status == "Paid")
                {
                    await TryCreateReferralLeadAsync(po.PaymentOrderId, ct);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Razorpay webhook.");
        }

        return Ok();
    }

    private async Task TryCreateReferralLeadAsync(Guid paymentOrderId, CancellationToken ct)
    {
        try
        {
            await _leadPush.CreateLeadAfterPaymentAsync(paymentOrderId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lead insert into lead_data failed for order {PaymentOrderId}.", paymentOrderId);
        }
    }

    private static ActivePlanDto ToDto(PaymentActivationService.ActivePlanRow row)
    {
        int? days = null;
        if (row.ActiveTo is not null)
            days = (int)Math.Ceiling((row.ActiveTo.Value - DateTime.Now).TotalDays);

        return new ActivePlanDto(
            row.PlanCode,
            row.PlanName,
            row.ActiveFrom,
            row.ActiveTo,
            row.Status,
            row.CancelAtPeriodEnd,
            row.AutoRenewEnabled,
            days);
    }

    private static string HmacSha256Hex(string payload, string secret)
    {
        using var h = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var bytes = h.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes) sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
        return sb.ToString();
    }

    private static bool FixedTimeEqualsCaseInsensitive(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b) || a.Length != b.Length) return false;
        var ab = Encoding.ASCII.GetBytes(a.ToLowerInvariant());
        var bb = Encoding.ASCII.GetBytes(b.ToLowerInvariant());
        return CryptographicOperations.FixedTimeEquals(ab, bb);
    }

    private static string Truncate(string s, int max)
        => string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s[..max]);

    private static string? TryExtractRazorpayError(string body)
    {
        try
        {
            using var json = JsonDocument.Parse(body);
            if (!json.RootElement.TryGetProperty("error", out var err) || err.ValueKind != JsonValueKind.Object)
                return null;
            if (err.TryGetProperty("description", out var d) && d.ValueKind == JsonValueKind.String)
            {
                var s = d.GetString();
                return string.IsNullOrWhiteSpace(s) ? null : s;
            }
            return null;
        }
        catch
        {
            return null;
        }
    }
}
