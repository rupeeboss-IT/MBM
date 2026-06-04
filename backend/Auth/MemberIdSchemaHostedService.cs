using Microsoft.EntityFrameworkCore;
using RB_Website_API.Data;

namespace RB_Website_API.Auth;

public sealed class MemberIdSchemaHostedService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<MemberIdSchemaHostedService> _logger;

    public MemberIdSchemaHostedService(IServiceProvider services, ILogger<MemberIdSchemaHostedService> logger)
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
            IF OBJECT_ID('MemberIdSequences', 'U') IS NULL
            CREATE TABLE MemberIdSequences (
                [Year] int NOT NULL PRIMARY KEY,
                LastNumber int NOT NULL CONSTRAINT DF_MemberIdSequences_LastNumber DEFAULT 0
            );
            """,
            """
            IF COL_LENGTH('Users', 'MemberId') IS NULL
            ALTER TABLE Users ADD MemberId nvarchar(16) NULL;
            """,
            """
            IF COL_LENGTH('Users', 'MemberId') IS NOT NULL
               AND NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'UX_Users_MemberId'
                      AND object_id = OBJECT_ID('Users'))
            EXEC(N'CREATE UNIQUE INDEX UX_Users_MemberId ON Users(MemberId) WHERE MemberId IS NOT NULL;');
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
                _logger.LogWarning(ex, "Member ID schema update skipped or failed.");
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
