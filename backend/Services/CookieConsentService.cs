using RB_Website_API.Data;
using RB_Website_API.Models;

namespace RB_Website_API.Services;

public sealed class CookieConsentService : ICookieConsentService
{
    private readonly AppDbContext _db;

    public CookieConsentService(AppDbContext db) => _db = db;

    public async Task AcceptAsync(Guid? userId, string? sessionToken, string? ipAddress, string? userAgent, CancellationToken ct = default)
    {
        var log = new CookieConsentLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SessionToken = sessionToken,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            AcceptedAt = DateTime.UtcNow,
        };

        _db.CookieConsentLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }
}
