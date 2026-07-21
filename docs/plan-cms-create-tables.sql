-- Plan CMS (run manually). See docs/plan-cms-schema.md

IF COL_LENGTH(N'dbo.Plans', N'IconEmoji') IS NULL
  ALTER TABLE dbo.Plans ADD IconEmoji NVARCHAR(20) NOT NULL CONSTRAINT DF_Plans_IconEmoji DEFAULT (CONVERT(NVARCHAR(20), 0x3CD831DF)); -- seedling
IF COL_LENGTH(N'dbo.Plans', N'BadgeClass') IS NULL
  ALTER TABLE dbo.Plans ADD BadgeClass NVARCHAR(50) NOT NULL CONSTRAINT DF_Plans_BadgeClass DEFAULT (N'free');
IF COL_LENGTH(N'dbo.Plans', N'Tagline') IS NULL
  ALTER TABLE dbo.Plans ADD Tagline NVARCHAR(500) NULL;
IF COL_LENGTH(N'dbo.Plans', N'IsFeatured') IS NULL
  ALTER TABLE dbo.Plans ADD IsFeatured BIT NOT NULL CONSTRAINT DF_Plans_IsFeatured DEFAULT (0);
IF COL_LENGTH(N'dbo.Plans', N'SortOrder') IS NULL
  ALTER TABLE dbo.Plans ADD SortOrder INT NOT NULL CONSTRAINT DF_Plans_SortOrder DEFAULT (0);
IF COL_LENGTH(N'dbo.Plans', N'IncludesPlanCode') IS NULL
  ALTER TABLE dbo.Plans ADD IncludesPlanCode NVARCHAR(40) NULL;
IF COL_LENGTH(N'dbo.Plans', N'DisplayPriceSuffix') IS NULL
  ALTER TABLE dbo.Plans ADD DisplayPriceSuffix NVARCHAR(120) NOT NULL CONSTRAINT DF_Plans_DisplayPriceSuffix DEFAULT (N'/ year + 18% GST');
IF COL_LENGTH(N'dbo.Plans', N'CtaLabel') IS NULL
  ALTER TABLE dbo.Plans ADD CtaLabel NVARCHAR(120) NULL;
IF COL_LENGTH(N'dbo.Plans', N'PopularLabel') IS NULL
  ALTER TABLE dbo.Plans ADD PopularLabel NVARCHAR(80) NULL;
IF COL_LENGTH(N'dbo.Plans', N'CreatedByUserId') IS NULL
  ALTER TABLE dbo.Plans ADD CreatedByUserId UNIQUEIDENTIFIER NULL;
GO

IF OBJECT_ID(N'dbo.PlanFeatures', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.PlanFeatures (
    PlanFeatureId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlanFeatures PRIMARY KEY,
    PlanId INT NOT NULL,
    Text NVARCHAR(1000) NOT NULL,
    Description NVARCHAR(2000) NULL,
    OfferingSlug NVARCHAR(100) NULL,
    IsIncludesLine BIT NOT NULL CONSTRAINT DF_PlanFeatures_IsIncludesLine DEFAULT (0),
    SortOrder INT NOT NULL CONSTRAINT DF_PlanFeatures_SortOrder DEFAULT (0),
    CONSTRAINT FK_PlanFeatures_Plans FOREIGN KEY (PlanId) REFERENCES dbo.Plans(PlanId) ON DELETE CASCADE
  );
  CREATE INDEX IX_PlanFeatures_Plan_Sort ON dbo.PlanFeatures (PlanId, SortOrder);
END
GO
