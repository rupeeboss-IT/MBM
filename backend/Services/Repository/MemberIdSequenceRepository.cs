using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RB_Website_API.Data;
using RB_Website_API.Services.IRepository;

namespace RB_Website_API.Services.Repository;

public sealed class MemberIdSequenceRepository : IMemberIdSequenceRepository
{
    private readonly AppDbContext _db;

    public MemberIdSequenceRepository(AppDbContext db) => _db = db;

    public async Task<int> GetNextSequenceNumberAsync(int year, CancellationToken ct)
    {
        // IMPORTANT:
        // Do not dispose the DbContext's underlying connection. Disposing it can complete/tear down
        // an active EF Core transaction, leading to: "This SqlTransaction has completed; it is no longer usable."
        var conn = _db.Database.GetDbConnection();
        var openedHere = false;
        if (conn.State != System.Data.ConnectionState.Open)
        {
            await conn.OpenAsync(ct);
            openedHere = true;
        }

        await using var cmd = conn.CreateCommand();
        var efTx = _db.Database.CurrentTransaction;
        if (efTx is not null)
            cmd.Transaction = efTx.GetDbTransaction();

        cmd.CommandText = """
            DECLARE @NextTable TABLE (NextVal int);

            MERGE MemberIdSequences WITH (HOLDLOCK) AS t
            USING (SELECT @Year AS [Year]) AS s
            ON t.[Year] = s.[Year]
            WHEN MATCHED THEN
                UPDATE SET LastNumber = t.LastNumber + 1
            WHEN NOT MATCHED THEN
                INSERT ([Year], LastNumber) VALUES (s.[Year], 1)
            OUTPUT INSERTED.LastNumber INTO @NextTable(NextVal);

            SELECT TOP 1 NextVal FROM @NextTable;
            """;
        cmd.Parameters.Add(new SqlParameter("@Year", year));

        var scalar = await cmd.ExecuteScalarAsync(ct);
        if (scalar is null or DBNull)
            throw new InvalidOperationException("Failed to allocate member ID sequence.");

        var next = Convert.ToInt32(scalar);

        // If we opened the connection outside any EF transaction, close it to avoid holding it.
        if (openedHere && efTx is null)
            await conn.CloseAsync();

        return next;
    }
}
