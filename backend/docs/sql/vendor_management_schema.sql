-- Vendor Management schema (manual execution only)
-- Run against the MBM application database before using Vendor Management APIs.

IF OBJECT_ID(N'dbo.Vendors', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Vendors (
        VendorId           UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Vendors PRIMARY KEY,
        ServiceName        NVARCHAR(200)    NOT NULL,
        CompanyName        NVARCHAR(200)    NOT NULL,
        ContactPersonName  NVARCHAR(160)    NOT NULL,
        Mobile             NVARCHAR(10)     NOT NULL,
        AlternateMobile    NVARCHAR(10)     NULL,
        Email              NVARCHAR(508)    NOT NULL,
        AlternateEmail     NVARCHAR(508)    NULL,
        Website            NVARCHAR(500)    NULL,
        Address            NVARCHAR(1000)   NULL,
        City               NVARCHAR(120)    NULL,
        State              NVARCHAR(120)    NULL,
        Country            NVARCHAR(120)    NULL,
        Pincode            NVARCHAR(10)     NULL,
        Remarks            NVARCHAR(2000)   NULL,
        IsActive           BIT              NOT NULL CONSTRAINT DF_Vendors_IsActive DEFAULT (1),
        IsDeleted          BIT              NOT NULL CONSTRAINT DF_Vendors_IsDeleted DEFAULT (0),
        DeletedAt          DATETIME         NULL,
        DeletedByUserId    UNIQUEIDENTIFIER NULL,
        CreatedAt          DATETIME         NOT NULL CONSTRAINT DF_Vendors_CreatedAt DEFAULT (GETDATE()),
        UpdatedAt          DATETIME         NOT NULL CONSTRAINT DF_Vendors_UpdatedAt DEFAULT (GETDATE()),
        CreatedByUserId    UNIQUEIDENTIFIER NULL
    );

    CREATE INDEX IX_Vendors_IsDeleted_IsActive ON dbo.Vendors (IsDeleted, IsActive);
    CREATE INDEX IX_Vendors_ServiceName ON dbo.Vendors (ServiceName);
    CREATE INDEX IX_Vendors_CompanyName ON dbo.Vendors (CompanyName);
END
GO

IF OBJECT_ID(N'dbo.VendorPlanMappings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.VendorPlanMappings (
        Id                UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_VendorPlanMappings PRIMARY KEY,
        VendorId          UNIQUEIDENTIFIER NOT NULL,
        PlanId            INT              NOT NULL,
        AssignedAt        DATETIME         NOT NULL CONSTRAINT DF_VendorPlanMappings_AssignedAt DEFAULT (GETDATE()),
        AssignedByUserId  UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT FK_VendorPlanMappings_Vendors FOREIGN KEY (VendorId) REFERENCES dbo.Vendors (VendorId),
        CONSTRAINT FK_VendorPlanMappings_Plans FOREIGN KEY (PlanId) REFERENCES dbo.Plans (PlanId),
        CONSTRAINT UQ_VendorPlanMappings_Vendor_Plan UNIQUE (VendorId, PlanId)
    );

    CREATE INDEX IX_VendorPlanMappings_PlanId ON dbo.VendorPlanMappings (PlanId);
END
GO

IF OBJECT_ID(N'dbo.VendorAuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.VendorAuditLogs (
        Id                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_VendorAuditLogs PRIMARY KEY,
        VendorId            UNIQUEIDENTIFIER NOT NULL,
        Action              NVARCHAR(40)     NOT NULL,
        PerformedByUserId   UNIQUEIDENTIFIER NOT NULL,
        PerformedOn         DATETIME         NOT NULL CONSTRAINT DF_VendorAuditLogs_PerformedOn DEFAULT (GETDATE()),
        PreviousValues      NVARCHAR(MAX)    NULL,
        NewValues           NVARCHAR(MAX)    NULL,
        Remarks             NVARCHAR(800)    NULL,
        CONSTRAINT FK_VendorAuditLogs_Vendors FOREIGN KEY (VendorId) REFERENCES dbo.Vendors (VendorId)
    );

    CREATE INDEX IX_VendorAuditLogs_VendorId_PerformedOn ON dbo.VendorAuditLogs (VendorId, PerformedOn DESC);
END
GO
