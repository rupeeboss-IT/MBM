-- Explicit Member ID seed for known Users (run after member_id_schema.sql)
-- Admins / superadmin: MemberId stays NULL

SET NOCOUNT ON;

IF COL_LENGTH('dbo.Users', 'MemberId') IS NULL
BEGIN
    RAISERROR('Run member_id_schema.sql first.', 16, 1);
    RETURN;
END

BEGIN TRANSACTION;

-- Members only
UPDATE dbo.Users
SET MemberId = 'MBM260001', UpdatedAt = GETUTCDATE()
WHERE UserId = 'BCF9CCAB-F300-4AD4-B75C-736299AA928A'
  AND LOWER(LTRIM(RTRIM(Role))) = 'member';

UPDATE dbo.Users
SET MemberId = 'MBM260002', UpdatedAt = GETUTCDATE()
WHERE UserId = 'E6534E2E-FA92-48E2-A030-F6E719E12F8C'
  AND LOWER(LTRIM(RTRIM(Role))) = 'member';

-- Rashid (admin) and Pravin (superadmin): leave MemberId NULL — no UPDATE

-- Sequence: next new registration in 2026 gets MBM260003
MERGE dbo.MemberIdSequences AS t
USING (SELECT 2026 AS [Year], 2 AS LastNumber) AS s
ON t.[Year] = s.[Year]
WHEN MATCHED THEN
    UPDATE SET LastNumber = CASE WHEN t.LastNumber < s.LastNumber THEN s.LastNumber ELSE t.LastNumber END
WHEN NOT MATCHED THEN
    INSERT ([Year], LastNumber) VALUES (s.[Year], s.LastNumber);

COMMIT TRANSACTION;

SELECT UserId, MemberId, Role, FullName, Email
FROM dbo.Users
ORDER BY Role, MemberId;

SELECT [Year], LastNumber FROM dbo.MemberIdSequences;
