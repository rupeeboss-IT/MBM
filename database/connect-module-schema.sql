/*
  MBM Connect Module — run manually on MBM database.
  Does NOT alter any existing tables. Creates new tables only.

  Prerequisites: dbo.Users must exist with UserId (uniqueidentifier PK).
*/

-- ---------------------------------------------------------------------------
-- 1. Customer-owned Connect profile (member/partner edits via app)
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.ConnectMemberProfiles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConnectMemberProfiles (
        UserId           uniqueidentifier NOT NULL,
        Designation      nvarchar(120)    NULL,
        BusinessType     nvarchar(80)     NULL,
        Sector           nvarchar(120)    NULL,
        State            nvarchar(120)    NULL,
        City             nvarchar(120)    NULL,
        Turnover         nvarchar(80)     NULL,
        Udyam            nvarchar(40)     NULL,
        Employees        nvarchar(40)     NULL,
        Description      nvarchar(2000)   NULL,
        Website          nvarchar(500)    NULL,
        Established      nvarchar(10)     NULL,
        SocialLinksJson  nvarchar(2000)   NULL,
        CreatedAt        datetime2        NOT NULL CONSTRAINT DF_ConnectMemberProfiles_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt        datetime2        NOT NULL CONSTRAINT DF_ConnectMemberProfiles_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_ConnectMemberProfiles PRIMARY KEY (UserId),
        CONSTRAINT FK_ConnectMemberProfiles_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId)
    );
END;
GO

-- ---------------------------------------------------------------------------
-- 2. Admin-owned listing (admin adds for existing member/partner only)
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.ConnectAdminListings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConnectAdminListings (
        ListingId        uniqueidentifier NOT NULL,
        UserId           uniqueidentifier NOT NULL,
        IsListed         bit              NOT NULL CONSTRAINT DF_ConnectAdminListings_IsListed DEFAULT (0),
        IsVerified       bit              NOT NULL CONSTRAINT DF_ConnectAdminListings_IsVerified DEFAULT (0),
        IsActive         bit              NOT NULL CONSTRAINT DF_ConnectAdminListings_IsActive DEFAULT (1),
        BusinessType     nvarchar(80)     NULL,
        Sector           nvarchar(120)    NULL,
        State            nvarchar(120)    NULL,
        City             nvarchar(120)    NULL,
        Turnover         nvarchar(80)     NULL,
        Udyam            nvarchar(40)     NULL,
        Employees        nvarchar(40)     NULL,
        Description      nvarchar(2000)   NULL,
        Website          nvarchar(500)    NULL,
        Established      nvarchar(10)     NULL,
        SocialLinksJson  nvarchar(2000)   NULL,
        Remarks          nvarchar(2000)   NULL,
        CreatedByUserId  uniqueidentifier NULL,
        CreatedAt        datetime2        NOT NULL CONSTRAINT DF_ConnectAdminListings_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt        datetime2        NOT NULL CONSTRAINT DF_ConnectAdminListings_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_ConnectAdminListings PRIMARY KEY (ListingId),
        CONSTRAINT FK_ConnectAdminListings_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId),
        CONSTRAINT FK_ConnectAdminListings_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (UserId)
    );
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_ConnectAdminListings_UserId'
      AND object_id = OBJECT_ID(N'dbo.ConnectAdminListings'))
BEGIN
    CREATE UNIQUE INDEX UX_ConnectAdminListings_UserId
        ON dbo.ConnectAdminListings (UserId);
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ConnectAdminListings_IsListed'
      AND object_id = OBJECT_ID(N'dbo.ConnectAdminListings'))
BEGIN
    CREATE INDEX IX_ConnectAdminListings_IsListed
        ON dbo.ConnectAdminListings (IsListed, IsActive)
        INCLUDE (UserId, Sector, State, City);
END;
GO

-- ---------------------------------------------------------------------------
-- 3. Connection requests (LinkedIn-style)
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.ConnectRequests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConnectRequests (
        RequestId    uniqueidentifier NOT NULL,
        FromUserId   uniqueidentifier NOT NULL,
        ToUserId     uniqueidentifier NOT NULL,
        Status       nvarchar(20)     NOT NULL,
        Message      nvarchar(500)    NULL,
        CreatedAt    datetime2        NOT NULL CONSTRAINT DF_ConnectRequests_CreatedAt DEFAULT (SYSUTCDATETIME()),
        RespondedAt  datetime2        NULL,
        CONSTRAINT PK_ConnectRequests PRIMARY KEY (RequestId),
        CONSTRAINT FK_ConnectRequests_FromUser FOREIGN KEY (FromUserId) REFERENCES dbo.Users (UserId),
        CONSTRAINT FK_ConnectRequests_ToUser FOREIGN KEY (ToUserId) REFERENCES dbo.Users (UserId),
        CONSTRAINT CK_ConnectRequests_Status CHECK (Status IN (N'Pending', N'Connected', N'Rejected'))
    );
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ConnectRequests_FromUser'
      AND object_id = OBJECT_ID(N'dbo.ConnectRequests'))
BEGIN
    CREATE INDEX IX_ConnectRequests_FromUser
        ON dbo.ConnectRequests (FromUserId, Status);
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ConnectRequests_ToUser'
      AND object_id = OBJECT_ID(N'dbo.ConnectRequests'))
BEGIN
    CREATE INDEX IX_ConnectRequests_ToUser
        ON dbo.ConnectRequests (ToUserId, Status);
END;
GO

-- ---------------------------------------------------------------------------
-- 4. Lifetime contact unlocks (Basic / Standard — max 5)
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.ConnectContactUnlocks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConnectContactUnlocks (
        ViewerUserId  uniqueidentifier NOT NULL,
        TargetUserId  uniqueidentifier NOT NULL,
        UnlockedAt    datetime2        NOT NULL CONSTRAINT DF_ConnectContactUnlocks_UnlockedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_ConnectContactUnlocks PRIMARY KEY (ViewerUserId, TargetUserId),
        CONSTRAINT FK_ConnectContactUnlocks_Viewer FOREIGN KEY (ViewerUserId) REFERENCES dbo.Users (UserId),
        CONSTRAINT FK_ConnectContactUnlocks_Target FOREIGN KEY (TargetUserId) REFERENCES dbo.Users (UserId)
    );
END;
GO
