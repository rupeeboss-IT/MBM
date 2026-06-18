using Microsoft.EntityFrameworkCore;
using RB_Website_API.Data;
using RB_Website_API.Models;

namespace RB_Website_API.Auth;

/// <summary>
/// Ensures scheme discovery tables and the one-time report plan exist (no EF migrations in this project).
/// </summary>
public sealed class SchemeDiscoveryBootstrapHostedService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<SchemeDiscoveryBootstrapHostedService> _logger;

    public SchemeDiscoveryBootstrapHostedService(
        IServiceProvider services,
        ILogger<SchemeDiscoveryBootstrapHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await EnsureSchemeDiscoveryRequestsTableAsync(db, cancellationToken);
        await EnsureCustomerReportSdrColumnsAsync(db, cancellationToken);
        await EnsureOneTimePlanAsync(db, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task EnsureSchemeDiscoveryRequestsTableAsync(AppDbContext db, CancellationToken ct)
    {
        const string sql = """
            IF OBJECT_ID('SchemeDiscoveryRequests', 'U') IS NULL
            CREATE TABLE SchemeDiscoveryRequests (
                Id uniqueidentifier NOT NULL PRIMARY KEY,
                UserId uniqueidentifier NOT NULL,
                MemberId nvarchar(64) NOT NULL,
                UserPlanId uniqueidentifier NOT NULL,
                UdyamNumber nvarchar(32) NOT NULL,
                PaymentId uniqueidentifier NULL,
                EntitlementType nvarchar(20) NOT NULL,
                Status nvarchar(20) NOT NULL,
                ExternalReference nvarchar(128) NULL,
                ErrorMessage nvarchar(1000) NULL,
                CustomerReportId uniqueidentifier NULL,
                RequestedAt datetime2 NOT NULL,
                CompletedAt datetime2 NULL,
                CreatedAt datetime2 NOT NULL,
                UpdatedAt datetime2 NOT NULL
            );
            """;

        try
        {
            await db.Database.ExecuteSqlRawAsync(sql, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SchemeDiscoveryRequests schema update skipped or failed.");
        }
    }

    private async Task EnsureCustomerReportSdrColumnsAsync(AppDbContext db, CancellationToken ct)
    {
        string[] scripts =
        [
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
        ];

        foreach (var sql in scripts)
        {
            try
            {
                await db.Database.ExecuteSqlRawAsync(sql, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CustomerReports SDR column update skipped or failed.");
            }
        }
    }

    private async Task EnsureOneTimePlanAsync(AppDbContext db, CancellationToken ct)
    {
        try
        {
            var code = SchemeDiscoveryCatalog.OneTimePlanCode;
            var existing = await db.Plans.FirstOrDefaultAsync(p => p.Code == code, ct);
            var now = DateTime.Now;

            if (existing is null)
            {
                db.Plans.Add(new Plan
                {
                    Code = code,
                    Name = "Government Scheme Discovery Report (One-Time)",
                    Description = "One-time purchase for a personalized government scheme eligibility report.",
                    BaseAmountPaise = 99_900,
                    GstPercent = 18m,
                    GstPaise = 17_982,
                    TotalAmountPaise = 117_882,
                    Currency = "INR",
                    DurationDays = 0,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
                await db.SaveChangesAsync(ct);
                _logger.LogInformation("Seeded plan {PlanCode}.", code);
                return;
            }

            var changed = false;
            if (!existing.IsActive)
            {
                existing.IsActive = true;
                changed = true;
            }
            if (existing.TotalAmountPaise != 117_882)
            {
                existing.BaseAmountPaise = 99_900;
                existing.GstPercent = 18m;
                existing.GstPaise = 17_982;
                existing.TotalAmountPaise = 117_882;
                changed = true;
            }
            if (string.IsNullOrWhiteSpace(existing.Name))
            {
                existing.Name = "Government Scheme Discovery Report (One-Time)";
                changed = true;
            }

            if (changed)
            {
                existing.UpdatedAt = now;
                await db.SaveChangesAsync(ct);
                _logger.LogInformation("Updated plan {PlanCode}.", code);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Scheme discovery one-time plan seed skipped or failed.");
        }
    }
}
