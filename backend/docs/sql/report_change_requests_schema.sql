-- Report Correction & Approval Workflow schema
-- Execute manually against the MBM database (DB1).
-- Does NOT run automatically from the application.

-- ─── ReportChangeRequests ───
IF OBJECT_ID('ReportChangeRequests', 'U') IS NULL
BEGIN
    CREATE TABLE ReportChangeRequests (
        Id uniqueidentifier NOT NULL PRIMARY KEY,
        ReportId uniqueidentifier NOT NULL,
        RequestType nvarchar(20) NOT NULL,
        RequestedBy uniqueidentifier NOT NULL,
        RequestedOn datetime2 NOT NULL,
        ApprovedBy uniqueidentifier NULL,
        ApprovedOn datetime2 NULL,
        RejectedBy uniqueidentifier NULL,
        RejectedOn datetime2 NULL,
        Reason nvarchar(2000) NOT NULL,
        Remarks nvarchar(2000) NULL,
        PreviousReportPath nvarchar(500) NULL,
        NewReportPath nvarchar(500) NULL,
        PreviousValues nvarchar(max) NULL,
        NewValues nvarchar(max) NULL,
        Status nvarchar(20) NOT NULL CONSTRAINT DF_ReportChangeRequests_Status DEFAULT 'Pending',
        PendingFileName nvarchar(260) NULL,
        PendingOriginalFileName nvarchar(260) NULL,
        PendingFileSize bigint NULL
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_ReportChangeRequests_ReportId_Status'
      AND object_id = OBJECT_ID('ReportChangeRequests'))
CREATE INDEX IX_ReportChangeRequests_ReportId_Status
    ON ReportChangeRequests(ReportId, Status);

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_ReportChangeRequests_Status_RequestedOn'
      AND object_id = OBJECT_ID('ReportChangeRequests'))
CREATE INDEX IX_ReportChangeRequests_Status_RequestedOn
    ON ReportChangeRequests(Status, RequestedOn DESC);

-- Prevent duplicate pending requests per report (filtered unique index)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_ReportChangeRequests_ReportId_Pending'
      AND object_id = OBJECT_ID('ReportChangeRequests'))
CREATE UNIQUE INDEX UX_ReportChangeRequests_ReportId_Pending
    ON ReportChangeRequests(ReportId)
    WHERE Status = 'Pending';

-- ─── Extend ReportAuditLogs for change-management audit trail ───
IF COL_LENGTH('ReportAuditLogs', 'RequestId') IS NULL
    ALTER TABLE ReportAuditLogs ADD RequestId uniqueidentifier NULL;

IF COL_LENGTH('ReportAuditLogs', 'Remarks') IS NULL
    ALTER TABLE ReportAuditLogs ADD Remarks nvarchar(2000) NULL;

IF COL_LENGTH('ReportAuditLogs', 'PreviousReportPath') IS NULL
    ALTER TABLE ReportAuditLogs ADD PreviousReportPath nvarchar(500) NULL;

IF COL_LENGTH('ReportAuditLogs', 'NewReportPath') IS NULL
    ALTER TABLE ReportAuditLogs ADD NewReportPath nvarchar(500) NULL;

IF COL_LENGTH('ReportAuditLogs', 'PreviousValues') IS NULL
    ALTER TABLE ReportAuditLogs ADD PreviousValues nvarchar(max) NULL;

IF COL_LENGTH('ReportAuditLogs', 'NewValues') IS NULL
    ALTER TABLE ReportAuditLogs ADD NewValues nvarchar(max) NULL;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_ReportAuditLogs_ReportId_CreatedAt'
      AND object_id = OBJECT_ID('ReportAuditLogs'))
CREATE INDEX IX_ReportAuditLogs_ReportId_CreatedAt
    ON ReportAuditLogs(ReportId, CreatedAt DESC);
