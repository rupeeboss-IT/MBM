/*
  MBM Credit Rebuild — run manually on the MBM database.

  Stores credit rebuild enquiry submissions. Id matches RBMAIN.dbo.lead_data.Lead_id
  (inserted in the same transaction by the API). Does NOT create or alter RBMAIN tables.

  Prerequisites:
    - Database: MBM
    - RBMAIN.dbo.lead_data must already exist (used by the API for CRM lead insert)
*/

-- ---------------------------------------------------------------------------
-- Credit rebuild enquiries (MBM only)
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.CreditRebuildEnquiries', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CreditRebuildEnquiries (
        Id                int            NOT NULL,
        FullName          nvarchar(160)  NOT NULL,
        Phone             nvarchar(10)   NOT NULL,
        Email             nvarchar(508)  NOT NULL,
        AdvisorCode       nvarchar(50)   NULL,
        ConsentAccepted   bit            NOT NULL,
        ConsentAcceptedAt datetime2      NULL,
        CreatedAt         datetime2      NOT NULL,
        CONSTRAINT PK_CreditRebuildEnquiries PRIMARY KEY (Id)
    );
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_CreditRebuildEnquiries_CreatedAt'
      AND object_id = OBJECT_ID(N'dbo.CreditRebuildEnquiries')
)
BEGIN
    CREATE INDEX IX_CreditRebuildEnquiries_CreatedAt
        ON dbo.CreditRebuildEnquiries (CreatedAt DESC);
END;
GO
