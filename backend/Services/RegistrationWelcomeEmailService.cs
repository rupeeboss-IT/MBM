using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RB_Website_API.Auth;
using RB_Website_API.Data;

namespace RB_Website_API.Services;

/// <summary>
/// Sends registration welcome emails. Unpaid users receive account setup guidance;
/// paid users (first purchase) receive membership confirmation with invoice.
/// </summary>
public sealed class RegistrationWelcomeEmailService
{
    private const string UnpaidTemplateFile = "RegistrationWelcomeUnpaidEmail.html";
    private const string PaidTemplateFile = "RegistrationWelcomePaidEmail.html";

    private readonly AppDbContext _db;
    private readonly IEmailSender _email;
    private readonly InvoicePdfService _invoices;
    private readonly ApplicationUrlsSettings _urls;
    private readonly ContactSettings _contact;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<RegistrationWelcomeEmailService> _logger;

    public RegistrationWelcomeEmailService(
        AppDbContext db,
        IEmailSender email,
        InvoicePdfService invoices,
        IOptions<ApplicationUrlsSettings> urls,
        IOptions<ContactSettings> contact,
        IWebHostEnvironment env,
        ILogger<RegistrationWelcomeEmailService> logger)
    {
        _db = db;
        _email = email;
        _invoices = invoices;
        _urls = urls.Value;
        _contact = contact.Value;
        _env = env;
        _logger = logger;
    }

    /// <summary>
    /// Called after a new account is created. Sends the unpaid welcome when the user has no active plan.
    /// </summary>
    public async Task TrySendAfterRegistrationAsync(Guid userId, CancellationToken ct)
    {
        try
        {
            if (await HasActiveMembershipAsync(userId, ct))
            {
                _logger.LogInformation(
                    "Registration welcome skipped for user {UserId}: active membership already exists.",
                    userId);
                return;
            }

            await SendUnpaidWelcomeAsync(userId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send registration welcome email for user {UserId}.", userId);
        }
    }

    /// <summary>
    /// Called after first membership payment. Sends the paid registration welcome with invoice PDF.
    /// </summary>
    public async Task TrySendPaidWelcomeAfterFirstPurchaseAsync(ActivationResult result, CancellationToken ct)
    {
        if (!result.Activated || result.PaymentId is null || result.Kind is not ActivationKind.FirstPurchase)
            return;

        try
        {
            var payment = await _db.Payments.AsNoTracking()
                .FirstOrDefaultAsync(p => p.PaymentId == result.PaymentId.Value, ct);
            if (payment is null) return;

            var order = await _db.PaymentOrders.AsNoTracking()
                .FirstOrDefaultAsync(o => o.PaymentOrderId == payment.PaymentOrderId, ct);
            if (order is null) return;

            var plan = await _db.Plans.AsNoTracking().FirstOrDefaultAsync(p => p.PlanId == order.PlanId, ct);
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == order.UserId, ct);
            if (plan is null || user is null || string.IsNullOrWhiteSpace(user.Email)) return;

            var activeFrom = result.ActiveFrom ?? payment.PaidAt;
            var activeTo = result.ActiveTo;
            var invoiceNo = InvoiceNumber.ForPayment(payment.PaymentId, payment.PaidAt);
            var pdf = _invoices.Generate(payment, order, plan, user, activeFrom, activeTo);

            var subject = $"Welcome to MSME Bharat Manch — {plan.Name} activated";
            var body = BuildPaidBody(user, plan, activeFrom, activeTo);
            var attachment = new EmailAttachment($"{invoiceNo}.pdf", pdf, "application/pdf");

            await _email.SendAsync(user.Email, subject, body, isHtml: true, [attachment], ct);
            _logger.LogInformation(
                "Paid registration welcome email sent to {Email} for user {UserId}.",
                user.Email,
                user.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send paid registration welcome for payment {PaymentId}.", result.PaymentId);
        }
    }

    private async Task SendUnpaidWelcomeAsync(Guid userId, CancellationToken ct)
    {
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted, ct);
        if (user is null || string.IsNullOrWhiteSpace(user.Email)) return;

        var subject = "Welcome to MSME Bharat Manch — your account is ready";
        var body = BuildUnpaidBody(user);
        await _email.SendAsync(user.Email, subject, body, isHtml: true, attachments: null, ct);
        _logger.LogInformation("Unpaid registration welcome email sent to {Email}.", user.Email);
    }

    private async Task<bool> HasActiveMembershipAsync(Guid userId, CancellationToken ct)
    {
        var now = DateTime.Now;
        return await _db.UserPlans.AsNoTracking().AnyAsync(up =>
            up.UserId == userId
            && up.Status == "Active"
            && (up.ActiveTo == null || up.ActiveTo > now), ct);
    }

    private string BuildUnpaidBody(Models.User user)
    {
        var template = LoadTemplate(UnpaidTemplateFile);
        return template
            .Replace("{{FullName}}", Encode(user.FullName))
            .Replace("{{MemberId}}", Encode(user.MemberId ?? "—"))
            .Replace("{{Email}}", Encode(user.Email))
            .Replace("{{RegisteredAt}}", Encode(AppDateTime.FormatDateTime(user.CreatedAt)))
            .Replace("{{DashboardUrl}}", Encode(_urls.ProfileUrl))
            .Replace("{{MembershipUrl}}", Encode(_urls.MembershipUrl))
            .Replace("{{SupportEmail}}", Encode(_contact.FromEmail))
            .Replace("{{SupportPhone}}", Encode(_contact.SupportPhone))
            .Replace("{{Year}}", DateTime.Now.Year.ToString(CultureInfo.InvariantCulture));
    }

    private string BuildPaidBody(
        Models.User user,
        Models.Plan plan,
        DateTime activeFrom,
        DateTime? activeTo)
    {
        var template = LoadTemplate(PaidTemplateFile);
        return template
            .Replace("{{FullName}}", Encode(user.FullName))
            .Replace("{{MemberId}}", Encode(user.MemberId ?? "—"))
            .Replace("{{PlanName}}", Encode(plan.Name))
            .Replace("{{ActiveFrom}}", Encode(AppDateTime.FormatDate(activeFrom)))
            .Replace("{{ActiveTo}}", Encode(AppDateTime.FormatDate(activeTo)))
            .Replace("{{BenefitsHtml}}", BuildBenefitsHtml(plan.Code))
            .Replace("{{MyPlanUrl}}", Encode(_urls.MyPlanUrl))
            .Replace("{{DashboardUrl}}", Encode(_urls.ProfileUrl))
            .Replace("{{SupportEmail}}", Encode(_contact.FromEmail))
            .Replace("{{SupportPhone}}", Encode(_contact.SupportPhone))
            .Replace("{{Year}}", DateTime.Now.Year.ToString(CultureInfo.InvariantCulture));
    }

    private static string BuildBenefitsHtml(string planCode)
    {
        var benefits = PlanBenefitsCatalog.GetBenefits(planCode);
        if (benefits.Count == 0)
            return "<p style=\"margin:0;font-size:15px;line-height:1.65;color:#334155;\">Your membership benefits are available in your dashboard.</p>";

        var sb = new StringBuilder();
        sb.Append("<ul style=\"margin:12px 0 0;padding-left:20px;color:#334155;\">");
        foreach (var benefit in benefits)
            sb.Append("<li style=\"margin:6px 0;font-size:15px;line-height:1.5;\">").Append(Encode(benefit)).Append("</li>");
        sb.Append("</ul>");
        return sb.ToString();
    }

    private string LoadTemplate(string fileName)
    {
        var path = Path.Combine(_env.ContentRootPath, "Templates", fileName);
        if (File.Exists(path))
            return File.ReadAllText(path, Encoding.UTF8);

        _logger.LogWarning("Registration welcome template not found at {Path}.", path);
        return fileName.Contains("Paid", StringComparison.OrdinalIgnoreCase)
            ? GetPaidFallbackTemplate()
            : GetUnpaidFallbackTemplate();
    }

    private static string GetUnpaidFallbackTemplate() =>
        """
        <p>Dear {{FullName}},</p>
        <p>Welcome to <strong>MSME Bharat Manch</strong>! Your account is ready.</p>
        <p><strong>Member ID:</strong> {{MemberId}}</p>
        <p>Explore membership plans: <a href="{{MembershipUrl}}">{{MembershipUrl}}</a></p>
        <p>Go to dashboard: <a href="{{DashboardUrl}}">{{DashboardUrl}}</a></p>
        <p>— MSME Bharat Manch</p>
        """;

    private static string GetPaidFallbackTemplate() =>
        """
        <p>Dear {{FullName}},</p>
        <p>Welcome! Your <strong>{{PlanName}}</strong> membership is now active.</p>
        <p><strong>Member ID:</strong> {{MemberId}}</p>
        <p><strong>Valid from:</strong> {{ActiveFrom}}<br/><strong>Valid until:</strong> {{ActiveTo}}</p>
        {{BenefitsHtml}}
        <p>View your plan: <a href="{{MyPlanUrl}}">{{MyPlanUrl}}</a></p>
        <p>Your invoice is attached to this email.</p>
        <p>— MSME Bharat Manch</p>
        """;

    private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? "");
}
