-- Enquiry Management schema (manual execution only)
-- Run against the MBM application database before using Enquiry Management APIs.
-- Reuses existing dbo.ContactSubmissions records; does not modify enquiry creation or email flows.

-- Extend ContactSubmissions for admin enquiry management
IF COL_LENGTH(N'dbo.ContactSubmissions', N'CompanyName') IS NULL
BEGIN
    ALTER TABLE dbo.ContactSubmissions
        ADD CompanyName NVARCHAR(200) NULL;
END
GO

IF COL_LENGTH(N'dbo.ContactSubmissions', N'Source') IS NULL
BEGIN
    ALTER TABLE dbo.ContactSubmissions
        ADD Source NVARCHAR(80) NOT NULL
            CONSTRAINT DF_ContactSubmissions_Source DEFAULT (N'Other Pages');
END
GO

IF COL_LENGTH(N'dbo.ContactSubmissions', N'Status') IS NULL
BEGIN
    ALTER TABLE dbo.ContactSubmissions
        ADD Status NVARCHAR(20) NOT NULL
            CONSTRAINT DF_ContactSubmissions_Status DEFAULT (N'New');
END
GO

IF COL_LENGTH(N'dbo.ContactSubmissions', N'AssignedToUserId') IS NULL
BEGIN
    ALTER TABLE dbo.ContactSubmissions
        ADD AssignedToUserId UNIQUEIDENTIFIER NULL;
END
GO

IF COL_LENGTH(N'dbo.ContactSubmissions', N'UpdatedAt') IS NULL
BEGIN
    ALTER TABLE dbo.ContactSubmissions
        ADD UpdatedAt DATETIME2 NULL;
END
GO

-- Backfill source for existing records (heuristic; safe for legacy data)
UPDATE dbo.ContactSubmissions
SET Source = N'Scheme Discovery Page'
WHERE (Source IS NULL OR Source = N'' OR Source = N'Other Pages')
  AND Email = N''
  AND SubjectId = 3;
GO

UPDATE dbo.ContactSubmissions
SET Source = N'Contact Us Page'
WHERE Source IS NULL OR Source = N'' OR Source = N'Other Pages';
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ContactSubmissions_Status_CreatedAt'
      AND object_id = OBJECT_ID(N'dbo.ContactSubmissions'))
BEGIN
    CREATE INDEX IX_ContactSubmissions_Status_CreatedAt
        ON dbo.ContactSubmissions (Status, CreatedAt DESC);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ContactSubmissions_Source'
      AND object_id = OBJECT_ID(N'dbo.ContactSubmissions'))
BEGIN
    CREATE INDEX IX_ContactSubmissions_Source
        ON dbo.ContactSubmissions (Source);
END
GO

-- Status change audit trail
IF OBJECT_ID(N'dbo.EnquiryStatusHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EnquiryStatusHistory (
        Id                   UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_EnquiryStatusHistory PRIMARY KEY,
        ContactSubmissionId  INT              NOT NULL,
        OldStatus            NVARCHAR(20)     NULL,
        NewStatus            NVARCHAR(20)     NOT NULL,
        ChangedByUserId      UNIQUEIDENTIFIER NOT NULL,
        ChangedOn            DATETIME2        NOT NULL CONSTRAINT DF_EnquiryStatusHistory_ChangedOn DEFAULT (GETDATE()),
        Remarks              NVARCHAR(800)    NULL,
        CONSTRAINT FK_EnquiryStatusHistory_ContactSubmissions
            FOREIGN KEY (ContactSubmissionId) REFERENCES dbo.ContactSubmissions (Id)
    );

    CREATE INDEX IX_EnquiryStatusHistory_ContactSubmissionId_ChangedOn
        ON dbo.EnquiryStatusHistory (ContactSubmissionId, ChangedOn DESC);
END
GO
