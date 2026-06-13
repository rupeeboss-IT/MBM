using Microsoft.EntityFrameworkCore;
using RB_Website_API.Data;

namespace RB_Website_API.Auth;

/// <summary>
/// Ensures customer report tables exist in DB1 (no EF migrations in this project).
/// </summary>
public sealed class CustomerReportSchemaHostedService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<CustomerReportSchemaHostedService> _logger;

    public CustomerReportSchemaHostedService(IServiceProvider services, ILogger<CustomerReportSchemaHostedService> logger)
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
            IF OBJECT_ID('CustomerReports', 'U') IS NULL
            CREATE TABLE CustomerReports (
                Id uniqueidentifier NOT NULL PRIMARY KEY,
                CustomerId uniqueidentifier NOT NULL,
                MemberId nvarchar(64) NOT NULL,
                ReportFileName nvarchar(260) NOT NULL,
                OriginalFileName nvarchar(260) NOT NULL,
                FilePath nvarchar(500) NOT NULL,
                FileSize bigint NOT NULL,
                UploadDate datetime2 NOT NULL,
                UploadedBy uniqueidentifier NOT NULL,
                SubscriptionId uniqueidentifier NOT NULL,
                IsActive bit NOT NULL CONSTRAINT DF_CustomerReports_IsActive DEFAULT 1,
                DownloadCount int NOT NULL CONSTRAINT DF_CustomerReports_DownloadCount DEFAULT 0,
                LastDownloadDate datetime2 NULL
            );
            """,
            """
            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = 'IX_CustomerReports_CustomerId_UploadDate'
                  AND object_id = OBJECT_ID('CustomerReports'))
            CREATE INDEX IX_CustomerReports_CustomerId_UploadDate
                ON CustomerReports(CustomerId, UploadDate DESC);
            """,
            """
            IF COL_LENGTH('CustomerReports', 'ReportType') IS NULL
                ALTER TABLE CustomerReports ADD ReportType nvarchar(40) NOT NULL
                    CONSTRAINT DF_CustomerReports_ReportType DEFAULT 'General';
            """,
            """
            IF COL_LENGTH('CustomerReports', 'SchemeDiscoveryRequestId') IS NULL
                ALTER TABLE CustomerReports ADD SchemeDiscoveryRequestId uniqueidentifier NULL;
            """,
            """
            IF COL_LENGTH('CustomerReports', 'ExpiryDate') IS NULL
                ALTER TABLE CustomerReports ADD ExpiryDate datetime2 NULL;
            """,
            """
            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = 'IX_CustomerReports_SdrLookup'
                  AND object_id = OBJECT_ID('CustomerReports'))
            CREATE INDEX IX_CustomerReports_SdrLookup
                ON CustomerReports(CustomerId, ReportType, IsActive, ExpiryDate DESC);
            """,
            """
            IF OBJECT_ID('ReportAuditLogs', 'U') IS NULL
            CREATE TABLE ReportAuditLogs (
                AuditId uniqueidentifier NOT NULL PRIMARY KEY,
                UserId uniqueidentifier NULL,
                Action nvarchar(40) NOT NULL,
                ReportId uniqueidentifier NULL,
                CustomerId uniqueidentifier NULL,
                CreatedAt datetime2 NOT NULL,
                IpAddress nvarchar(64) NULL
            );
            """,
            """
            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = 'IX_ReportAuditLogs_CreatedAt'
                  AND object_id = OBJECT_ID('ReportAuditLogs'))
            CREATE INDEX IX_ReportAuditLogs_CreatedAt
                ON ReportAuditLogs(CreatedAt DESC);
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
                _logger.LogWarning(ex, "Customer report schema update skipped or failed.");
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
