using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace RB_Website_API.Auth;

public interface IPasswordResetService
{
    Task RequestResetAsync(string email, CancellationToken ct);
    Task ResetAsync(string email, string code, string newPassword, CancellationToken ct);
}

/// <summary>
/// Email OTP-based password reset. In-memory store (10-minute expiry).
/// </summary>
public sealed class InMemoryPasswordResetService : IPasswordResetService
{
    private readonly ConcurrentDictionary<string, ResetRecord> _store = new();
    private readonly IEmailSender _email;
    private readonly IOtpRateLimiter _rate;

    public InMemoryPasswordResetService(IEmailSender email, IOtpRateLimiter rate)
    {
        _email = email;
        _rate = rate;
    }

    public async Task RequestResetAsync(string email, CancellationToken ct)
    {
        _rate.ThrowIfEmailRateLimited(email);

        var norm = NormalizeEmail(email);
        var otp = NewOtp();
        await _email.SendAsync(norm, "MBM Password Reset OTP", $"Your password reset OTP is {otp}. It expires in 10 minutes.", ct);

        _rate.RecordSuccessfulEmailOtpSend(norm);
        _store[norm] = new ResetRecord(Hash(otp), DateTimeOffset.UtcNow.AddMinutes(10), AttemptsLeft: 5);
    }

    public Task ResetAsync(string email, string code, string newPassword, CancellationToken ct)
    {
        var norm = NormalizeEmail(email);
        if (!_store.TryGetValue(norm, out var rec) || rec.ExpiresAt < DateTimeOffset.UtcNow)
            throw new InvalidOperationException("OTP expired. Please request a new OTP.");

        if (rec.AttemptsLeft <= 0)
            throw new InvalidOperationException("Too many attempts. Please request a new OTP.");

        if (!SlowEquals(rec.CodeHash, Hash(code)))
        {
            _store[norm] = rec with { AttemptsLeft = rec.AttemptsLeft - 1 };
            throw new InvalidOperationException("Invalid OTP.");
        }

        // OTP is correct; caller will update password in DB. Remove record to prevent reuse.
        _store.TryRemove(norm, out _);
        return Task.CompletedTask;
    }

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException("Email is required.");
        return email.Trim().ToLowerInvariant();
    }

    private static string NewOtp()
    {
        var bytes = RandomNumberGenerator.GetBytes(4);
        var n = BitConverter.ToUInt32(bytes, 0) % 1_000_000;
        return n.ToString("D6");
    }

    private static byte[] Hash(string code)
    {
        return SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(code ?? ""));
    }

    private static bool SlowEquals(byte[] a, byte[] b)
    {
        if (a.Length != b.Length) return false;
        var diff = 0;
        for (var i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
        return diff == 0;
    }

    private sealed record ResetRecord(byte[] CodeHash, DateTimeOffset ExpiresAt, int AttemptsLeft);
}

