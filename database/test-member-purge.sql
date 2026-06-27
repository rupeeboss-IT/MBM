/*
  MBM Test Member Purge
  ---------------------
  MemberIds: MBM260005, MBM260006, MBM260007, MBM260008, MBM260010, MBM260011,
              MBM260018, MBM260021, MBM260022, MBM260023, MBM260024, MBM260025

  BEFORE RUNNING:
  1. Run the verification block at the bottom (read-only).
  2. Export CustomerReports.FilePath values and delete files from wwwroot after DB delete.
  3. Run once with @CommitChanges = 0 (dry run) — success message but NO data removed.
  4. Set @CommitChanges = 1 and run again to permanently delete.

  DOES NOT touch RBMAIN lead_data (separate database).
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

-- *** 0 = dry run (ROLLBACK).  1 = permanent delete (COMMIT). ***
DECLARE @CommitChanges bit = 0;
-- *** 1 = abort unless all 12 MemberIds exist.  0 = purge whatever exists (warn on missing). ***
DECLARE @RequireExactCount bit = 0;

BEGIN TRANSACTION;

DECLARE @TargetMemberIds TABLE (MemberId nvarchar(16) NOT NULL PRIMARY KEY);
INSERT INTO @TargetMemberIds (MemberId) VALUES
    (N'MBM260005'), (N'MBM260006'), (N'MBM260007'), (N'MBM260008'),
    (N'MBM260010'), (N'MBM260011'), (N'MBM260018'), (N'MBM260021'),
    (N'MBM260022'), (N'MBM260023'), (N'MBM260024'), (N'MBM260025');

DECLARE @UserIds TABLE (
    UserId   uniqueidentifier NOT NULL PRIMARY KEY,
    MemberId nvarchar(16)     NOT NULL
);

INSERT INTO @UserIds (UserId, MemberId)
SELECT u.UserId, u.MemberId
FROM dbo.Users AS u
INNER JOIN @TargetMemberIds AS t ON t.MemberId = u.MemberId;

DECLARE @ExpectedCount int = (SELECT COUNT(*) FROM @TargetMemberIds);
DECLARE @FoundCount    int = (SELECT COUNT(*) FROM @UserIds);

-- Show which MemberIds are missing (already deleted or never created)
SELECT t.MemberId AS MissingMemberId
FROM @TargetMemberIds AS t
WHERE NOT EXISTS (SELECT 1 FROM @UserIds AS u WHERE u.MemberId = t.MemberId);

IF @FoundCount = 0
BEGIN
    RAISERROR(N'No matching users found for any target MemberId. Nothing to purge.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END;

IF @RequireExactCount = 1 AND @FoundCount <> @ExpectedCount
BEGIN
    RAISERROR(N'Expected %d test users; found %d. Set @RequireExactCount = 0 to purge partial list.', 16, 1, @ExpectedCount, @FoundCount);
    ROLLBACK TRANSACTION;
    RETURN;
END;

IF @FoundCount <> @ExpectedCount
    PRINT CONCAT('Warning: expected ', @ExpectedCount, ' users, found ', @FoundCount, '. Proceeding with found users only.');

SELECT MemberId, UserId FROM @UserIds ORDER BY MemberId;

-- 1. Report change requests
DELETE rcr
FROM dbo.ReportChangeRequests AS rcr
INNER JOIN dbo.CustomerReports AS cr ON cr.Id = rcr.ReportId
INNER JOIN @UserIds AS u ON cr.CustomerId = u.UserId;

-- 2. Customer reports (export FilePath before delete for wwwroot cleanup)
DELETE cr
FROM dbo.CustomerReports AS cr
INNER JOIN @UserIds AS u ON cr.CustomerId = u.UserId;

-- 3. Scheme discovery
DELETE sdr
FROM dbo.SchemeDiscoveryRequests AS sdr
INNER JOIN @UserIds AS u ON sdr.UserId = u.UserId;

-- 4. Payments (FK → PaymentOrders; delete before orders)
DELETE pay
FROM dbo.Payments AS pay
INNER JOIN dbo.PaymentOrders AS po ON po.PaymentOrderId = pay.PaymentOrderId
INNER JOIN @UserIds AS u ON po.UserId = u.UserId;

-- 5. UserPlans (FK_UserPlans_PaymentOrders → must delete BEFORE PaymentOrders)
DELETE up
FROM dbo.UserPlans AS up
INNER JOIN @UserIds AS u ON up.UserId = u.UserId;

-- 6. Payment order children, then orders
DELETE por
FROM dbo.PaymentOrderReferrals AS por
INNER JOIN dbo.PaymentOrders AS po ON po.PaymentOrderId = por.PaymentOrderId
INNER JOIN @UserIds AS u ON po.UserId = u.UserId;

DELETE rlo
FROM dbo.ReferralLeadOutbox AS rlo
INNER JOIN @UserIds AS u ON rlo.UserId = u.UserId;

DELETE rlo
FROM dbo.ReferralLeadOutbox AS rlo
INNER JOIN dbo.PaymentOrders AS po ON po.PaymentOrderId = rlo.PaymentOrderId
INNER JOIN @UserIds AS u ON po.UserId = u.UserId;

DELETE po
FROM dbo.PaymentOrders AS po
INNER JOIN @UserIds AS u ON po.UserId = u.UserId;

-- 7. Registration leads
DELETE url
FROM dbo.UserRegistrationLeads AS url
INNER JOIN @UserIds AS u ON url.UserId = u.UserId;

-- 8. Connect module (FK-enforced)
DELETE cu
FROM dbo.ConnectContactUnlocks AS cu
INNER JOIN @UserIds AS u ON cu.ViewerUserId = u.UserId OR cu.TargetUserId = u.UserId;

DELETE creq
FROM dbo.ConnectRequests AS creq
INNER JOIN @UserIds AS u ON creq.FromUserId = u.UserId OR creq.ToUserId = u.UserId;

DELETE cal
FROM dbo.ConnectAdminListings AS cal
INNER JOIN @UserIds AS u ON cal.UserId = u.UserId;

DELETE cmp
FROM dbo.ConnectMemberProfiles AS cmp
INNER JOIN @UserIds AS u ON cmp.UserId = u.UserId;

-- 9. User audit (subject rows)
DELETE FROM dbo.UserAuditLog
WHERE UserId IN (SELECT UserId FROM @UserIds);

DELETE FROM dbo.UserStatusHistory
WHERE UserId IN (SELECT UserId FROM @UserIds);

DELETE FROM dbo.UserStatusAudit
WHERE TargetUserId IN (SELECT UserId FROM @UserIds);

-- 10. Optional diagnostics
DELETE FROM dbo.ApiExceptionLogs
WHERE UserId IN (SELECT UserId FROM @UserIds);

-- 11. Clear references pointing at test users
UPDATE dbo.ContactSubmissions
SET AssignedToUserId = NULL
WHERE AssignedToUserId IN (SELECT UserId FROM @UserIds);

UPDATE dbo.Users
SET CreatedByUserId = NULL
WHERE CreatedByUserId IN (SELECT UserId FROM @UserIds);

UPDATE dbo.ConnectAdminListings
SET CreatedByUserId = NULL
WHERE CreatedByUserId IN (SELECT UserId FROM @UserIds);

UPDATE dbo.Vendors
SET CreatedByUserId = NULL,
    DeletedByUserId = NULL
WHERE CreatedByUserId IN (SELECT UserId FROM @UserIds)
   OR DeletedByUserId IN (SELECT UserId FROM @UserIds);

UPDATE dbo.Users
SET DeletedByUserId = NULL
WHERE DeletedByUserId IN (SELECT UserId FROM @UserIds);

-- 12. Delete users
DELETE u
FROM dbo.Users AS u
INNER JOIN @UserIds AS ids ON u.UserId = ids.UserId;

DECLARE @DeletedUsers int = @@ROWCOUNT;
PRINT CONCAT('Deleted ', @DeletedUsers, ' user row(s) in this transaction.');

IF @CommitChanges = 1
BEGIN
    COMMIT TRANSACTION;
    PRINT '*** COMMITTED — test data permanently removed from database. ***';
END
ELSE
BEGIN
    ROLLBACK TRANSACTION;
    PRINT '*** ROLLED BACK — dry run only; database unchanged. ***';
    PRINT '    To apply for real: set @CommitChanges = 1 at top of script and re-run.';
END;

-- Verify (runs after commit/rollback — should return 0 rows when committed successfully)
SELECT t.MemberId, CASE WHEN u.UserId IS NULL THEN N'GONE' ELSE N'STILL EXISTS' END AS Status
FROM @TargetMemberIds AS t
LEFT JOIN dbo.Users AS u ON u.MemberId = t.MemberId
ORDER BY t.MemberId;

GO

/* ========== READ-ONLY: run separately before purge ========== */

/*
SELECT UserId, MemberId, Email, Phone, FullName, Role, IsDeleted, CreatedAt
FROM dbo.Users
WHERE MemberId IN (
    N'MBM260005', N'MBM260006', N'MBM260007', N'MBM260008', N'MBM260010', N'MBM260011',
    N'MBM260018', N'MBM260021', N'MBM260022', N'MBM260023', N'MBM260024', N'MBM260025'
)
ORDER BY MemberId;

;WITH TestUsers AS (
    SELECT UserId, MemberId, Email
    FROM dbo.Users
    WHERE MemberId IN (
        N'MBM260005', N'MBM260006', N'MBM260007', N'MBM260008', N'MBM260010', N'MBM260011',
        N'MBM260018', N'MBM260021', N'MBM260022', N'MBM260023', N'MBM260024', N'MBM260025'
    )
)
SELECT tu.MemberId, tu.Email,
       (SELECT COUNT(*) FROM dbo.UserPlans up WHERE up.UserId = tu.UserId) AS UserPlans,
       (SELECT COUNT(*) FROM dbo.PaymentOrders po WHERE po.UserId = tu.UserId) AS PaymentOrders,
       (SELECT COUNT(*) FROM dbo.Payments p
        JOIN dbo.PaymentOrders po ON po.PaymentOrderId = p.PaymentOrderId
        WHERE po.UserId = tu.UserId) AS Payments,
       (SELECT COUNT(*) FROM dbo.CustomerReports cr WHERE cr.CustomerId = tu.UserId) AS Reports
FROM TestUsers tu
ORDER BY tu.MemberId;

SELECT tu.MemberId, cr.FilePath, cr.OriginalFileName
FROM dbo.Users tu
JOIN dbo.CustomerReports cr ON cr.CustomerId = tu.UserId
WHERE tu.MemberId IN (
    N'MBM260005', N'MBM260006', N'MBM260007', N'MBM260008', N'MBM260010', N'MBM260011',
    N'MBM260018', N'MBM260021', N'MBM260022', N'MBM260023', N'MBM260024', N'MBM260025'
);
*/
