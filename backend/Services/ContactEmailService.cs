using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using RB_Website_API.Auth;
using RB_Website_API.Models;

namespace RB_Website_API.Services;

public sealed class ContactEmailService : IContactEmailService
{
    private const string CustomerTemplateFile = "ContactQueryReceivedEmail.html";
    private const string SupportTemplateFile = "ContactSupportNotifyEmail.html";

    private readonly IEmailSender _email;
    private readonly ContactSettings _contact;
    private readonly ApplicationUrlsSettings _urls;
    private readonly EmailSettings _emailSettings;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ContactEmailService> _logger;

    public ContactEmailService(
        IEmailSender email,
        IOptions<ContactSettings> contact,
        IOptions<ApplicationUrlsSettings> urls,
        IOptions<EmailSettings> emailSettings,
        IWebHostEnvironment env,
        ILogger<ContactEmailService> logger)
    {
        _email = email;
        _contact = contact.Value;
        _urls = urls.Value;
        _emailSettings = emailSettings.Value;
        _env = env;
        _logger = logger;
    }

    public async Task<ContactEmailResult> TrySendEmailsAsync(
        ContactSubmission submission,
        CancellationToken ct,
        bool notifyCustomer = true)
    {
        if (!IsSharedSmtpConfigured() && !IsContactSmtpConfigured())
        {
            _logger.LogWarning(
                "Contact emails skipped for submission {Id}: configure EmailSettings or ContactSettings SMTP.",
                submission.Id);
            return new ContactEmailResult(false, false);
        }

        // Customer first (external inbox) — uses EmailSettings SMTP for better deliverability.
        var customerSent = notifyCustomer && await TrySendCustomerConfirmationAsync(submission, ct);
        var supportSent = await TrySendSupportNotificationAsync(submission, ct);
        return new ContactEmailResult(customerSent, supportSent);
    }

    private bool IsSharedSmtpConfigured() =>
        !string.IsNullOrWhiteSpace(_emailSettings.Host)
        && !string.IsNullOrWhiteSpace(_emailSettings.Username)
        && !string.IsNullOrWhiteSpace(_emailSettings.Password);

    private bool IsContactSmtpConfigured()
    {
        var host = string.IsNullOrWhiteSpace(_contact.SmtpHost) ? _emailSettings.Host : _contact.SmtpHost;
        var user = string.IsNullOrWhiteSpace(_contact.SmtpUsername) ? null : _contact.SmtpUsername;
        var pass = string.IsNullOrWhiteSpace(_contact.SmtpPassword) ? null : _contact.SmtpPassword;
        return !string.IsNullOrWhiteSpace(host)
               && !string.IsNullOrWhiteSpace(user)
               && !string.IsNullOrWhiteSpace(pass);
    }

    private async Task<bool> TrySendCustomerConfirmationAsync(ContactSubmission submission, CancellationToken ct)
    {
        var to = submission.Email?.Trim();
        if (string.IsNullOrWhiteSpace(to))
        {
            _logger.LogWarning("Customer confirmation skipped for submission {Id}: email is required but missing.", submission.Id);
            return false;
        }

        var subjectLabel = ContactSubjectCatalog.GetLabel(submission.SubjectId);
        var mailSubject = $"Thank you for reaching out — {subjectLabel}";
        var body = BuildCustomerBody(
            submission.FullName,
            to,
            subjectLabel,
            submission.CreatedAt,
            _urls.FrontendBase,
            _contact.FromEmail,
            _contact.SupportPhone);

        // Prefer EmailSettings (info@) for external customer inboxes; many hosts block support@ → Gmail.
        if (IsSharedSmtpConfigured())
        {
            try
            {
                await SendMailAsync(to, mailSubject, body, ct, _contact.FromEmail, useContactSmtpCredentials: false);
                _logger.LogInformation("Customer confirmation sent to {Email} for submission {Id}.", to, submission.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Customer email via EmailSettings failed for {Email}; trying contact SMTP.", to);
            }
        }

        if (!IsContactSmtpConfigured())
            return false;

        try
        {
            await SendMailAsync(to, mailSubject, body, ct, _contact.FromEmail, useContactSmtpCredentials: true);
            _logger.LogInformation("Customer confirmation sent to {Email} via contact SMTP for submission {Id}.", to, submission.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Customer confirmation failed for submission {Id}, email {Email}.", submission.Id, to);
            return false;
        }
    }

    private async Task<bool> TrySendSupportNotificationAsync(ContactSubmission submission, CancellationToken ct)
    {
        var supportTo = string.IsNullOrWhiteSpace(_contact.SupportNotifyEmail)
            ? _contact.FromEmail
            : _contact.SupportNotifyEmail.Trim();

        if (string.IsNullOrWhiteSpace(supportTo))
            return false;

        try
        {
            var subjectLabel = ContactSubjectCatalog.GetLabel(submission.SubjectId);
            var subject = $"New contact enquiry — {subjectLabel} (#{submission.Id})";
            var body = BuildSupportBody(submission, subjectLabel);

            var replyTo = submission.Email?.Trim();
            if (string.IsNullOrWhiteSpace(replyTo))
                replyTo = null;

            await SendMailAsync(
                supportTo,
                subject,
                body,
                ct,
                replyToEmail: replyTo,
                useContactSmtpCredentials: IsContactSmtpConfigured());

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Support contact notification failed for submission {Id}.", submission.Id);
            return false;
        }
    }

    private Task SendMailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken ct,
        string? replyToEmail,
        bool useContactSmtpCredentials) =>
        _email.SendAsync(
            toEmail,
            subject,
            htmlBody,
            isHtml: true,
            attachments: null,
            ct,
            fromEmail: _contact.FromEmail,
            fromDisplayName: _contact.FromDisplayName,
            replyToEmail: replyToEmail,
            smtpHost: useContactSmtpCredentials && !string.IsNullOrWhiteSpace(_contact.SmtpHost)
                ? _contact.SmtpHost.Trim()
                : null,
            smtpPort: useContactSmtpCredentials ? _contact.SmtpPort : null,
            smtpUsername: useContactSmtpCredentials && !string.IsNullOrWhiteSpace(_contact.SmtpUsername)
                ? _contact.SmtpUsername.Trim()
                : null,
            smtpPassword: useContactSmtpCredentials && !string.IsNullOrWhiteSpace(_contact.SmtpPassword)
                ? _contact.SmtpPassword
                : null);

    private string BuildCustomerBody(
        string customerName,
        string customerEmail,
        string subjectLabel,
        DateTime submittedAt,
        string siteUrl,
        string supportEmail,
        string supportPhone)
    {
        var template = LoadTemplate(CustomerTemplateFile);
        var dateText = AppDateTime.FormatDateTime(submittedAt);

        return template
            .Replace("{{CustomerName}}", Encode(customerName))
            .Replace("{{CustomerEmail}}", Encode(customerEmail))
            .Replace("{{SubjectLabel}}", Encode(subjectLabel))
            .Replace("{{SubmittedAt}}", Encode(dateText))
            .Replace("{{SiteUrl}}", Encode(siteUrl))
            .Replace("{{SupportEmail}}", Encode(supportEmail))
            .Replace("{{SupportPhone}}", Encode(supportPhone))
            .Replace("{{Year}}", DateTime.Now.Year.ToString(CultureInfo.InvariantCulture));
    }

    private string BuildSupportBody(ContactSubmission submission, string subjectLabel)
    {
        var template = LoadTemplate(SupportTemplateFile);
        var dateText = AppDateTime.FormatDateTime(submission.CreatedAt);

        var emailDisplay = string.IsNullOrWhiteSpace(submission.Email)
            ? "Not provided (phone callback)"
            : submission.Email;

        return template
            .Replace("{{SubmissionId}}", submission.Id.ToString(CultureInfo.InvariantCulture))
            .Replace("{{FullName}}", Encode(submission.FullName))
            .Replace("{{Phone}}", Encode(submission.Phone))
            .Replace("{{Email}}", Encode(emailDisplay))
            .Replace("{{SubjectLabel}}", Encode(subjectLabel))
            .Replace("{{Message}}", EncodeMultiline(submission.Message))
            .Replace("{{SubmittedAt}}", Encode(dateText))
            .Replace("{{Year}}", DateTime.Now.Year.ToString(CultureInfo.InvariantCulture));
    }

    private string LoadTemplate(string fileName)
    {
        var path = Path.Combine(_env.ContentRootPath, "Templates", fileName);
        if (File.Exists(path))
            return File.ReadAllText(path, Encoding.UTF8);

        _logger.LogWarning("Email template not found at {Path}.", path);
        return fileName.Contains("Support", StringComparison.OrdinalIgnoreCase)
            ? "<p>New enquiry from {{FullName}} ({{Email}}). Subject: {{SubjectLabel}}. Message: {{Message}}</p>"
            : """
              <p>Dear {{CustomerName}},</p>
              <p>Thank you for reaching out regarding <strong>{{SubjectLabel}}</strong>.</p>
              <p>Our team will connect with you at the earliest for the same and resolve the issue.</p>
              <p>Thank you.<br/>Regards,<br/>MSME Bharat Manch Support Team</p>
              """;
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value ?? "");

    private static string EncodeMultiline(string value)
    {
        var encoded = Encode(value ?? "");
        return encoded.Replace("\r\n", "<br/>", StringComparison.Ordinal)
            .Replace("\n", "<br/>", StringComparison.Ordinal);
    }
}
