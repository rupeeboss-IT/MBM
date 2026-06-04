-- DB1 (mbm) — Referral extension tables
-- Run manually on production. Do not execute from the application in production if you prefer controlled migrations.

IF OBJECT_ID('PaymentOrderReferrals', 'U') IS NULL
BEGIN
    CREATE TABLE PaymentOrderReferrals (
        PaymentOrderId uniqueidentifier NOT NULL PRIMARY KEY,
        ReferralCode nvarchar(50) NULL,
        LeadPushedAt datetime2 NULL,
        CreatedAt datetime2 NOT NULL,
        UpdatedAt datetime2 NOT NULL
    );
END
GO

IF COL_LENGTH('PaymentOrderReferrals', 'LeadPushedAt') IS NULL
    ALTER TABLE PaymentOrderReferrals ADD LeadPushedAt datetime2 NULL;
GO

IF OBJECT_ID('ReferralLeadOutbox', 'U') IS NULL
BEGIN
    CREATE TABLE ReferralLeadOutbox (
        ReferralLeadOutboxId uniqueidentifier NOT NULL PRIMARY KEY,
        PaymentOrderId uniqueidentifier NOT NULL,
        UserId uniqueidentifier NOT NULL,
        PlanCode nvarchar(40) NOT NULL,
        ReferralCode nvarchar(50) NULL,
        AmountPaise bigint NOT NULL,
        Status nvarchar(20) NOT NULL,
        Attempts int NOT NULL,
        LastError nvarchar(1000) NULL,
        CreatedAt datetime2 NOT NULL,
        UpdatedAt datetime2 NOT NULL,
        NextAttemptAt datetime2 NULL
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_ReferralLeadOutbox_Status_NextAttempt'
      AND object_id = OBJECT_ID('ReferralLeadOutbox'))
CREATE INDEX IX_ReferralLeadOutbox_Status_NextAttempt
    ON ReferralLeadOutbox(Status, NextAttemptAt);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_ReferralLeadOutbox_PaymentOrderId'
      AND object_id = OBJECT_ID('ReferralLeadOutbox'))
CREATE UNIQUE INDEX UX_ReferralLeadOutbox_PaymentOrderId
    ON ReferralLeadOutbox(PaymentOrderId);
GO
