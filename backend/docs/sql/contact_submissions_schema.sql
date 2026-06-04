-- MBM: dbo.ContactSubmissions — public /contact form enquiries
USE MBM;
GO

IF OBJECT_ID(N'dbo.ContactSubmissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContactSubmissions (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        FullName nvarchar(160) NOT NULL,
        Phone nvarchar(10) NOT NULL,
        Email nvarchar(508) NOT NULL,
        SubjectId int NOT NULL,
        Message nvarchar(4000) NOT NULL,
        ConsentAccepted bit NOT NULL,
        ConsentAcceptedAt datetime2 NULL,
        CreatedAt datetime2 NOT NULL,
        ConfirmationEmailSentAt datetime2 NULL
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ContactSubmissions_CreatedAt'
      AND object_id = OBJECT_ID(N'dbo.ContactSubmissions'))
CREATE INDEX IX_ContactSubmissions_CreatedAt
    ON dbo.ContactSubmissions(CreatedAt DESC);
GO

-- SubjectId reference (application enum, not stored in DB):
--   1 Payment | 2 Loan Enquiry | 3 Government Scheme | 4 Company Formation
--   5 Technology Services | 6 Digital Marketing | 7 Membership | 8 Partnership
--   9 General Enquiry | 10 Other
