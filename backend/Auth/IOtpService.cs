namespace RB_Website_API.Auth;

public interface IOtpService
{
    Task SendEmailOtpAsync(string email, CancellationToken ct);
    Task VerifyEmailOtpAsync(string email, string code, CancellationToken ct);
    Task EnsureEmailVerifiedAsync(string email, CancellationToken ct);

    Task SendSmsOtpAsync(string phone, CancellationToken ct);
    Task VerifySmsOtpAsync(string phone, string code, CancellationToken ct);
    Task EnsureSmsVerifiedAsync(string phone, CancellationToken ct);
    void InvalidateSmsOtp(string phone);

    Task SendPasswordResetEmailOtpAsync(string email, string customerName, CancellationToken ct);
    Task VerifyPasswordResetEmailOtpAsync(string email, string code, CancellationToken ct);
    Task EnsurePasswordResetEmailVerifiedAsync(string email, CancellationToken ct);
    void InvalidatePasswordResetEmailOtp(string email);

    Task SendPasswordResetSmsOtpAsync(string phone, CancellationToken ct);
    Task VerifyPasswordResetSmsOtpAsync(string phone, string code, CancellationToken ct);
    Task EnsurePasswordResetSmsVerifiedAsync(string phone, CancellationToken ct);
    void InvalidatePasswordResetSmsOtp(string phone);
}

