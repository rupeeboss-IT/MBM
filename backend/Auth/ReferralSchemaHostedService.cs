using Microsoft.EntityFrameworkCore;
using RB_Website_API.Data;

namespace RB_Website_API.Auth;

/// <summary>
/// Ensures referral extension tables exist in DB1 (no EF migrations in this project).
/// </summary>
public sealed class ReferralSchemaHostedService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ReferralSchemaHostedService> _logger;

    public ReferralSchemaHostedService(IServiceProvider services, ILogger<ReferralSchemaHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var scripts = new[]
        {
            """
            IF OBJECT_ID('PaymentOrderReferrals', 'U') IS NULL
            CREATE TABLE PaymentOrderReferrals (
                PaymentOrderId uniqueidentifier NOT NULL PRIMARY KEY,
                ReferralCode nvarchar(50) NULL,
                LeadPushedAt datetime2 NULL,
                CreatedAt datetime2 NOT NULL,
                UpdatedAt datetime2 NOT NULL
            );
            """,
            """
            IF COL_LENGTH('PaymentOrderReferrals', 'LeadPushedAt') IS NULL
            ALTER TABLE PaymentOrderReferrals ADD LeadPushedAt datetime2 NULL;
            """,
            """
            IF OBJECT_ID('ReferralLeadOutbox', 'U') IS NULL
            CREATE TABLE ReferralLeadOutbox (
                ReferralLeadOutboxId uniqueidentifier NOT NULL PRIMARY KEY,
                PaymentOrderId uniqueidentifier NOT NULL,
                UserId uniqueidentifier NOT NULL,
                PlanCode nvarchar(40) NOT NULL,
                ReferralCode nvarchar(50) NULL,
                AmountPaise bigint NOT NULL,
                Status nvarchar(20) NOT NULL,
                Attempts int NOT NULL,
                LastError nvarchar(1000) NULL,
                CreatedAt datetime2 NOT NULL,
                UpdatedAt datetime2 NOT NULL,
                NextAttemptAt datetime2 NULL
            );
            """,
            """
            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = 'IX_ReferralLeadOutbox_Status_NextAttempt'
                  AND object_id = OBJECT_ID('ReferralLeadOutbox'))
            CREATE INDEX IX_ReferralLeadOutbox_Status_NextAttempt
                ON ReferralLeadOutbox(Status, NextAttemptAt);
            """,
            """
            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = 'UX_ReferralLeadOutbox_PaymentOrderId'
                  AND object_id = OBJECT_ID('ReferralLeadOutbox'))
            CREATE UNIQUE INDEX UX_ReferralLeadOutbox_PaymentOrderId
                ON ReferralLeadOutbox(PaymentOrderId);
            """,
        };

        foreach (var sql in scripts)
        {
            try
            {
                await db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Referral schema update skipped or failed.");
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
