using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace RB_Website_API.Auth;

public sealed class InMemoryOtpService : IOtpService
{
    private static readonly TimeSpan OtpTtl = TimeSpan.FromMinutes(10);

    private readonly IMemoryCache _cache;
    private readonly IEmailSender _email;
    private readonly ISmsSender _sms;
    private readonly IOtpRateLimiter _rate;
    private readonly IOtpEmailTemplateService _otpEmail;
    private readonly ILogger<InMemoryOtpService> _logger;

    public InMemoryOtpService(
        IMemoryCache cache,
        IEmailSender email,
        ISmsSender sms,
        IOtpRateLimiter rate,
        IOtpEmailTemplateService otpEmail,
        ILogger<InMemoryOtpService> logger)
    {
        _cache = cache;
        _email = email;
        _sms = sms;
        _rate = rate;
        _otpEmail = otpEmail;
        _logger = logger;
    }

    public async Task SendEmailOtpAsync(string email, CancellationToken ct)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        _rate.ThrowIfEmailRateLimited(email);

        var key = Key("email", email);
        var otp = NewOtp();
        _logger.LogInformation("OTP generated for {Email} Channel={Channel}", normalizedEmail, "email");

        var sw = Stopwatch.StartNew();
        try
        {
            var htmlBody = _otpEmail.BuildRegistrationOtpEmail(otp);
            await _email.SendAsync(
                email,
                "Your MSME Bharat Manch verification code",
                htmlBody,
                isHtml: true,
                attachments: null,
                ct);
            _logger.LogInformation(
                "Email provider response success for {Email} ElapsedMs={ElapsedMs}",
                normalizedEmail,
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Email send failure for {Email} ElapsedMs={ElapsedMs}",
                normalizedEmail,
                sw.ElapsedMilliseconds);
            throw;
        }

        _rate.RecordSuccessfulEmailOtpSend(email);
        _cache.Set(key, new OtpRecord(Hash(otp), Verified: false), OtpTtl);
        _logger.LogInformation("OTP saved for {Email} Channel={Channel}", normalizedEmail, "email");
    }

    public Task VerifyEmailOtpAsync(string email, string code, CancellationToken ct)
    {
        var key = Key("email", email);
        if (!_cache.TryGetValue(key, out OtpRecord? rec) || rec is null)
            throw new InvalidOperationException("OTP expired. Please resend OTP.");

        if (!SlowEquals(rec.CodeHash, Hash(code)))
            throw new InvalidOperationException("Invalid OTP.");

        _cache.Set(key, rec with { Verified = true }, OtpTtl);
        return Task.CompletedTask;
    }

    public Task EnsureEmailVerifiedAsync(string email, CancellationToken ct)
    {
        var key = Key("email", email);
        if (!_cache.TryGetValue(key, out OtpRecord? rec) || rec is null)
            throw new InvalidOperationException("Email OTP expired. Please verify again.");
        if (!rec.Verified)
            throw new InvalidOperationException("Email not verified. Please verify via OTP.");
        return Task.CompletedTask;
    }

    public async Task SendSmsOtpAsync(string phone, CancellationToken ct)
    {
        _rate.ThrowIfPhoneRateLimited(phone);
        var key = Key("sms", phone);
        var otp = NewOtp();
        var normalizedPhone = IndianPhone.Digits(phone);
        var smsBody = SmsOtpMessages.VerificationCode(otp);

        _logger.LogInformation("Registration SMS OTP generated for {Phone}", normalizedPhone);

        try
        {
            ct.ThrowIfCancellationRequested();
            await _sms.SendAsync(normalizedPhone, smsBody, ct);
            _logger.LogInformation("Registration SMS OTP sent for {Phone}", normalizedPhone);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration SMS OTP send failed for {Phone}", normalizedPhone);
            throw;
        }

        _rate.RecordSuccessfulSmsOtpSend(phone);
        _cache.Set(key, new OtpRecord(Hash(otp), Verified: false), OtpTtl);
    }

    public Task VerifySmsOtpAsync(string phone, string code, CancellationToken ct)
    {
        var key = Key("sms", phone);
        if (!_cache.TryGetValue(key, out OtpRecord? rec) || rec is null)
            throw new InvalidOperationException("OTP expired. Please resend OTP.");

        if (!SlowEquals(rec.CodeHash, Hash(code)))
            throw new InvalidOperationException("Invalid OTP.");

        _cache.Set(key, rec with { Verified = true }, OtpTtl);
        return Task.CompletedTask;
    }

    public Task EnsureSmsVerifiedAsync(string phone, CancellationToken ct)
    {
        var key = Key("sms", phone);
        if (!_cache.TryGetValue(key, out OtpRecord? rec) || rec is null)
            throw new InvalidOperationException("Mobile OTP expired. Please verify again.");
        if (!rec.Verified)
            throw new InvalidOperationException("Mobile not verified. Please verify via OTP.");
        return Task.CompletedTask;
    }

    public async Task SendPasswordResetEmailOtpAsync(string email, string customerName, CancellationToken ct)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        _rate.ThrowIfEmailRateLimited(email);

        var key = PasswordResetKey("email", email);
        var otp = NewOtp();
        _logger.LogInformation("Password reset OTP generated for {Email} Channel={Channel}", normalizedEmail, "email");

        var sw = Stopwatch.StartNew();
        try
        {
            var htmlBody = _otpEmail.BuildPasswordResetOtpEmail(otp, customerName);
            await _email.SendAsync(
                email,
                "MSME Bharat Manch — Password Reset Verification",
                htmlBody,
                isHtml: true,
                attachments: null,
                ct);
            _logger.LogInformation(
                "Password reset email sent for {Email} ElapsedMs={ElapsedMs}",
                normalizedEmail,
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Password reset email send failure for {Email} ElapsedMs={ElapsedMs}",
                normalizedEmail,
                sw.ElapsedMilliseconds);
            throw;
        }

        _rate.RecordSuccessfulEmailOtpSend(email);
        _cache.Set(key, new OtpRecord(Hash(otp), Verified: false, AttemptsLeft: 5), OtpTtl);
    }

    public Task VerifyPasswordResetEmailOtpAsync(string email, string code, CancellationToken ct)
        => VerifyPasswordResetOtpAsync(PasswordResetKey("email", email), code);

    public Task EnsurePasswordResetEmailVerifiedAsync(string email, CancellationToken ct)
        => EnsurePasswordResetVerifiedAsync(PasswordResetKey("email", email), "Email");

    public void InvalidatePasswordResetEmailOtp(string email)
        => _cache.Remove(PasswordResetKey("email", email));

    public async Task SendPasswordResetSmsOtpAsync(string phone, CancellationToken ct)
    {
        _rate.ThrowIfPhoneRateLimited(phone);
        var key = PasswordResetKey("sms", phone);
        var otp = NewOtp();
        var normalizedPhone = IndianPhone.Digits(phone);
        var smsBody = SmsOtpMessages.VerificationCode(otp);

        _logger.LogInformation("Password reset SMS OTP generated for {Phone}", normalizedPhone);

        try
        {
            ct.ThrowIfCancellationRequested();
            await _sms.SendAsync(normalizedPhone, smsBody, ct);
            _logger.LogInformation("Password reset SMS OTP sent for {Phone}", normalizedPhone);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password reset SMS OTP send failed for {Phone}", normalizedPhone);
            throw;
        }

        _rate.RecordSuccessfulSmsOtpSend(phone);
        _cache.Set(key, new OtpRecord(Hash(otp), Verified: false, AttemptsLeft: 5), OtpTtl);
    }

    public Task VerifyPasswordResetSmsOtpAsync(string phone, string code, CancellationToken ct)
        => VerifyPasswordResetOtpAsync(PasswordResetKey("sms", phone), code);

    public Task EnsurePasswordResetSmsVerifiedAsync(string phone, CancellationToken ct)
        => EnsurePasswordResetVerifiedAsync(PasswordResetKey("sms", phone), "Mobile");

    public void InvalidatePasswordResetSmsOtp(string phone)
        => _cache.Remove(PasswordResetKey("sms", phone));

    public void InvalidateSmsOtp(string phone)
        => _cache.Remove(Key("sms", phone));

    private Task VerifyPasswordResetOtpAsync(string key, string code)
    {
        if (!_cache.TryGetValue(key, out OtpRecord? rec) || rec is null)
            throw new InvalidOperationException("OTP expired. Please resend OTP.");

        if (rec.AttemptsLeft <= 0)
            throw new InvalidOperationException("Too many attempts. Please request a new OTP.");

        if (!SlowEquals(rec.CodeHash, Hash(code)))
        {
            _cache.Set(key, rec with { AttemptsLeft = rec.AttemptsLeft - 1 }, OtpTtl);
            throw new InvalidOperationException("Invalid OTP.");
        }

        _cache.Set(key, rec with { Verified = true }, OtpTtl);
        return Task.CompletedTask;
    }

    private Task EnsurePasswordResetVerifiedAsync(string key, string channelLabel)
    {
        if (!_cache.TryGetValue(key, out OtpRecord? rec) || rec is null)
            throw new InvalidOperationException($"{channelLabel} OTP expired. Please verify again.");
        if (!rec.Verified)
            throw new InvalidOperationException($"{channelLabel} not verified. Please verify via OTP.");
        return Task.CompletedTask;
    }

    private static string PasswordResetKey(string kind, string id) =>
        kind == "sms"
            ? $"pwdreset:sms:{IndianPhone.Digits(id)}"
            : $"pwdreset:email:{id.Trim().ToLowerInvariant()}";

    private static string Key(string kind, string id) =>
        kind == "sms"
            ? $"sms:{IndianPhone.Digits(id)}"
            : $"{kind}:{id.Trim().ToLowerInvariant()}";

    private static string NewOtp()
    {
        // 6-digit numeric
        var bytes = RandomNumberGenerator.GetBytes(4);
        var n = BitConverter.ToUInt32(bytes, 0) % 1_000_000;
        return n.ToString("D6");
    }

    private static byte[] Hash(string code)
    {
        // Simple SHA256 hash (replace with HMAC + secret when you add appsettings)
        return SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(code));
    }

    private static bool SlowEquals(byte[] a, byte[] b)
    {
        if (a.Length != b.Length) return false;
        var diff = 0;
        for (var i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
        return diff == 0;
    }

    private sealed record OtpRecord(byte[] CodeHash, bool Verified, int AttemptsLeft = int.MaxValue);
}
