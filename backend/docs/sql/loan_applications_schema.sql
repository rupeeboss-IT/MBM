-- MBM: dbo.LoanApplications — Id matches RBMAIN.dbo.lead_data.Lead_id (1:1 mapping).
--
-- Column layout (nvarchar Length in SSMS = bytes; char capacity = Length/2):
--   Id int PK (not identity)
--   FullName nvarchar(160), Phone nvarchar(10), Email nvarchar(508) NULL
--   Pincode nvarchar(6), LoanTypeId int, LoanAmount bigint
--   ConsentAccepted bit, ConsentAcceptedAt datetime2 NULL, CreatedAt datetime2
USE MBM;
GO

IF OBJECT_ID(N'dbo.LoanApplications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LoanApplications (
        Id int NOT NULL PRIMARY KEY,
        FullName nvarchar(160) NOT NULL,
        Phone nvarchar(10) NOT NULL,
        Email nvarchar(508) NULL,
        Pincode nvarchar(6) NOT NULL,
        LoanTypeId int NOT NULL,
        LoanAmount bigint NOT NULL,
        ConsentAccepted bit NOT NULL,
        ConsentAcceptedAt datetime2 NULL,
        CreatedAt datetime2 NOT NULL
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_LoanApplications_CreatedAt'
      AND object_id = OBJECT_ID(N'dbo.LoanApplications'))
CREATE INDEX IX_LoanApplications_CreatedAt
    ON dbo.LoanApplications(CreatedAt DESC);
GO
