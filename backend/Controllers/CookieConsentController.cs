using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RB_Website_API.Auth;
using RB_Website_API.Services;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/cookie-consent")]
public sealed class CookieConsentController : ControllerBase
{
    private readonly ICookieConsentService _consent;

    public CookieConsentController(ICookieConsentService consent) => _consent = consent;

    public sealed record AcceptRequest(string? SessionToken = null);

    [AllowAnonymous]
    [HttpPost("accept")]
    public async Task<IActionResult> Accept([FromBody] AcceptRequest? req, CancellationToken ct)
    {
        var userId = CurrentUser.GetUserId(User);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var ua = HttpContext.Request.Headers.UserAgent.ToString();

        await _consent.AcceptAsync(userId, req?.SessionToken, ip, ua, ct);

        return Ok(new { success = true });
    }
}
