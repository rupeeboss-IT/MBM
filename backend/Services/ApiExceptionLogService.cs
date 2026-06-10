using RB_Website_API.Auth;
using RB_Website_API.Data;
using RB_Website_API.Models;

namespace RB_Website_API.Services;

public sealed class ApiExceptionLogService : IApiExceptionLogService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ApiExceptionLogService> _logger;

    public ApiExceptionLogService(IServiceScopeFactory scopeFactory, ILogger<ApiExceptionLogService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void LogInBackground(
        HttpContext context,
        Exception exception,
        int statusCode,
        string userMessage,
        string? operationKey)
    {
        var ex = exception;
        var method = context.Request.Method;
        var path = $"{context.Request.Path}{context.Request.QueryString}";
        var userId = CurrentUser.GetUserId(context.User);
        var ip = context.Connection.RemoteIpAddress?.ToString();
        var createdAt = DateTime.Now;

        _logger.LogError(
            exception,
            "API error {StatusCode} {Method} {Path} UserId={UserId} IP={IP} Operation={Operation} UserMessage={UserMessage}",
            statusCode,
            method,
            path,
            userId,
            ip,
            operationKey,
            userMessage);

        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var inner = ex.InnerException;
                db.ApiExceptionLogs.Add(new ApiExceptionLog
                {
                    LogId = Guid.NewGuid(),
                    CreatedAt = createdAt,
                    HttpMethod = method.Length > 16 ? method[..16] : method,
                    RequestPath = path.Length > 500 ? path[..500] : path,
                    StatusCode = statusCode,
                    UserId = userId,
                    IpAddress = ip,
                    OperationKey = operationKey,
                    UserMessage = userMessage.Length > 500 ? userMessage[..500] : userMessage,
                    ExceptionType = ex.GetType().FullName ?? ex.GetType().Name,
                    ExceptionMessage = ex.Message,
                    InnerExceptionMessage = inner?.Message,
                    StackTrace = ex.StackTrace ?? "",
                });

                await db.SaveChangesAsync();
            }
            catch (Exception logEx)
            {
                _logger.LogWarning(logEx, "Failed to persist ApiExceptionLog for {Path}", path);
            }
        });
    }
}
