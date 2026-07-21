using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RB_Website_API.Auth;
using RB_Website_API.Data;

namespace RB_Website_API.Services;

public sealed class MembershipEmailService
{
    private readonly AppDbContext _db;
    private readonly IEmailSender _email;
    private readonly InvoicePdfService _invoices;
    private readonly IPlanBenefitsService _planBenefits;
    private readonly InvoiceSettings _settings;
    private readonly ApplicationUrlsSettings _urls;
    private readonly ILogger<MembershipEmailService> _logger;

    public MembershipEmailService(
        AppDbContext db,
        IEmailSender email,
        InvoicePdfService invoices,
        IPlanBenefitsService planBenefits,
        IOptions<InvoiceSettings> settings,
        IOptions<ApplicationUrlsSettings> urls,
        ILogger<MembershipEmailService> logger)
    {
        _db = db;
        _email = email;
        _invoices = invoices;
        _planBenefits = planBenefits;
        _settings = settings.Value;
        _urls = urls.Value;
        _logger = logger;
    }

    public async Task SendActivationEmailAsync(ActivationResult result, CancellationToken ct)
    {
        if (!result.Activated || result.PaymentId is null || result.Kind is ActivationKind.None or ActivationKind.OneTimeReport)
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
            var benefits = await _planBenefits.GetBenefitTextsAsync(plan.Code, ct);
            var pdf = _invoices.Generate(payment, order, plan, user, activeFrom, activeTo, benefits);

            var subject = result.Kind switch
            {
                ActivationKind.Renewal => $"Renewed — {plan.Name} extended until {FormatDate(activeTo)}",
                ActivationKind.Upgrade => $"Upgraded — {result.PreviousPlanName ?? "Previous plan"} → {plan.Name}",
                _ => $"Welcome — {plan.Name} activated",
            };

            var body = await BuildHtmlBodyAsync(user.FullName, plan.Name, plan.Code, result, activeFrom, activeTo, ct);
            var attachment = new EmailAttachment($"{invoiceNo}.pdf", pdf, "application/pdf");

            await _email.SendAsync(user.Email, subject, body, isHtml: true, [attachment], ct);
            _logger.LogInformation("Membership email sent to {Email} for payment {PaymentId}.", user.Email, payment.PaymentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send membership email for payment {PaymentId}.", result.PaymentId);
        }
    }

    private async Task<string> BuildHtmlBodyAsync(
        string fullName,
        string planName,
        string planCode,
        ActivationResult result,
        DateTime activeFrom,
        DateTime? activeTo,
        CancellationToken ct)
    {
        var benefits = await _planBenefits.GetBenefitTextsAsync(planCode, ct);
        var sb = new StringBuilder();
        sb.Append("<html><body style=\"font-family:Arial,sans-serif;color:#222;\">");
        sb.Append($"<p>Hello {System.Net.WebUtility.HtmlEncode(fullName)},</p>");

        sb.Append(result.Kind switch
        {
            ActivationKind.Renewal =>
                $"<p>Your <strong>{System.Net.WebUtility.HtmlEncode(planName)}</strong> membership has been <strong>renewed</strong>.</p>",
            ActivationKind.Upgrade =>
                $"<p>Your plan has been <strong>upgraded</strong> from <strong>{System.Net.WebUtility.HtmlEncode(result.PreviousPlanName ?? "your previous plan")}</strong> to <strong>{System.Net.WebUtility.HtmlEncode(planName)}</strong>.</p>" +
                "<p>Your previous plan ended today. The full price of the new plan was charged; unused time on the old plan is not refunded.</p>",
            _ =>
                $"<p>Welcome! Your <strong>{System.Net.WebUtility.HtmlEncode(planName)}</strong> membership is now active.</p>",
        });

        sb.Append("<p><strong>Valid from:</strong> ").Append(FormatDate(activeFrom));
        sb.Append("<br/><strong>Valid until:</strong> ").Append(FormatDate(activeTo)).Append("</p>");

        if (benefits.Count > 0)
        {
            sb.Append("<p><strong>Benefits included:</strong></p><ul>");
            foreach (var b in benefits)
                sb.Append("<li>").Append(System.Net.WebUtility.HtmlEncode(b)).Append("</li>");
            sb.Append("</ul>");
        }

        var myPlanUrl = _urls.MyPlanUrl;
        sb.Append($"<p>View your plan anytime: <a href=\"{myPlanUrl}\">{myPlanUrl}</a></p>");
        sb.Append("<p>Your invoice is attached to this email. You can also download it from your profile.</p>");
        sb.Append("<p>— MSME Bharat Manch</p></body></html>");
        return sb.ToString();
    }

    private static string FormatDate(DateTime? value) => AppDateTime.FormatDate(value);
}
