namespace RB_Website_API.Auth;

public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string body, CancellationToken ct);
}

public interface ISmsSender
{
    Task SendAsync(string toPhone, string message, CancellationToken ct);
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

