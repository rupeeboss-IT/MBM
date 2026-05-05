using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RB_Website_API.Auth;
using RB_Website_API.Data;
using RB_Website_API.Models;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/payment")]
public sealed class PaymentController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly RazorpaySettings _rzp;
    private readonly IHttpClientFactory _http;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        AppDbContext db,
        IOptions<RazorpaySettings> rzp,
        IHttpClientFactory http,
        ILogger<PaymentController> logger)
    {
        _db = db;
        _rzp = rzp.Value;
        _http = http;
        _logger = logger;
    }

    // -------- DTOs --------
    public sealed record CreateOrderRequest(Guid UserId, string PlanCode);

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
        Guid UserId,
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
        DateTime? ActiveTo = null
    );

    public sealed record ActivePlanDto(
        string PlanCode,
        string PlanName,
        DateTime ActiveFrom,
        DateTime? ActiveTo,
        string Status
    );

    public sealed record MyPlanResponse(bool Success, string? Message = null, ActivePlanDto? Plan = null);

    // -------- 1) Create order --------
    [HttpPost("razorpay/order")]
    public async Task<ActionResult<CreateOrderResponse>> CreateOrder([FromBody] CreateOrderRequest? req, CancellationToken ct)
    {
        if (req is null) return BadRequest(new CreateOrderResponse(false, "Request is required."));
        if (req.UserId == Guid.Empty) return BadRequest(new CreateOrderResponse(false, "Login required."));
        if (string.IsNullOrWhiteSpace(req.PlanCode)) return BadRequest(new CreateOrderResponse(false, "Plan code is required."));

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == req.UserId, ct);
        if (user is null) return Unauthorized(new CreateOrderResponse(false, "User not found. Please login again."));

        var planCode = req.PlanCode.Trim().ToLowerInvariant();
        var plan = await _db.Plans.AsNoTracking().FirstOrDefaultAsync(p => p.Code == planCode && p.IsActive, ct);
        if (plan is null) return NotFound(new CreateOrderResponse(false, "Plan not found."));

        // Block duplicate purchase of the currently active plan
        var existingActive = await _db.UserPlans.AsNoTracking()
            .FirstOrDefaultAsync(up => up.UserId == user.UserId && up.Status == "Active", ct);
        if (existingActive is not null && string.Equals(existingActive.PlanCode, plan.Code, StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new CreateOrderResponse(false, $"You already have {plan.Name} active."));
        }

        if (string.IsNullOrWhiteSpace(_rzp.KeyId) || string.IsNullOrWhiteSpace(_rzp.KeySecret))
        {
            _logger.LogError("Razorpay credentials are missing in configuration.");
            return StatusCode(500, new CreateOrderResponse(false, "Payment is not configured. Please try later."));
        }

        var orderId = Guid.NewGuid();
        var receipt = $"mbm_{orderId.ToString("N").Substring(0, 16)}";

        var po = new PaymentOrder
        {
            PaymentOrderId   = orderId,
            UserId           = user.UserId,
            PlanId           = plan.PlanId,
            PlanCode         = plan.Code,
            BaseAmountPaise  = plan.BaseAmountPaise,
            GstPaise         = plan.GstPaise,
            TotalAmountPaise = plan.TotalAmountPaise,
            Currency         = plan.Currency,
            Provider         = "Razorpay",
            Receipt          = receipt,
            Status           = "Created",
            CreatedAt        = DateTime.UtcNow,
            UpdatedAt        = DateTime.UtcNow,
        };
        _db.PaymentOrders.Add(po);
        await _db.SaveChangesAsync(ct);

        // Call Razorpay Orders API
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
                    ["user_id"]          = user.UserId.ToString(),
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
                po.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
                var friendly = TryExtractRazorpayError(body) ?? "Could not initiate payment. Please try again.";
                if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    friendly = "Razorpay authentication failed. Please check KeyId/KeySecret in backend configuration.";
                return StatusCode(502, new CreateOrderResponse(false, friendly));
            }

            using var json = JsonDocument.Parse(body);
            var rzpOrderId = json.RootElement.GetProperty("id").GetString();
            if (string.IsNullOrWhiteSpace(rzpOrderId))
            {
                po.Status = "Failed";
                po.FailureReason = "Razorpay returned no order id.";
                po.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
                return StatusCode(502, new CreateOrderResponse(false, "Could not initiate payment. Please try again."));
            }

            po.RazorpayOrderId = rzpOrderId;
            po.UpdatedAt = DateTime.UtcNow;
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
            po.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return StatusCode(500, new CreateOrderResponse(false, "Could not initiate payment. Please try again."));
        }
    }

    // -------- 2) Verify signature & activate plan --------
    [HttpPost("razorpay/verify")]
    public async Task<ActionResult<VerifyResponse>> Verify([FromBody] VerifyRequest? req, CancellationToken ct)
    {
        if (req is null
            || req.UserId == Guid.Empty
            || req.PaymentOrderId == Guid.Empty
            || string.IsNullOrWhiteSpace(req.RazorpayOrderId)
            || string.IsNullOrWhiteSpace(req.RazorpayPaymentId)
            || string.IsNullOrWhiteSpace(req.RazorpaySignature))
        {
            return BadRequest(new VerifyResponse(false, "Missing required fields."));
        }

        var po = await _db.PaymentOrders.FirstOrDefaultAsync(p => p.PaymentOrderId == req.PaymentOrderId, ct);
        if (po is null) return NotFound(new VerifyResponse(false, "Order not found."));
        if (po.UserId != req.UserId) return Unauthorized(new VerifyResponse(false, "Order does not belong to this user."));
        if (!string.Equals(po.RazorpayOrderId, req.RazorpayOrderId, StringComparison.Ordinal))
            return BadRequest(new VerifyResponse(false, "Order id mismatch."));

        // Verify HMAC SHA256(razorpay_order_id|razorpay_payment_id, key_secret)
        var expected = HmacSha256Hex($"{req.RazorpayOrderId}|{req.RazorpayPaymentId}", _rzp.KeySecret);
        if (!FixedTimeEqualsCaseInsensitive(expected, req.RazorpaySignature))
        {
            po.Status = "Failed";
            po.FailureReason = "Signature mismatch.";
            po.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return BadRequest(new VerifyResponse(false, "Payment verification failed."));
        }

        // Already verified previously? (idempotent)
        var alreadyPaid = await _db.Payments.AnyAsync(p => p.RazorpayPaymentId == req.RazorpayPaymentId, ct);
        var plan = await _db.Plans.AsNoTracking().FirstOrDefaultAsync(p => p.PlanId == po.PlanId, ct);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            if (!alreadyPaid)
            {
                _db.Payments.Add(new Payment
                {
                    PaymentId         = Guid.NewGuid(),
                    PaymentOrderId    = po.PaymentOrderId,
                    RazorpayOrderId   = req.RazorpayOrderId,
                    RazorpayPaymentId = req.RazorpayPaymentId,
                    RazorpaySignature = req.RazorpaySignature,
                    AmountPaise       = po.TotalAmountPaise,
                    Currency          = po.Currency,
                    Status            = "Captured",
                    PaidAt            = DateTime.UtcNow,
                    CreatedAt         = DateTime.UtcNow,
                });
            }

            po.Status = "Paid";
            po.UpdatedAt = DateTime.UtcNow;

            // Expire any other active plan for the user, then activate this one
            var others = await _db.UserPlans.Where(up => up.UserId == po.UserId && up.Status == "Active").ToListAsync(ct);
            foreach (var up in others)
            {
                up.Status = "Cancelled";
                up.ActiveTo = DateTime.UtcNow;
                up.UpdatedAt = DateTime.UtcNow;
            }

            var activeFrom = DateTime.UtcNow;
            var activeTo = activeFrom.AddDays(plan?.DurationDays ?? 365);
            var newUp = new UserPlan
            {
                UserPlanId     = Guid.NewGuid(),
                UserId         = po.UserId,
                PlanId         = po.PlanId,
                PlanCode       = po.PlanCode,
                PaymentOrderId = po.PaymentOrderId,
                ActiveFrom     = activeFrom,
                ActiveTo       = activeTo,
                Status         = "Active",
                CreatedAt      = activeFrom,
                UpdatedAt      = activeFrom,
            };
            _db.UserPlans.Add(newUp);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return Ok(new VerifyResponse(true, "Payment verified.", po.PlanCode, activeFrom, activeTo));
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            _logger.LogError(ex, "Error finalizing verified payment.");
            return StatusCode(500, new VerifyResponse(false, "Payment captured but activation failed. We will reconcile shortly."));
        }
    }

    // -------- 3) Get current user's active plan --------
    [HttpGet("my-plan")]
    public async Task<ActionResult<MyPlanResponse>> MyPlan([FromQuery] Guid userId, CancellationToken ct)
    {
        if (userId == Guid.Empty) return BadRequest(new MyPlanResponse(false, "userId is required."));

        var row = await (
            from up in _db.UserPlans.AsNoTracking()
            join p in _db.Plans.AsNoTracking() on up.PlanId equals p.PlanId
            where up.UserId == userId && up.Status == "Active"
            orderby up.ActiveFrom descending
            select new ActivePlanDto(up.PlanCode, p.Name, up.ActiveFrom, up.ActiveTo, up.Status)
        ).FirstOrDefaultAsync(ct);

        if (row is null) return Ok(new MyPlanResponse(true, "No active plan.", null));
        return Ok(new MyPlanResponse(true, "OK", row));
    }

    // -------- 4) Cancel an unpaid order (user closed checkout) --------
    [HttpPost("razorpay/cancel/{paymentOrderId:guid}")]
    public async Task<IActionResult> Cancel(Guid paymentOrderId, [FromQuery] Guid userId, CancellationToken ct)
    {
        var po = await _db.PaymentOrders.FirstOrDefaultAsync(p => p.PaymentOrderId == paymentOrderId, ct);
        if (po is null) return NotFound();
        if (po.UserId != userId) return Unauthorized();
        if (po.Status == "Created")
        {
            po.Status = "Cancelled";
            po.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
        return Ok();
    }

    // -------- 5) Webhook (optional reconciliation) --------
    [HttpPost("razorpay/webhook")]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync(ct);
        Request.Body.Position = 0;

        if (string.IsNullOrWhiteSpace(_rzp.WebhookSecret))
        {
            _logger.LogWarning("Razorpay webhook received but WebhookSecret is not configured. Ignoring.");
            return Ok();
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

            if (evt == "payment.captured" || evt == "order.paid")
            {
                var paymentEntity = json.RootElement
                    .GetProperty("payload").GetProperty("payment").GetProperty("entity");
                var rzpOrderId   = paymentEntity.GetProperty("order_id").GetString() ?? "";
                var rzpPaymentId = paymentEntity.GetProperty("id").GetString() ?? "";

                var po = await _db.PaymentOrders.FirstOrDefaultAsync(p => p.RazorpayOrderId == rzpOrderId, ct);
                if (po is not null && po.Status != "Paid")
                {
                    var alreadyPaid = await _db.Payments.AnyAsync(p => p.RazorpayPaymentId == rzpPaymentId, ct);
                    if (!alreadyPaid)
                    {
                        _db.Payments.Add(new Payment
                        {
                            PaymentId         = Guid.NewGuid(),
                            PaymentOrderId    = po.PaymentOrderId,
                            RazorpayOrderId   = rzpOrderId,
                            RazorpayPaymentId = rzpPaymentId,
                            AmountPaise       = po.TotalAmountPaise,
                            Currency          = po.Currency,
                            Status            = "Captured",
                            RawPayload        = Truncate(body, 4000),
                            PaidAt            = DateTime.UtcNow,
                            CreatedAt         = DateTime.UtcNow,
                        });
                    }
                    po.Status = "Paid";
                    po.UpdatedAt = DateTime.UtcNow;

                    // Activate plan if not already
                    var hasActive = await _db.UserPlans.AnyAsync(
                        up => up.UserId == po.UserId && up.PaymentOrderId == po.PaymentOrderId && up.Status == "Active", ct);
                    if (!hasActive)
                    {
                        var plan = await _db.Plans.AsNoTracking().FirstOrDefaultAsync(p => p.PlanId == po.PlanId, ct);
                        var others = await _db.UserPlans.Where(up => up.UserId == po.UserId && up.Status == "Active").ToListAsync(ct);
                        foreach (var up in others)
                        {
                            up.Status = "Cancelled";
                            up.ActiveTo = DateTime.UtcNow;
                            up.UpdatedAt = DateTime.UtcNow;
                        }
                        var activeFrom = DateTime.UtcNow;
                        _db.UserPlans.Add(new UserPlan
                        {
                            UserPlanId     = Guid.NewGuid(),
                            UserId         = po.UserId,
                            PlanId         = po.PlanId,
                            PlanCode       = po.PlanCode,
                            PaymentOrderId = po.PaymentOrderId,
                            ActiveFrom     = activeFrom,
                            ActiveTo       = activeFrom.AddDays(plan?.DurationDays ?? 365),
                            Status         = "Active",
                            CreatedAt      = activeFrom,
                            UpdatedAt      = activeFrom,
                        });
                    }
                    await _db.SaveChangesAsync(ct);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Razorpay webhook.");
            // Do not fail the webhook with 500; respond 200 to avoid retries flood.
        }
        return Ok();
    }

    // -------- helpers --------
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
        => string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s.Substring(0, max));

    private static string? TryExtractRazorpayError(string body)
    {
        try
        {
            using var json = JsonDocument.Parse(body);
            if (!json.RootElement.TryGetProperty("error", out var err)) return null;
            if (err.ValueKind != JsonValueKind.Object) return null;
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
