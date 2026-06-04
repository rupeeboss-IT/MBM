using Microsoft.EntityFrameworkCore;
using RB_Website_API.Data;

namespace RB_Website_API.Auth;

public static class ContactSchemaBootstrap
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static volatile bool _ensured;

    public static async Task EnsureAsync(AppDbContext db, ILogger logger, CancellationToken ct = default)
    {
        if (_ensured) return;

        await Gate.WaitAsync(ct);
        try
        {
            if (_ensured) return;

            var exists = await db.Database
                .SqlQueryRaw<int>(
                    """
                    SELECT CASE WHEN OBJECT_ID(N'dbo.ContactSubmissions', N'U') IS NOT NULL THEN 1 ELSE 0 END AS [Value]
                    """)
                .SingleAsync(ct);

            if (exists == 0)
            {
                await db.Database.ExecuteSqlRawAsync(
                    """
                    CREATE TABLE dbo.ContactSubmissions (
                        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        FullName nvarchar(160) NOT NULL,
                        Phone nvarchar(10) NOT NULL,
                        Email nvarchar(508) NOT NULL,
                        SubjectId int NOT NULL,
                        Message nvarchar(4000) NOT NULL,
                        ConsentAccepted bit NOT NULL,
                        ConsentAcceptedAt datetime2 NULL,
                        CreatedAt datetime2 NOT NULL,
                        ConfirmationEmailSentAt datetime2 NULL
                    );
                    """,
                    ct);
                logger.LogInformation("Created dbo.ContactSubmissions in MBM.");
            }

            await db.Database.ExecuteSqlRawAsync(
                """
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = N'IX_ContactSubmissions_CreatedAt'
                      AND object_id = OBJECT_ID(N'dbo.ContactSubmissions'))
                CREATE INDEX IX_ContactSubmissions_CreatedAt
                    ON dbo.ContactSubmissions(CreatedAt DESC);
                """,
                ct);

            _ensured = true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to ensure dbo.ContactSubmissions exists in MBM.");
            throw;
        }
        finally
        {
            Gate.Release();
        }
    }
}
