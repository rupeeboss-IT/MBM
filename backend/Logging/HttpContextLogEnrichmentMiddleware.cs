using RB_Website_API.Auth;
using Serilog.Context;

namespace RB_Website_API.Logging;

/// <summary>
/// Pushes request-scoped properties into Serilog's LogContext for file log enrichment.
/// </summary>
public sealed class HttpContextLogEnrichmentMiddleware
{
    private readonly RequestDelegate _next;

    public HttpContextLogEnrichmentMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var requestUrl = $"{context.Request.Method} {context.Request.Path}{context.Request.QueryString}";
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "-";
        var correlationId = context.TraceIdentifier;
        var userId = CurrentUser.GetUserId(context.User)?.ToString() ?? "-";

        using (LogContext.PushProperty("RequestUrl", requestUrl))
        using (LogContext.PushProperty("IpAddress", ipAddress))
        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("UserId", userId))
        {
            await _next(context);
        }
    }
}
