-- Lead Source Tracking schema (REFERENCE ONLY — manual execution)

-- Attribution is computed at runtime from Users, PaymentOrders, PaymentOrderReferrals,

-- UserPlans, CustomerReports, and referral DB employee/broker masters.

-- No tables are required for the module to work; run this only if you want persisted snapshots.



IF OBJECT_ID(N'dbo.LeadCustomerAttributions', N'U') IS NULL

BEGIN

    CREATE TABLE dbo.LeadCustomerAttributions (

        UserId               UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_LeadCustomerAttributions PRIMARY KEY,

        MemberId             NVARCHAR(16)     NULL,

        SourceType           NVARCHAR(40)     NOT NULL,

        SourceUserId         UNIQUEIDENTIFIER NULL,

        SourceCode           NVARCHAR(50)     NULL,

        SourceName           NVARCHAR(200)    NULL,

        AssignedEmployeeId   INT              NULL,

        AssignedPartnerId    UNIQUEIDENTIFIER NULL,

        AssignedAdvisorName  NVARCHAR(200)    NULL,

        CreatedThrough       NVARCHAR(80)     NULL,

        FirstPaymentOrderId  UNIQUEIDENTIFIER NULL,

        FirstReferralCode    NVARCHAR(50)     NULL,

        FirstPaidPlanCode    NVARCHAR(40)     NULL,

        FirstPaidPlanName    NVARCHAR(120)    NULL,

        MembershipSalesCount INT              NOT NULL CONSTRAINT DF_LeadCustomerAttributions_MembershipSalesCount DEFAULT (0),

        ReportPurchaseCount  INT              NOT NULL CONSTRAINT DF_LeadCustomerAttributions_ReportPurchaseCount DEFAULT (0),

        ReportGeneratedCount INT              NOT NULL CONSTRAINT DF_LeadCustomerAttributions_ReportGeneratedCount DEFAULT (0),

        ComputedAt           DATETIME         NOT NULL CONSTRAINT DF_LeadCustomerAttributions_ComputedAt DEFAULT (GETDATE()),

        CONSTRAINT FK_LeadCustomerAttributions_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId)

    );



    CREATE INDEX IX_LeadCustomerAttributions_SourceType ON dbo.LeadCustomerAttributions (SourceType);

    CREATE INDEX IX_LeadCustomerAttributions_SourceCode ON dbo.LeadCustomerAttributions (SourceCode);

    CREATE INDEX IX_LeadCustomerAttributions_MemberId ON dbo.LeadCustomerAttributions (MemberId);

    CREATE INDEX IX_LeadCustomerAttributions_AssignedEmployeeId ON dbo.LeadCustomerAttributions (AssignedEmployeeId);

    CREATE INDEX IX_LeadCustomerAttributions_AssignedPartnerId ON dbo.LeadCustomerAttributions (AssignedPartnerId);

END

GO



-- Optional payment history snapshot (denormalized from PaymentOrders + PaymentOrderReferrals).

IF OBJECT_ID(N'dbo.LeadCustomerPaymentHistory', N'U') IS NULL

BEGIN

    CREATE TABLE dbo.LeadCustomerPaymentHistory (

        Id               UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_LeadCustomerPaymentHistory PRIMARY KEY,

        UserId           UNIQUEIDENTIFIER NOT NULL,

        PaymentOrderId   UNIQUEIDENTIFIER NOT NULL,

        PlanCode         NVARCHAR(40)     NOT NULL,

        PlanName         NVARCHAR(120)    NOT NULL,

        OrderType        NVARCHAR(20)     NOT NULL,  -- Membership | Report

        ReferralCode     NVARCHAR(50)     NULL,

        TotalAmountPaise BIGINT           NOT NULL,

        PaidAt           DATETIME         NOT NULL,

        CreatedAt        DATETIME         NOT NULL CONSTRAINT DF_LeadCustomerPaymentHistory_CreatedAt DEFAULT (GETDATE()),

        CONSTRAINT FK_LeadCustomerPaymentHistory_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId),

        CONSTRAINT FK_LeadCustomerPaymentHistory_PaymentOrders FOREIGN KEY (PaymentOrderId) REFERENCES dbo.PaymentOrders (PaymentOrderId)

    );



    CREATE INDEX IX_LeadCustomerPaymentHistory_UserId_PaidAt ON dbo.LeadCustomerPaymentHistory (UserId, PaidAt DESC);

    CREATE INDEX IX_LeadCustomerPaymentHistory_OrderType ON dbo.LeadCustomerPaymentHistory (OrderType);

END

GO



-- Optional UTM / campaign capture for future Organic / Campaign attribution (registration/checkout extension point).

IF OBJECT_ID(N'dbo.LeadSourceEvents', N'U') IS NULL

BEGIN

    CREATE TABLE dbo.LeadSourceEvents (

        Id            UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_LeadSourceEvents PRIMARY KEY,

        UserId        UNIQUEIDENTIFIER NULL,

        EventType     NVARCHAR(40)     NOT NULL,

        SourceType    NVARCHAR(40)     NULL,

        SourceCode    NVARCHAR(50)     NULL,

        UtmSource     NVARCHAR(120)    NULL,

        UtmMedium     NVARCHAR(120)    NULL,

        UtmCampaign   NVARCHAR(120)    NULL,

        LandingPath   NVARCHAR(500)    NULL,

        CreatedAt     DATETIME         NOT NULL CONSTRAINT DF_LeadSourceEvents_CreatedAt DEFAULT (GETDATE()),

        CONSTRAINT FK_LeadSourceEvents_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId)

    );



    CREATE INDEX IX_LeadSourceEvents_UserId_CreatedAt ON dbo.LeadSourceEvents (UserId, CreatedAt DESC);

    CREATE INDEX IX_LeadSourceEvents_SourceType ON dbo.LeadSourceEvents (SourceType);

END

GO



-- Existing tables used at runtime (no changes required):

-- dbo.Users (MemberId, CreatedByUserId, CreatedAt)

-- dbo.PaymentOrders (UserId, PlanCode, Status, CreatedAt)

-- dbo.PaymentOrderReferrals (PaymentOrderId, ReferralCode)

-- dbo.UserPlans (active membership)

-- dbo.CustomerReports (generated report PDFs)

-- dbo.Plans (plan code/name lookup)

--

-- Report-only plan code excluded from membership sales:

--   scheme-report-onetime


