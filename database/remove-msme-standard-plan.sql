/*
  Retire MSME Standard membership plan (Code = 'standard')
  ---------------------------------------------------------------------------
  Run against your MBM SQL Server database after deploying the app changes.

  Strategy:
    - Deactivate the plan (IsActive = 0) so new checkouts cannot select it.
    - Keep Plans / UserPlans / PaymentOrders rows for billing history and
      existing subscribers until their term ends.
    - Remove VendorPlanMappings so vendors are not assigned to a retired plan.

  Optional: If you have active Standard subscribers to migrate, see section 5.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

-- ---------------------------------------------------------------------------
-- 1. Preview — confirm the plan row exists
-- ---------------------------------------------------------------------------
SELECT
    PlanId,
    Code,
    Name,
    IsActive,
    BaseAmountPaise,
    GstPaise,
    TotalAmountPaise,
    DurationDays
FROM dbo.Plans
WHERE Code = N'standard';

-- ---------------------------------------------------------------------------
-- 2. Preview — active subscribers (informational)
-- ---------------------------------------------------------------------------
SELECT
    up.UserPlanId,
    up.UserId,
    up.PlanCode,
    up.Status,
    up.ActiveFrom,
    up.ActiveTo
FROM dbo.UserPlans AS up
WHERE up.PlanCode = N'standard'
  AND up.Status = N'Active';

-- ---------------------------------------------------------------------------
-- 3. Deactivate plan — blocks new purchases via API (IsActive = 1 filter)
-- ---------------------------------------------------------------------------
UPDATE dbo.Plans
SET
    IsActive  = 0,
    UpdatedAt = SYSUTCDATETIME()
WHERE Code = N'standard';

IF @@ROWCOUNT = 0
BEGIN
    RAISERROR(N'No Plans row found with Code = standard. Nothing was updated.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END;

-- ---------------------------------------------------------------------------
-- 4. Remove vendor-to-plan assignments for the retired plan
-- ---------------------------------------------------------------------------
DELETE vpm
FROM dbo.VendorPlanMappings AS vpm
INNER JOIN dbo.Plans AS p ON p.PlanId = vpm.PlanId
WHERE p.Code = N'standard';

-- ---------------------------------------------------------------------------
-- 5. OPTIONAL — migrate active Standard subscribers to Basic
--    Uncomment only if you want to move everyone off Standard immediately.
--    Adjust target plan Code if your Basic plan uses a different code.
-- ---------------------------------------------------------------------------
/*
DECLARE @BasicPlanId int = (
    SELECT TOP (1) PlanId FROM dbo.Plans WHERE Code = N'basic' AND IsActive = 1
);

IF @BasicPlanId IS NULL
BEGIN
    RAISERROR(N'Active basic plan not found. Aborting migration.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END;

UPDATE up
SET
    PlanId    = @BasicPlanId,
    PlanCode  = N'basic',
    UpdatedAt = SYSUTCDATETIME()
FROM dbo.UserPlans AS up
WHERE up.PlanCode = N'standard'
  AND up.Status = N'Active';
*/

-- ---------------------------------------------------------------------------
-- 6. Verify
-- ---------------------------------------------------------------------------
SELECT PlanId, Code, Name, IsActive, UpdatedAt
FROM dbo.Plans
WHERE Code = N'standard';

SELECT COUNT(*) AS RemainingVendorMappings
FROM dbo.VendorPlanMappings AS vpm
INNER JOIN dbo.Plans AS p ON p.PlanId = vpm.PlanId
WHERE p.Code = N'standard';

COMMIT TRANSACTION;

PRINT N'MSME Standard plan deactivated successfully.';
