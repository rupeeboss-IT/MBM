-- MBM: Replace UTC datetime defaults with GETDATE() (server local time).
-- Safe to re-run; matches DateTimeDefaultsSchemaHostedService.

USE MBM;
GO

SET NOCOUNT ON;
BEGIN TRANSACTION;

ALTER TABLE dbo.Users DROP CONSTRAINT IF EXISTS DF_Users_ConsentAt;
ALTER TABLE dbo.Users ADD CONSTRAINT DF_Users_ConsentAt DEFAULT (GETDATE()) FOR ConsentAcceptedAt;

ALTER TABLE dbo.Users DROP CONSTRAINT IF EXISTS DF_Users_CreatedAt;
ALTER TABLE dbo.Users ADD CONSTRAINT DF_Users_CreatedAt DEFAULT (GETDATE()) FOR CreatedAt;

ALTER TABLE dbo.Users DROP CONSTRAINT IF EXISTS DF_Users_UpdatedAt;
ALTER TABLE dbo.Users ADD CONSTRAINT DF_Users_UpdatedAt DEFAULT (GETDATE()) FOR UpdatedAt;

ALTER TABLE dbo.OtpVerifications DROP CONSTRAINT IF EXISTS DF_Otp_CreatedAt;
ALTER TABLE dbo.OtpVerifications ADD CONSTRAINT DF_Otp_CreatedAt DEFAULT (GETDATE()) FOR CreatedAt;

ALTER TABLE dbo.Plans DROP CONSTRAINT IF EXISTS DF_Plans_CreatedAt;
ALTER TABLE dbo.Plans ADD CONSTRAINT DF_Plans_CreatedAt DEFAULT (GETDATE()) FOR CreatedAt;

ALTER TABLE dbo.Plans DROP CONSTRAINT IF EXISTS DF_Plans_UpdatedAt;
ALTER TABLE dbo.Plans ADD CONSTRAINT DF_Plans_UpdatedAt DEFAULT (GETDATE()) FOR UpdatedAt;

ALTER TABLE dbo.PaymentOrders DROP CONSTRAINT IF EXISTS DF_PaymentOrders_CreatedAt;
ALTER TABLE dbo.PaymentOrders ADD CONSTRAINT DF_PaymentOrders_CreatedAt DEFAULT (GETDATE()) FOR CreatedAt;

ALTER TABLE dbo.PaymentOrders DROP CONSTRAINT IF EXISTS DF_PaymentOrders_UpdatedAt;
ALTER TABLE dbo.PaymentOrders ADD CONSTRAINT DF_PaymentOrders_UpdatedAt DEFAULT (GETDATE()) FOR UpdatedAt;

ALTER TABLE dbo.Payments DROP CONSTRAINT IF EXISTS DF_Payments_PaidAt;
ALTER TABLE dbo.Payments ADD CONSTRAINT DF_Payments_PaidAt DEFAULT (GETDATE()) FOR PaidAt;

ALTER TABLE dbo.Payments DROP CONSTRAINT IF EXISTS DF_Payments_CreatedAt;
ALTER TABLE dbo.Payments ADD CONSTRAINT DF_Payments_CreatedAt DEFAULT (GETDATE()) FOR CreatedAt;

ALTER TABLE dbo.UserPlans DROP CONSTRAINT IF EXISTS DF_UserPlans_ActiveFrom;
ALTER TABLE dbo.UserPlans ADD CONSTRAINT DF_UserPlans_ActiveFrom DEFAULT (GETDATE()) FOR ActiveFrom;

ALTER TABLE dbo.UserPlans DROP CONSTRAINT IF EXISTS DF_UserPlans_CreatedAt;
ALTER TABLE dbo.UserPlans ADD CONSTRAINT DF_UserPlans_CreatedAt DEFAULT (GETDATE()) FOR CreatedAt;

ALTER TABLE dbo.UserPlans DROP CONSTRAINT IF EXISTS DF_UserPlans_UpdatedAt;
ALTER TABLE dbo.UserPlans ADD CONSTRAINT DF_UserPlans_UpdatedAt DEFAULT (GETDATE()) FOR UpdatedAt;

ALTER TABLE dbo.UserStatusAudit DROP CONSTRAINT IF EXISTS DF_UserStatusAudit_CreatedAt;
ALTER TABLE dbo.UserStatusAudit ADD CONSTRAINT DF_UserStatusAudit_CreatedAt DEFAULT (GETDATE()) FOR CreatedAt;

COMMIT TRANSACTION;
GO
