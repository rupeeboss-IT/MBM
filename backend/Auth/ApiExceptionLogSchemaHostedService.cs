using Microsoft.EntityFrameworkCore;
using RB_Website_API.Data;

namespace RB_Website_API.Auth;

public sealed class ApiExceptionLogSchemaHostedService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ApiExceptionLogSchemaHostedService> _logger;

    public ApiExceptionLogSchemaHostedService(IServiceProvider services, ILogger<ApiExceptionLogSchemaHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        const string sql = """
            IF OBJECT_ID('ApiExceptionLogs', 'U') IS NULL
            CREATE TABLE ApiExceptionLogs (
                LogId uniqueidentifier NOT NULL PRIMARY KEY,
                CreatedAt datetime2 NOT NULL,
                HttpMethod nvarchar(16) NOT NULL,
                RequestPath nvarchar(500) NOT NULL,
                StatusCode int NOT NULL,
                UserId uniqueidentifier NULL,
                IpAddress nvarchar(64) NULL,
                OperationKey nvarchar(80) NULL,
                UserMessage nvarchar(500) NOT NULL,
                ExceptionType nvarchar(200) NOT NULL,
                ExceptionMessage nvarchar(max) NOT NULL,
                InnerExceptionMessage nvarchar(max) NULL,
                StackTrace nvarchar(max) NOT NULL
            );
            """;

        try
        {
            await db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ApiExceptionLogs schema update skipped or failed.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
