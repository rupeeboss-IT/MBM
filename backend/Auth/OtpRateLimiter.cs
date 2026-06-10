using System.Collections.Concurrent;

namespace RB_Website_API.Auth;

public interface IOtpRateLimiter
{
    /// <summary>Throws <see cref="OtpRateLimitExceededException"/> if this email cannot receive another OTP in the current window.</summary>
    void ThrowIfEmailRateLimited(string email);

    /// <summary>Throws <see cref="OtpRateLimitExceededException"/> if this phone cannot receive another OTP in the current window.</summary>
    void ThrowIfPhoneRateLimited(string phone);

    /// <summary>Call only after a successful email OTP send.</summary>
    void RecordSuccessfulEmailOtpSend(string email);

    /// <summary>Call only after a successful SMS OTP send.</summary>
    void RecordSuccessfulSmsOtpSend(string phone);
}

/// <summary>Raised when more than 3 OTP sends occurred for the same email or phone within a rolling 10-minute window.</summary>
public sealed class OtpRateLimitExceededException : InvalidOperationException
{
    public OtpRateLimitExceededException(string message) : base(message) { }
}

/// <summary>In-memory rolling window: max 3 successful OTP sends per email and per phone per 10 minutes.</summary>
public sealed class OtpRateLimiter : IOtpRateLimiter
{
    private const int MaxSendsPerWindow = 3;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(10);

    private readonly ConcurrentDictionary<string, List<DateTimeOffset>> _events = new();

    public void ThrowIfEmailRateLimited(string email)
    {
        ThrowIfLimited(KeyEmail(email));
    }

    public void ThrowIfPhoneRateLimited(string phone)
    {
        ThrowIfLimited(KeyPhone(phone));
    }

    public void RecordSuccessfulEmailOtpSend(string email)
    {
        Record(KeyEmail(email));
    }

    public void RecordSuccessfulSmsOtpSend(string phone)
    {
        Record(KeyPhone(phone));
    }

    private static string KeyEmail(string email) =>
        $"e:{(email ?? "").Trim().ToLowerInvariant()}";

    private static string KeyPhone(string phone) => $"p:{IndianPhone.Digits(phone)}";

    private void ThrowIfLimited(string key)
    {
        if (string.IsNullOrEmpty(key) || key is "e:" or "p:")
            return;

        var list = _events.GetOrAdd(key, _ => new List<DateTimeOffset>());
        lock (list)
        {
            Prune(list);
            if (list.Count >= MaxSendsPerWindow)
            {
                throw new OtpRateLimitExceededException(
                    "You have requested too many OTPs for this contact. Please wait 10 minutes before trying again.");
            }
        }
    }

    private void Record(string key)
    {
        if (string.IsNullOrEmpty(key) || key is "e:" or "p:")
            return;

        var list = _events.GetOrAdd(key, _ => new List<DateTimeOffset>());
        lock (list)
        {
            Prune(list);
            list.Add(DateTimeOffset.Now);
        }
    }

    private static void Prune(List<DateTimeOffset> list)
    {
        var cutoff = DateTimeOffset.Now - Window;
        list.RemoveAll(t => t < cutoff);
    }
}
