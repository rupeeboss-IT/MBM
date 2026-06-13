using Microsoft.EntityFrameworkCore;
using RB_Website_API.Data;

namespace RB_Website_API.Auth;

/// <summary>
/// Replaces sysutcdatetime()/getutcdate() column defaults with GETDATE() on MBM tables.
/// </summary>
public sealed class DateTimeDefaultsSchemaHostedService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<DateTimeDefaultsSchemaHostedService> _logger;

    public DateTimeDefaultsSchemaHostedService(
        IServiceProvider services,
        ILogger<DateTimeDefaultsSchemaHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var migrations = new (string Table, string Column, string Constraint)[]
        {
            ("Users", "ConsentAcceptedAt", "DF_Users_ConsentAt"),
            ("Users", "CreatedAt", "DF_Users_CreatedAt"),
            ("Users", "UpdatedAt", "DF_Users_UpdatedAt"),
            ("OtpVerifications", "CreatedAt", "DF_Otp_CreatedAt"),
            ("Plans", "CreatedAt", "DF_Plans_CreatedAt"),
            ("Plans", "UpdatedAt", "DF_Plans_UpdatedAt"),
            ("PaymentOrders", "CreatedAt", "DF_PaymentOrders_CreatedAt"),
            ("PaymentOrders", "UpdatedAt", "DF_PaymentOrders_UpdatedAt"),
            ("Payments", "PaidAt", "DF_Payments_PaidAt"),
            ("Payments", "CreatedAt", "DF_Payments_CreatedAt"),
            ("UserPlans", "ActiveFrom", "DF_UserPlans_ActiveFrom"),
            ("UserPlans", "CreatedAt", "DF_UserPlans_CreatedAt"),
            ("UserPlans", "UpdatedAt", "DF_UserPlans_UpdatedAt"),
            ("UserStatusAudit", "CreatedAt", "DF_UserStatusAudit_CreatedAt"),
        };

        foreach (var (table, column, constraint) in migrations)
        {
            var sql = $"""
                IF EXISTS (
                    SELECT 1
                    FROM sys.default_constraints dc
                    INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
                    INNER JOIN sys.tables t ON t.object_id = c.object_id
                    WHERE t.name = '{table}' AND c.name = '{column}'
                      AND (LOWER(dc.definition) LIKE '%sysutcdatetime%' OR LOWER(dc.definition) LIKE '%getutcdate%'))
                BEGIN
                    ALTER TABLE dbo.[{table}] DROP CONSTRAINT [{constraint}];
                    ALTER TABLE dbo.[{table}] ADD CONSTRAINT [{constraint}] DEFAULT (GETDATE()) FOR [{column}];
                END
                """;

            try
            {
                await db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "DateTime default migration skipped for {Table}.{Column}", table, column);
            }
        }

        _logger.LogInformation("DateTime column defaults verified (GETDATE()).");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
