-- Customer-facing Member ID (MBM + YY + 4-digit sequence per calendar year)
-- Run in SSMS / Azure Data Studio. Each GO starts a new batch (required for the index step).

IF OBJECT_ID('MemberIdSequences', 'U') IS NULL
CREATE TABLE MemberIdSequences (
    [Year] int NOT NULL PRIMARY KEY,
    LastNumber int NOT NULL CONSTRAINT DF_MemberIdSequences_LastNumber DEFAULT 0
);
GO

IF COL_LENGTH('dbo.Users', 'MemberId') IS NULL
    ALTER TABLE dbo.Users ADD MemberId nvarchar(16) NULL;
GO

-- Dynamic SQL: avoids "Invalid column name 'MemberId'" when the index is compiled in the same batch as ALTER TABLE.
IF COL_LENGTH('dbo.Users', 'MemberId') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = 'UX_Users_MemberId'
          AND object_id = OBJECT_ID('dbo.Users'))
BEGIN
    EXEC(N'CREATE UNIQUE INDEX UX_Users_MemberId ON dbo.Users(MemberId) WHERE MemberId IS NOT NULL;');
END
GO
