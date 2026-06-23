/*
  Seed ConnectAdminListings from existing Users (member / partner).
  Run AFTER connect-module-schema.sql.

  Safe to re-run: skips users who already have a ConnectAdminListings row.
*/

INSERT INTO dbo.ConnectAdminListings (
    ListingId,
    UserId,
    IsListed,
    IsVerified,
    IsActive,
    BusinessType,
    Sector,
    State,
    City,
    Turnover,
    Udyam,
    Employees,
    Description,
    Website,
    Established,
    SocialLinksJson,
    Remarks,
    CreatedByUserId,
    CreatedAt,
    UpdatedAt
)
SELECT
    NEWID(),
    u.UserId,
    1,                          -- IsListed: visible on /connect
    0,                          -- IsVerified: set manually in admin if needed
    1,                          -- IsActive
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    N'Auto-seeded from Users table',
    NULL,
    SYSUTCDATETIME(),
    SYSUTCDATETIME()
FROM dbo.Users u
WHERE u.IsDeleted = 0
  AND u.IsActive = 1
  AND LOWER(LTRIM(RTRIM(u.Role))) IN (N'member', N'partner')
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.ConnectAdminListings l
      WHERE l.UserId = u.UserId
  );

-- How many are now listed?
SELECT COUNT(*) AS ListedOnConnect
FROM dbo.ConnectAdminListings
WHERE IsListed = 1 AND IsActive = 1;
