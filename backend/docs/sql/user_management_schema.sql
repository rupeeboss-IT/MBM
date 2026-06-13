-- User Management & Role Management schema (manual execution only)
-- Run against the MBM application database (Users table).

-- ─── Users: soft delete & creator tracking ───────────────────────────────────
IF COL_LENGTH('dbo.Users', 'IsDeleted') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD IsDeleted BIT NOT NULL CONSTRAINT DF_Users_IsDeleted DEFAULT (0);
END
GO

IF COL_LENGTH('dbo.Users', 'DeletedAt') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD DeletedAt DATETIME2(7) NULL;
END
GO

IF COL_LENGTH('dbo.Users', 'DeletedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD DeletedByUserId UNIQUEIDENTIFIER NULL;
END
GO

IF COL_LENGTH('dbo.Users', 'CreatedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD CreatedByUserId UNIQUEIDENTIFIER NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_IsDeleted' AND object_id = OBJECT_ID('dbo.Users'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Users_IsDeleted ON dbo.Users (IsDeleted) INCLUDE (Role, IsActive);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_CreatedByUserId' AND object_id = OBJECT_ID('dbo.Users'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Users_CreatedByUserId ON dbo.Users (CreatedByUserId) WHERE CreatedByUserId IS NOT NULL;
END
GO

-- ─── UserStatusHistory: status change audit (never delete rows) ──────────────
IF OBJECT_ID('dbo.UserStatusHistory', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserStatusHistory (
        Id                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_UserStatusHistory PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        UserId              UNIQUEIDENTIFIER NOT NULL,
        UserType            NVARCHAR(40)     NOT NULL,
        ActionType          NVARCHAR(40)     NOT NULL,
        OldStatus           NVARCHAR(20)     NULL,
        NewStatus           NVARCHAR(20)     NULL,
        Remarks             NVARCHAR(800)    NULL,
        PerformedByUserId   UNIQUEIDENTIFIER NOT NULL,
        PerformedOn         DATETIME2(7)     NOT NULL CONSTRAINT DF_UserStatusHistory_PerformedOn DEFAULT (SYSUTCDATETIME())
    );

    CREATE NONCLUSTERED INDEX IX_UserStatusHistory_UserId ON dbo.UserStatusHistory (UserId, PerformedOn DESC);
    CREATE NONCLUSTERED INDEX IX_UserStatusHistory_PerformedBy ON dbo.UserStatusHistory (PerformedByUserId, PerformedOn DESC);
END
GO

-- ─── UserAuditLog: full action audit (never delete rows) ─────────────────────
IF OBJECT_ID('dbo.UserAuditLog', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserAuditLog (
        Id                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_UserAuditLog PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        UserId              UNIQUEIDENTIFIER NOT NULL,
        UserType            NVARCHAR(40)     NOT NULL,
        Action              NVARCHAR(40)     NOT NULL,
        PerformedByUserId   UNIQUEIDENTIFIER NOT NULL,
        PerformedOn         DATETIME2(7)     NOT NULL CONSTRAINT DF_UserAuditLog_PerformedOn DEFAULT (SYSUTCDATETIME()),
        PreviousValues      NVARCHAR(MAX)    NULL,
        NewValues           NVARCHAR(MAX)    NULL,
        Remarks             NVARCHAR(800)    NULL
    );

    CREATE NONCLUSTERED INDEX IX_UserAuditLog_UserId ON dbo.UserAuditLog (UserId, PerformedOn DESC);
    CREATE NONCLUSTERED INDEX IX_UserAuditLog_Action ON dbo.UserAuditLog (Action, PerformedOn DESC);
END
GO
