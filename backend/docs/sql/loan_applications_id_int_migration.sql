-- Migrate MBM.dbo.LoanApplications.Id from uniqueidentifier to int (RBMAIN lead_data.Lead_id).
-- WARNING: Drops the table when Id is uniqueidentifier. Back up first if you need legacy rows.

USE MBM;
GO

IF OBJECT_ID(N'dbo.LoanApplications', N'U') IS NOT NULL
AND EXISTS (
    SELECT 1
    FROM sys.columns c
    INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.LoanApplications')
      AND c.name = N'Id'
      AND t.system_type_id = 36)
BEGIN
    DROP TABLE dbo.LoanApplications;
END
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
