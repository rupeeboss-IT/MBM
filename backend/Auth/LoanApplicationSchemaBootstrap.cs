using Microsoft.EntityFrameworkCore;
using RB_Website_API.Data;

namespace RB_Website_API.Auth;

/// <summary>
/// Ensures dbo.LoanApplications exists in the MBM database (AppDbContext).
/// </summary>
public static class LoanApplicationSchemaBootstrap
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

            var tableExists = await db.Database
                .SqlQueryRaw<int>(
                    """
                    SELECT CASE WHEN OBJECT_ID(N'dbo.LoanApplications', N'U') IS NOT NULL THEN 1 ELSE 0 END AS [Value]
                    """)
                .SingleAsync(ct);

            // 36 = uniqueidentifier (legacy); 56 = int (matches RBMAIN lead_data.Lead_id)
            // Cast system_type_id — SQL returns tinyint (maps to byte in .NET, not int).
            var idTypeId = tableExists == 0
                ? 0
                : await db.Database.SqlQueryRaw<int>(
                    """
                    SELECT CAST(t.system_type_id AS int) AS [Value]
                    FROM sys.columns c
                    INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
                    WHERE c.object_id = OBJECT_ID(N'dbo.LoanApplications')
                      AND c.name = N'Id'
                    """)
                    .SingleAsync(ct);

            if (tableExists == 1 && idTypeId == 36)
            {
                logger.LogWarning(
                    "Recreating dbo.LoanApplications: Id was uniqueidentifier; new schema uses int (RBMAIN lead_id). Existing rows are removed.");
                await db.Database.ExecuteSqlRawAsync("DROP TABLE dbo.LoanApplications;", ct);
                tableExists = 0;
            }

            if (tableExists == 0)
            {
                await db.Database.ExecuteSqlRawAsync(
                    """
                    CREATE TABLE dbo.LoanApplications (
                        Id int NOT NULL PRIMARY KEY,
                        FullName nvarchar(160) NOT NULL,
                        Phone nvarchar(10) NOT NULL,
                        Email nvarchar(508) NULL,
                        Pincode nvarchar(6) NOT NULL,
                        LoanTypeId int NOT NULL,
                        LoanAmount bigint NOT NULL,
                        ConsentAccepted bit NOT NULL,
                        ConsentAcceptedAt datetime2 NULL,
                        CreatedAt datetime2 NOT NULL
                    );
                    """,
                    ct);

                logger.LogInformation("Created dbo.LoanApplications in MBM (Id int, maps to lead_data.Lead_id).");
            }

            await db.Database.ExecuteSqlRawAsync(
                """
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = N'IX_LoanApplications_CreatedAt'
                      AND object_id = OBJECT_ID(N'dbo.LoanApplications'))
                CREATE INDEX IX_LoanApplications_CreatedAt
                    ON dbo.LoanApplications(CreatedAt DESC);
                """,
                ct);

            _ensured = true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to ensure dbo.LoanApplications exists in MBM.");
            throw;
        }
        finally
        {
            Gate.Release();
        }
    }
}
