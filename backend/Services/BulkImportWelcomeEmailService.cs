using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using RB_Website_API.Auth;

namespace RB_Website_API.Services;

public sealed class BulkImportWelcomeEmailService
{
    private const string TemplateFile = "BulkImportWelcomeEmail.html";
    private const string Subject = "Welcome to MSME Bharat Manch – Your Member ID is Ready";
    private const string DefaultPortalUrl = "https://msmebharatmanch.com/register?mode=free";

    private readonly IEmailSender _email;
    private readonly ApplicationUrlsSettings _urls;
    private readonly ContactSettings _contact;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<BulkImportWelcomeEmailService> _logger;

    public BulkImportWelcomeEmailService(
        IEmailSender email,
        IOptions<ApplicationUrlsSettings> urls,
        IOptions<ContactSettings> contact,
        IWebHostEnvironment env,
        ILogger<BulkImportWelcomeEmailService> logger)
    {
        _email = email;
        _urls = urls.Value;
        _contact = contact.Value;
        _env = env;
        _logger = logger;
    }

    public async Task<bool> TrySendAsync(
        string toEmail,
        string customerName,
        string memberId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(toEmail)) return false;

        try
        {
            var body = BuildBody(customerName, memberId);
            await _email.SendAsync(toEmail.Trim(), Subject, body, isHtml: true, attachments: null, ct);
            _logger.LogInformation("Bulk import welcome email sent to {Email} for member {MemberId}.", toEmail, memberId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk import welcome email failed for {Email}, member {MemberId}.", toEmail, memberId);
            return false;
        }
    }

    private string BuildBody(string customerName, string memberId)
    {
        var portalUrl = string.IsNullOrWhiteSpace(_urls.FrontendBase)
            ? DefaultPortalUrl
            : $"{_urls.FrontendBase}/register?mode=free";

        return LoadTemplate()
            .Replace("{{CustomerName}}", Encode(customerName))
            .Replace("{{MemberID}}", Encode(memberId))
            .Replace("{{LoginUrl}}", Encode(portalUrl))
            .Replace("{{SupportEmail}}", Encode(_contact.FromEmail))
            .Replace("{{SupportPhone}}", Encode(_contact.SupportPhone))
            .Replace("{{Year}}", DateTime.Now.Year.ToString(CultureInfo.InvariantCulture));
    }

    private string LoadTemplate()
    {
        var path = Path.Combine(_env.ContentRootPath, "Templates", TemplateFile);
        if (File.Exists(path))
            return File.ReadAllText(path, Encoding.UTF8);

        _logger.LogWarning("Bulk import welcome template not found at {Path}.", path);
        return """
            <p>Hello {{CustomerName}},</p>
            <p>Your Member ID is <strong>{{MemberID}}</strong>.</p>
            <p><a href="{{LoginUrl}}">Go to Login</a> and use Forgot Password to create your password.</p>
            <p>— MSME Bharat Manch Team</p>
            """;
    }

    private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? "");
}
