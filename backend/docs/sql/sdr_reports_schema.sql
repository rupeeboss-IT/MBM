-- SDR (Scheme Discovery Report) records are stored in CustomerReports.
-- Auto-generated PDFs use ReportType = 'SDR' with a 365-day ExpiryDate.

IF COL_LENGTH('CustomerReports', 'ReportType') IS NULL
    ALTER TABLE CustomerReports ADD ReportType nvarchar(40) NOT NULL
        CONSTRAINT DF_CustomerReports_ReportType DEFAULT 'General';

IF COL_LENGTH('CustomerReports', 'SchemeDiscoveryRequestId') IS NULL
    ALTER TABLE CustomerReports ADD SchemeDiscoveryRequestId uniqueidentifier NULL;

IF COL_LENGTH('CustomerReports', 'ExpiryDate') IS NULL
    ALTER TABLE CustomerReports ADD ExpiryDate datetime2 NULL;

-- Valid SDR lookup: UserId + ReportType = 'SDR' + IsActive = 1 + ExpiryDate >= GETDATE()
