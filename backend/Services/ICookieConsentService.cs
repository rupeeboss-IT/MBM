namespace RB_Website_API.Services;

public interface ICookieConsentService
{
    Task AcceptAsync(Guid? userId, string? sessionToken, string? ipAddress, string? userAgent, CancellationToken ct = default);
}
