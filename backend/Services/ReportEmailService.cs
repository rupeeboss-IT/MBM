using System.Globalization;
using System.Text;
using Microsoft.Extensions.Options;
using RB_Website_API.Auth;
using RB_Website_API.Models;

namespace RB_Website_API.Services;

public sealed class ReportEmailService : IReportEmailService
{
    private const string TemplateFileName = "ReportReadyEmail.html";
    private readonly IEmailSender _email;
    private readonly ApplicationUrlsSettings _urls;
    private readonly InvoiceSettings _invoiceSettings;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ReportEmailService> _logger;

    public ReportEmailService(
        IEmailSender email,
        IOptions<ApplicationUrlsSettings> urls,
        IOptions<InvoiceSettings> invoiceSettings,
        IWebHostEnvironment env,
        ILogger<ReportEmailService> logger)
    {
        _email = email;
        _urls = urls.Value;
        _invoiceSettings = invoiceSettings.Value;
        _env = env;
        _logger = logger;
    }

    public async Task SendReportReadyEmailAsync(User customer, CustomerReport report, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(customer.Email))
        {
            _logger.LogWarning("Report ready email skipped: customer {CustomerId} has no email.", customer.UserId);
            return;
        }

        var subject = "Your Reports Are Ready";
        var body = BuildBody(
            customer.FullName,
            report.MemberId,
            report.UploadDate,
            _urls.ProfileReportsUrl,
            _invoiceSettings.SupportEmail);

        await _email.SendAsync(customer.Email, subject, body, isHtml: true, attachments: null, ct);
        _logger.LogInformation("Report ready email sent to {Email} for report {ReportId}.", customer.Email, report.Id);
    }

    public string BuildBody(
        string customerName,
        string memberId,
        DateTime reportGeneratedUtc,
        string reportUrl,
        string? supportEmail = null)
    {
        var template = LoadTemplate();
        var dateText = reportGeneratedUtc.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
        var support = string.IsNullOrWhiteSpace(supportEmail)
            ? _invoiceSettings.SupportEmail
            : supportEmail.Trim();

        return template
            .Replace("{{CustomerName}}", Encode(customerName))
            .Replace("{{MemberId}}", Encode(memberId))
            .Replace("{{ReportGeneratedDate}}", Encode(dateText))
            .Replace("{{ReportUrl}}", Encode(reportUrl))
            .Replace("{{SupportEmail}}", Encode(support))
            .Replace("{{Year}}", DateTime.Now.Year.ToString(CultureInfo.InvariantCulture));
    }

    private string LoadTemplate()
    {
        var path = Path.Combine(_env.ContentRootPath, "Templates", TemplateFileName);
        if (File.Exists(path))
            return File.ReadAllText(path, Encoding.UTF8);

        _logger.LogWarning("Email template not found at {Path}; using built-in fallback.", path);
        return GetEmbeddedFallbackTemplate();
    }

    private static string Encode(string? value) => System.Net.WebUtility.HtmlEncode(value ?? "");

    /// <summary>Minimal fallback if Templates/ReportReadyEmail.html is missing from deployment.</summary>
    private static string GetEmbeddedFallbackTemplate() =>
        """
        <!DOCTYPE html><html><body style="font-family:Segoe UI,Arial,sans-serif;background:#eef1f6;padding:24px;">
        <table width="600" align="center" style="background:#fff;border-radius:12px;padding:32px;">
        <tr><td><h1 style="color:#1a1a2e;">Your Reports Are Ready</h1>
        <p>Dear {{CustomerName}},</p>
        <p>Member ID: <strong>{{MemberId}}</strong><br/>Generated: {{ReportGeneratedDate}}</p>
        <p>Your reports are available in your account. <a href="{{ReportUrl}}" style="color:#e63946;">Login &amp; Download Reports</a></p>
        <p style="font-size:12px;color:#64748b;">&copy; {{Year}} MSME Bharat Manch</p></td></tr></table></body></html>
        """;

}
