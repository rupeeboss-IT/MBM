using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using RB_Website_API.Services;

namespace RB_Website_API.Middleware;

public sealed class GlobalApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalApiExceptionHandler> _logger;
    private readonly IApiExceptionLogService _exceptionLogs;

    public GlobalApiExceptionHandler(
        ILogger<GlobalApiExceptionHandler> logger,
        IApiExceptionLogService exceptionLogs)
    {
        _logger = logger;
        _exceptionLogs = exceptionLogs;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (!IsApiRequest(httpContext))
            return false;

        var operationKey = ResolveOperationKey(httpContext);
        var userMessage = UserFriendlyErrorMapper.GetUserMessage(exception, operationKey);
        var statusCode = UserFriendlyErrorMapper.GetStatusCode(exception);

        _exceptionLogs.LogInBackground(httpContext, exception, statusCode, userMessage, operationKey);

        if (httpContext.Response.HasStarted)
        {
            _logger.LogWarning("Response already started; cannot write error body for {Path}", httpContext.Request.Path);
            return false;
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        var payload = new { success = false, message = userMessage };
        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(payload), cancellationToken);
        return true;
    }

    private static bool IsApiRequest(HttpContext context) =>
        context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase);

    private static string? ResolveOperationKey(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        var method = context.Request.Method.ToUpperInvariant();

        if (path.Contains("/admin/reports/history")) return "report_history";
        if (path.Contains("/admin/reports/upload")) return "report_upload";
        if (path.Contains("/admin/reports") && path.Contains("/customers/search")) return "report_search";
        if (path.Contains("/customer/reports") && path.Contains("/download")) return "report_download";
        if (path.Contains("/customer/reports")) return "report_history";
        if (path.Contains("/scheme-discovery")) return "scheme_discovery";
        if (path.Contains("/payment") && path.Contains("/verify")) return "payment_verify";
        if (path.Contains("/payment") && path.Contains("/download")) return "invoice_download";
        if (path.Contains("/payment") && path.Contains("/invoices")) return "invoice_list";
        if (path.Contains("/payment") && method == "POST") return "payment_create";
        if (path.Contains("/user/login")) return "login";
        if (path.Contains("/user/register")) return "register";
        if (path.Contains("/password/otp/verify")) return "verify_password_reset_otp";
        if (path.Contains("/password/reset")) return "reset_password";
        if (path.Contains("/password/forgot")) return "forgot_password";
        if (path.Contains("/otp/email/send")) return "send_email_otp";
        if (path.Contains("/otp/sms/send")) return "send_sms_otp";
        if (path.Contains("/otp/email/verify")) return "verify_email_otp";
        if (path.Contains("/otp/sms/verify")) return "verify_sms_otp";
        if (path.Contains("/user/me")) return "profile";
        if (path.Contains("/my-plan")) return "my_plan";
        if (path.Contains("/admin/dashboard")) return "dashboard";
        if (path.Contains("/admin") && method == "DELETE") return "delete";
        if (path.Contains("/admin") && (method == "POST" || method == "PATCH" || method == "PUT")) return "save";
        if (path.Contains("/search")) return "search";

        return null;
    }
}
