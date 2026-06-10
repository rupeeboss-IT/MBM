using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace RB_Website_API.Auth;

public sealed class InMemoryOtpService : IOtpService
{
    private readonly ConcurrentDictionary<string, OtpRecord> _store = new();
    private readonly IEmailSender _email;
    private readonly ISmsSender _sms;
    private readonly IOtpRateLimiter _rate;

    public InMemoryOtpService(IEmailSender email, ISmsSender sms, IOtpRateLimiter rate)
    {
        _email = email;
        _sms = sms;
        _rate = rate;
    }

    public async Task SendEmailOtpAsync(string email, CancellationToken ct)
    {
        _rate.ThrowIfEmailRateLimited(email);
        var key = Key("email", email);
        var otp = NewOtp();
        await _email.SendAsync(email, "Your MBM OTP", $"Your OTP is {otp}. It expires in 10 minutes.", ct);
        _rate.RecordSuccessfulEmailOtpSend(email);
        _store[key] = new OtpRecord(Hash(otp), DateTimeOffset.Now.AddMinutes(10), Verified: false);
    }

    public Task VerifyEmailOtpAsync(string email, string code, CancellationToken ct)
    {
        var key = Key("email", email);
        if (!_store.TryGetValue(key, out var rec) || rec.ExpiresAt < DateTimeOffset.Now)
            throw new InvalidOperationException("OTP expired. Please resend OTP.");

        if (!SlowEquals(rec.CodeHash, Hash(code)))
            throw new InvalidOperationException("Invalid OTP.");

        _store[key] = rec with { Verified = true };
        return Task.CompletedTask;
    }

    public Task EnsureEmailVerifiedAsync(string email, CancellationToken ct)
    {
        var key = Key("email", email);
        if (!_store.TryGetValue(key, out var rec) || rec.ExpiresAt < DateTimeOffset.Now)
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
        var smsBody =
            $"Dear Customer,\nYour mobile verification code is {otp}\nPlease use this code to verify your account. Regard Team RupeeBoss.";
        await _sms.SendAsync(phone, smsBody, ct);
        _rate.RecordSuccessfulSmsOtpSend(phone);
        _store[key] = new OtpRecord(Hash(otp), DateTimeOffset.Now.AddMinutes(10), Verified: false);
    }

    public Task VerifySmsOtpAsync(string phone, string code, CancellationToken ct)
    {
        var key = Key("sms", phone);
        if (!_store.TryGetValue(key, out var rec) || rec.ExpiresAt < DateTimeOffset.Now)
            throw new InvalidOperationException("OTP expired. Please resend OTP.");

        if (!SlowEquals(rec.CodeHash, Hash(code)))
            throw new InvalidOperationException("Invalid OTP.");

        _store[key] = rec with { Verified = true };
        return Task.CompletedTask;
    }

    public Task EnsureSmsVerifiedAsync(string phone, CancellationToken ct)
    {
        var key = Key("sms", phone);
        if (!_store.TryGetValue(key, out var rec) || rec.ExpiresAt < DateTimeOffset.Now)
            throw new InvalidOperationException("Mobile OTP expired. Please verify again.");
        if (!rec.Verified)
            throw new InvalidOperationException("Mobile not verified. Please verify via OTP.");
        return Task.CompletedTask;
    }

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

    private sealed record OtpRecord(byte[] CodeHash, DateTimeOffset ExpiresAt, bool Verified);
}

