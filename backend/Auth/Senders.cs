using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RB_Website_API.Auth;

public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string body, CancellationToken ct);
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

    public async Task SendAsync(string toEmail, string subject, string body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_settings.Host))
        {
            _logger.LogWarning("EmailSettings.Host is not set; email not sent to {To}", toEmail);
            throw new InvalidOperationException("Email is not configured (missing EmailSettings:Host).");
        }

        var fromAddress = string.IsNullOrWhiteSpace(_settings.From)
            ? _settings.Username
            : _settings.From;
        if (string.IsNullOrWhiteSpace(fromAddress))
            throw new InvalidOperationException("Email is not configured (missing EmailSettings:From or Username).");

        var user = string.IsNullOrWhiteSpace(_settings.Username) ? fromAddress : _settings.Username;

        using var message = new MailMessage
        {
            From = string.IsNullOrEmpty(_settings.FromDisplayName)
                ? new MailAddress(fromAddress)
                : new MailAddress(fromAddress, _settings.FromDisplayName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false,
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(user, _settings.Password),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 30000,
        };

        try
        {
            await client.SendMailAsync(message, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP send failed to {To} via {Host}:{Port}", toEmail, _settings.Host, _settings.Port);
            throw;
        }
    }
}

public sealed class ConsoleEmailSender : IEmailSender
{
    public Task SendAsync(string toEmail, string subject, string body, CancellationToken ct)
    {
        Console.WriteLine($"[EMAIL] To={toEmail} Subject={subject} Body={body}");
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

