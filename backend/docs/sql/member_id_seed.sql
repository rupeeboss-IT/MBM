-- Seed MemberId for existing member accounts and sync MemberIdSequences.
-- Admins / superadmins stay NULL (no customer-facing Member ID).
-- Safe to re-run: only updates rows where MemberId IS NULL and role is member/partner.

SET NOCOUNT ON;

DECLARE @Year int = YEAR(GETDATE()); -- calendar year for sequence row (use 2026 if you prefer fixed year)
-- DECLARE @Year int = 2026;

IF COL_LENGTH('dbo.Users', 'MemberId') IS NULL
BEGIN
    RAISERROR('Column Users.MemberId does not exist. Run member_id_schema.sql first.', 16, 1);
    RETURN;
END

BEGIN TRANSACTION;

-- 1) Assign Member IDs to existing members (order: CreatedAt, then FullName)
;WITH ToAssign AS (
    SELECT
        u.UserId,
        ROW_NUMBER() OVER (ORDER BY u.CreatedAt, u.FullName) AS Seq
    FROM dbo.Users u
    WHERE u.MemberId IS NULL
      AND LOWER(LTRIM(RTRIM(u.Role))) IN ('member', 'partner')
)
UPDATE u
SET
    MemberId = 'MBM' + RIGHT('0' + CAST(@Year % 100 AS varchar(2)), 2)
             + RIGHT('0000' + CAST(t.Seq AS varchar(4)), 4),
    UpdatedAt = GETDATE()
FROM dbo.Users u
INNER JOIN ToAssign t ON t.UserId = u.UserId;

DECLARE @Assigned int = @@ROWCOUNT;

-- 2) Sync sequence table so the next registration gets the correct number
DECLARE @MaxSeq int = (
    SELECT ISNULL(MAX(TRY_CAST(RIGHT(MemberId, 4) AS int)), 0)
    FROM dbo.Users
    WHERE MemberId IS NOT NULL
      AND MemberId LIKE 'MBM[0-9][0-9][0-9][0-9][0-9][0-9]'
      AND TRY_CAST(SUBSTRING(MemberId, 4, 2) AS int) = @Year % 100
);

IF @MaxSeq > 0
BEGIN
    MERGE dbo.MemberIdSequences AS t
    USING (SELECT @Year AS [Year], @MaxSeq AS LastNumber) AS s
    ON t.[Year] = s.[Year]
    WHEN MATCHED AND t.LastNumber < s.LastNumber THEN
        UPDATE SET LastNumber = s.LastNumber
    WHEN NOT MATCHED THEN
        INSERT ([Year], LastNumber) VALUES (s.[Year], s.LastNumber);
END
ELSE IF NOT EXISTS (SELECT 1 FROM dbo.MemberIdSequences WHERE [Year] = @Year)
    INSERT INTO dbo.MemberIdSequences ([Year], LastNumber) VALUES (@Year, 0);

COMMIT TRANSACTION;

-- Optional: explicit IDs for known accounts (uncomment to force exact mapping)
/*
UPDATE dbo.Users SET MemberId = 'MBM260001', UpdatedAt = GETDATE()
WHERE UserId = 'BCF9CCAB-F300-4AD4-B75C-736299AA928A' AND LOWER(Role) = 'member';

UPDATE dbo.Users SET MemberId = 'MBM260002', UpdatedAt = GETDATE()
WHERE UserId = 'E6534E2E-FA92-48E2-A030-F6E719E12F8C' AND LOWER(Role) = 'member';

MERGE dbo.MemberIdSequences AS t
USING (SELECT 2026 AS [Year], 2 AS LastNumber) AS s
ON t.[Year] = s.[Year]
WHEN MATCHED THEN UPDATE SET LastNumber = s.LastNumber
WHEN NOT MATCHED THEN INSERT ([Year], LastNumber) VALUES (s.[Year], s.LastNumber);
*/

SELECT
    u.UserId,
    u.MemberId,
    u.Role,
    u.FullName,
    u.Email
FROM dbo.Users u
ORDER BY u.Role, u.MemberId, u.FullName;

SELECT [Year], LastNumber
FROM dbo.MemberIdSequences
ORDER BY [Year];

PRINT CONCAT('Assigned MemberId to ', @Assigned, ' member(s). Next ID will be MBM',
    RIGHT('0' + CAST(@Year % 100 AS varchar(2)), 2),
    RIGHT('0000' + CAST(ISNULL(@MaxSeq, 0) + 1 AS varchar(4)), 4), '.');
