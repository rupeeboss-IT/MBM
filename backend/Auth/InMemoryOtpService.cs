using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace RB_Website_API.Auth;

public sealed class InMemoryOtpService : IOtpService
{
    private readonly ConcurrentDictionary<string, OtpRecord> _store = new();
    private readonly IEmailSender _email;
    private readonly ISmsSender _sms;

    public InMemoryOtpService(IEmailSender email, ISmsSender sms)
    {
        _email = email;
        _sms = sms;
    }

    public async Task SendEmailOtpAsync(string email, CancellationToken ct)
    {
        var key = Key("email", email);
        var otp = NewOtp();
        _store[key] = new OtpRecord(Hash(otp), DateTimeOffset.UtcNow.AddMinutes(10), Verified: false);
        await _email.SendAsync(email, "Your MBM OTP", $"Your OTP is {otp}. It expires in 10 minutes.", ct);
    }

    public Task VerifyEmailOtpAsync(string email, string code, CancellationToken ct)
    {
        var key = Key("email", email);
        if (!_store.TryGetValue(key, out var rec) || rec.ExpiresAt < DateTimeOffset.UtcNow)
            throw new InvalidOperationException("OTP expired. Please resend OTP.");

        if (!SlowEquals(rec.CodeHash, Hash(code)))
            throw new InvalidOperationException("Invalid OTP.");

        _store[key] = rec with { Verified = true };
        return Task.CompletedTask;
    }

    public async Task SendSmsOtpAsync(string phone, CancellationToken ct)
    {
        var key = Key("sms", phone);
        var otp = NewOtp();
        _store[key] = new OtpRecord(Hash(otp), DateTimeOffset.UtcNow.AddMinutes(10), Verified: false);
        await _sms.SendAsync(phone, $"MBM OTP: {otp} (valid 10 min)", ct);
    }

    public Task VerifySmsOtpAsync(string phone, string code, CancellationToken ct)
    {
        var key = Key("sms", phone);
        if (!_store.TryGetValue(key, out var rec) || rec.ExpiresAt < DateTimeOffset.UtcNow)
            throw new InvalidOperationException("OTP expired. Please resend OTP.");

        if (!SlowEquals(rec.CodeHash, Hash(code)))
            throw new InvalidOperationException("Invalid OTP.");

        _store[key] = rec with { Verified = true };
        return Task.CompletedTask;
    }

    private static string Key(string kind, string id) => $"{kind}:{id.Trim().ToLowerInvariant()}";

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

