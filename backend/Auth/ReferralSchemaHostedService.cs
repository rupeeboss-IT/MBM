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
            """
            IF OBJECT_ID('UserRegistrationLeads', 'U') IS NULL
            CREATE TABLE UserRegistrationLeads (
                UserId uniqueidentifier NOT NULL PRIMARY KEY,
                RegistrationSource nvarchar(50) NOT NULL,
                AdvisorCode nvarchar(50) NULL,
                ResolvedEmpCode nvarchar(50) NULL,
                LeadType nvarchar(50) NULL,
                BrokerId int NULL,
                LeadId int NULL,
                UsedDefaultEmployee bit NOT NULL CONSTRAINT DF_UserRegistrationLeads_UsedDefaultEmployee DEFAULT (1),
                LeadPushedAt datetime2 NOT NULL,
                CreatedAt datetime2 NOT NULL,
                UpdatedAt datetime2 NOT NULL
            );
            """,
            """
            IF COL_LENGTH('UserRegistrationLeads', 'AdvisorCode') IS NULL
            ALTER TABLE UserRegistrationLeads ADD AdvisorCode nvarchar(50) NULL;
            """,
            """
            IF COL_LENGTH('UserRegistrationLeads', 'ResolvedEmpCode') IS NULL
            ALTER TABLE UserRegistrationLeads ADD ResolvedEmpCode nvarchar(50) NULL;
            """,
            """
            IF COL_LENGTH('UserRegistrationLeads', 'LeadType') IS NULL
            ALTER TABLE UserRegistrationLeads ADD LeadType nvarchar(50) NULL;
            """,
            """
            IF COL_LENGTH('UserRegistrationLeads', 'BrokerId') IS NULL
            ALTER TABLE UserRegistrationLeads ADD BrokerId int NULL;
            """,
            """
            IF COL_LENGTH('UserRegistrationLeads', 'LeadId') IS NULL
            ALTER TABLE UserRegistrationLeads ADD LeadId int NULL;
            """,
            """
            IF COL_LENGTH('UserRegistrationLeads', 'UsedDefaultEmployee') IS NULL
            ALTER TABLE UserRegistrationLeads ADD UsedDefaultEmployee bit NOT NULL
                CONSTRAINT DF_UserRegistrationLeads_UsedDefaultEmployee DEFAULT (1);
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
