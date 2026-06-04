using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RB_Website_API.Auth;

public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string body, CancellationToken ct);

    Task SendAsync(
        string toEmail,
        string subject,
        string body,
        bool isHtml,
        IReadOnlyList<EmailAttachment>? attachments,
        CancellationToken ct,
        string? fromEmail = null,
        string? fromDisplayName = null,
        string? replyToEmail = null,
        string? smtpHost = null,
        int? smtpPort = null,
        string? smtpUsername = null,
        string? smtpPassword = null);
}

public interface ISmsSender
{
    Task SendAsync(string toPhone, string message, CancellationToken ct);
}

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailSettings> options, ILogger<SmtpEmailSender> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public Task SendAsync(string toEmail, string subject, string body, CancellationToken ct)
        => SendAsync(toEmail, subject, body, isHtml: false, attachments: null, ct);

    public async Task SendAsync(
        string toEmail,
        string subject,
        string body,
        bool isHtml,
        IReadOnlyList<EmailAttachment>? attachments,
        CancellationToken ct,
        string? fromEmail = null,
        string? fromDisplayName = null,
        string? replyToEmail = null,
        string? smtpHost = null,
        int? smtpPort = null,
        string? smtpUsername = null,
        string? smtpPassword = null)
    {
        var host = string.IsNullOrWhiteSpace(smtpHost) ? _settings.Host : smtpHost.Trim();
        var port = smtpPort ?? _settings.Port;

        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogWarning("SMTP host is not set; email not sent to {To}", toEmail);
            throw new InvalidOperationException("Email is not configured (missing EmailSettings:Host or contact SMTP host).");
        }

        var fromAddress = string.IsNullOrWhiteSpace(fromEmail)
            ? (string.IsNullOrWhiteSpace(_settings.From) ? _settings.Username : _settings.From)
            : fromEmail.Trim();
        if (string.IsNullOrWhiteSpace(fromAddress))
            throw new InvalidOperationException("Email is not configured (missing EmailSettings:From or Username).");

        var user = string.IsNullOrWhiteSpace(smtpUsername) ? _settings.Username : smtpUsername.Trim();
        if (string.IsNullOrWhiteSpace(user))
            user = fromAddress;

        var password = string.IsNullOrWhiteSpace(smtpPassword) ? _settings.Password : smtpPassword;
        var displayName = fromDisplayName ?? _settings.FromDisplayName;

        using var message = new MailMessage
        {
            From = string.IsNullOrEmpty(displayName)
                ? new MailAddress(fromAddress)
                : new MailAddress(fromAddress, displayName),
            Subject = subject,
            Body = body,
            IsBodyHtml = isHtml,
        };
        message.To.Add(toEmail);

        if (!string.IsNullOrWhiteSpace(replyToEmail))
            message.ReplyToList.Add(new MailAddress(replyToEmail.Trim()));

        if (attachments is not null)
        {
            foreach (var att in attachments)
            {
                var stream = new MemoryStream(att.Content);
                message.Attachments.Add(new Attachment(stream, att.FileName, att.ContentType));
            }
        }

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(user, password),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 30000,
        };

        try
        {
            await client.SendMailAsync(message, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP send failed to {To} via {Host}:{Port}", toEmail, host, port);
            throw;
        }
    }
}

public sealed class ConsoleEmailSender : IEmailSender
{
    public Task SendAsync(string toEmail, string subject, string body, CancellationToken ct)
        => SendAsync(toEmail, subject, body, isHtml: false, attachments: null, ct);

    public Task SendAsync(
        string toEmail,
        string subject,
        string body,
        bool isHtml,
        IReadOnlyList<EmailAttachment>? attachments,
        CancellationToken ct,
        string? fromEmail = null,
        string? fromDisplayName = null,
        string? replyToEmail = null,
        string? smtpHost = null,
        int? smtpPort = null,
        string? smtpUsername = null,
        string? smtpPassword = null)
    {
        var attCount = attachments?.Count ?? 0;
        Console.WriteLine($"[EMAIL] SMTP={smtpHost ?? "(default)"} From={fromEmail ?? "(default)"} ReplyTo={replyToEmail ?? "-"} To={toEmail} Subject={subject} Html={isHtml} Attachments={attCount}");
        return Task.CompletedTask;
    }
}

public sealed class ConsoleSmsSender : ISmsSender
{
    public Task SendAsync(string toPhone, string message, CancellationToken ct)
    {
        Console.WriteLine($"[SMS] To={toPhone} Message={message}");
        return Task.CompletedTask;
    }
}

