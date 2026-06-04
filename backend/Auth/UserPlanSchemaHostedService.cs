using Microsoft.EntityFrameworkCore;
using RB_Website_API.Data;

namespace RB_Website_API.Auth;

/// <summary>
/// Ensures UserPlans subscription columns exist (no EF migrations in this project).
/// </summary>
public sealed class UserPlanSchemaHostedService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<UserPlanSchemaHostedService> _logger;

    public UserPlanSchemaHostedService(IServiceProvider services, ILogger<UserPlanSchemaHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var alters = new[]
        {
            "IF COL_LENGTH('UserPlans', 'CancelAtPeriodEnd') IS NULL ALTER TABLE UserPlans ADD CancelAtPeriodEnd bit NOT NULL CONSTRAINT DF_UserPlans_CancelAtPeriodEnd DEFAULT 0;",
            "IF COL_LENGTH('UserPlans', 'CancelledAt') IS NULL ALTER TABLE UserPlans ADD CancelledAt datetime2 NULL;",
            "IF COL_LENGTH('UserPlans', 'AutoRenewEnabled') IS NULL ALTER TABLE UserPlans ADD AutoRenewEnabled bit NOT NULL CONSTRAINT DF_UserPlans_AutoRenew DEFAULT 0;",
            "IF COL_LENGTH('UserPlans', 'RazorpaySubscriptionId') IS NULL ALTER TABLE UserPlans ADD RazorpaySubscriptionId nvarchar(64) NULL;",
            "IF COL_LENGTH('UserPlans', 'RazorpayCustomerId') IS NULL ALTER TABLE UserPlans ADD RazorpayCustomerId nvarchar(64) NULL;",
            """
            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = 'UX_Payments_RazorpayPaymentId' AND object_id = OBJECT_ID('Payments'))
            CREATE UNIQUE INDEX UX_Payments_RazorpayPaymentId ON Payments(RazorpayPaymentId);
            """,
        };

        foreach (var sql in alters)
        {
            try
            {
                await db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "UserPlans schema update skipped or failed: {Sql}", sql);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
