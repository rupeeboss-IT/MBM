namespace RB_Website_API.Auth;

public interface IOtpService
{
    Task SendEmailOtpAsync(string email, CancellationToken ct);
    Task VerifyEmailOtpAsync(string email, string code, CancellationToken ct);

    Task SendSmsOtpAsync(string phone, CancellationToken ct);
    Task VerifySmsOtpAsync(string phone, string code, CancellationToken ct);
}

