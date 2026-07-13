using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
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

    /// <summary>Pre-resolves DNS and opens a TCP connection to the default SMTP host (no message sent).</summary>
    Task WarmupAsync(CancellationToken ct = default);
}

public interface ISmsSender
{
    Task  SendAsync(string toPhone, string message, CancellationToken ct);
}

public sealed class SmtpEmailSender : IEmailSender
{
    private const int SmtpSendTimeoutMs = 60_000;

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
            Timeout = SmtpSendTimeoutMs,
        };

        // Do not pass the HTTP request token into SMTP: a slow first TLS handshake can outlast
        // the browser/proxy, which aborts the request and cancels delivery mid-send.
        ct.ThrowIfCancellationRequested();

        _logger.LogInformation(
            "Email send started To={To} Host={Host} Port={Port} Subject={Subject}",
            toEmail,
            host,
            port,
            subject);

        var sw = Stopwatch.StartNew();
        using var smtpCts = new CancellationTokenSource(SmtpSendTimeoutMs);

        try
        {
            await client.SendMailAsync(message, smtpCts.Token);
            _logger.LogInformation(
                "Email send success To={To} Host={Host} Port={Port} ElapsedMs={ElapsedMs}",
                toEmail,
                host,
                port,
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            var clientDisconnected = ct.IsCancellationRequested;
            _logger.LogError(
                ex,
                "Email send failure To={To} Host={Host} Port={Port} ElapsedMs={ElapsedMs} ClientDisconnected={ClientDisconnected} SmtpTimedOut={SmtpTimedOut}",
                toEmail,
                host,
                port,
                sw.ElapsedMilliseconds,
                clientDisconnected,
                smtpCts.IsCancellationRequested);
            throw;
        }
    }

    public async Task WarmupAsync(CancellationToken ct = default)
    {
        var host = _settings.Host?.Trim();
        var port = _settings.Port;
        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogInformation("SMTP warmup skipped: EmailSettings:Host is not configured.");
            return;
        }

        var sw = Stopwatch.StartNew();
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(host, ct);
            _logger.LogInformation(
                "SMTP warmup DNS resolved Host={Host} AddressCount={AddressCount} ElapsedMs={ElapsedMs}",
                host,
                addresses.Length,
                sw.ElapsedMilliseconds);

            using var tcp = new TcpClient();
            await tcp.ConnectAsync(host, port, ct);
            _logger.LogInformation(
                "SMTP warmup TCP connected Host={Host} Port={Port} ElapsedMs={ElapsedMs}",
                host,
                port,
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "SMTP warmup failed Host={Host} Port={Port} ElapsedMs={ElapsedMs}",
                host,
                port,
                sw.ElapsedMilliseconds);
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

    public Task WarmupAsync(CancellationToken ct = default) => Task.CompletedTask;
}

public sealed class ConsoleSmsSender : ISmsSender
{
    public Task SendAsync(string toPhone, string message, CancellationToken ct)
    {
        Console.WriteLine($"[SMS] To={toPhone} Message={message}");
        return Task.CompletedTask;
    }
}
