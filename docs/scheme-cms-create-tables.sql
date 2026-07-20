-- Scheme CMS tables (run manually). Do not use EF migrations for this module.
-- See docs/scheme-cms-schema.md

IF OBJECT_ID(N'dbo.SchemeCategories', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.SchemeCategories (
    SchemeCategoryId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SchemeCategories PRIMARY KEY,
    Slug NVARCHAR(50) NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    ShortDescription NVARCHAR(500) NULL,
    IconUrl NVARCHAR(500) NULL,
    SortOrder INT NOT NULL CONSTRAINT DF_SchemeCategories_SortOrder DEFAULT (0),
    IsActive BIT NOT NULL CONSTRAINT DF_SchemeCategories_IsActive DEFAULT (1),
    ShowInFilter BIT NOT NULL CONSTRAINT DF_SchemeCategories_ShowInFilter DEFAULT (1),
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    CONSTRAINT UQ_SchemeCategories_Slug UNIQUE (Slug)
  );
  CREATE INDEX IX_SchemeCategories_Active_Sort ON dbo.SchemeCategories (IsActive, SortOrder);
END
GO

IF OBJECT_ID(N'dbo.Schemes', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.Schemes (
    SchemeId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Schemes PRIMARY KEY,
    Slug NVARCHAR(200) NOT NULL,
    Name NVARCHAR(300) NOT NULL,
    Crumb NVARCHAR(200) NOT NULL CONSTRAINT DF_Schemes_Crumb DEFAULT (N''),
    Tagline NVARCHAR(500) NOT NULL CONSTRAINT DF_Schemes_Tagline DEFAULT (N''),
    ShortDescription NVARCHAR(2000) NOT NULL CONSTRAINT DF_Schemes_ShortDescription DEFAULT (N''),
    ContentHtml NVARCHAR(MAX) NOT NULL CONSTRAINT DF_Schemes_ContentHtml DEFAULT (N''),
    CategorySlug NVARCHAR(50) NOT NULL,
    PrimaryBadgeText NVARCHAR(100) NOT NULL CONSTRAINT DF_Schemes_PrimaryBadgeText DEFAULT (N''),
    PrimaryBadgeClass NVARCHAR(50) NOT NULL CONSTRAINT DF_Schemes_PrimaryBadgeClass DEFAULT (N'badge-green'),
    SecondaryBadgeText NVARCHAR(100) NOT NULL CONSTRAINT DF_Schemes_SecondaryBadgeText DEFAULT (N''),
    SecondaryBadgeClass NVARCHAR(50) NOT NULL CONSTRAINT DF_Schemes_SecondaryBadgeClass DEFAULT (N'badge-orange'),
    HomeTitle NVARCHAR(200) NOT NULL CONSTRAINT DF_Schemes_HomeTitle DEFAULT (N''),
    HomeBadgeText NVARCHAR(100) NOT NULL CONSTRAINT DF_Schemes_HomeBadgeText DEFAULT (N''),
    HomeBadgeClass NVARCHAR(50) NOT NULL CONSTRAINT DF_Schemes_HomeBadgeClass DEFAULT (N''),
    HomeDescription NVARCHAR(2000) NOT NULL CONSTRAINT DF_Schemes_HomeDescription DEFAULT (N''),
    SeoTitle NVARCHAR(500) NULL,
    MetaDescription NVARCHAR(1000) NULL,
    IsPublished BIT NOT NULL CONSTRAINT DF_Schemes_IsPublished DEFAULT (0),
    IsFeatured BIT NOT NULL CONSTRAINT DF_Schemes_IsFeatured DEFAULT (0),
    SortOrder INT NOT NULL CONSTRAINT DF_Schemes_SortOrder DEFAULT (0),
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    CreatedByUserId UNIQUEIDENTIFIER NULL,
    CONSTRAINT UQ_Schemes_Slug UNIQUE (Slug)
  );
  CREATE INDEX IX_Schemes_Published_Sort ON dbo.Schemes (IsPublished, SortOrder, CreatedAt);
  CREATE INDEX IX_Schemes_CategorySlug ON dbo.Schemes (CategorySlug);
  CREATE INDEX IX_Schemes_IsFeatured ON dbo.Schemes (IsFeatured);
END
GO

IF OBJECT_ID(N'dbo.SchemeBenefits', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.SchemeBenefits (
    SchemeBenefitId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SchemeBenefits PRIMARY KEY,
    SchemeId INT NOT NULL,
    Text NVARCHAR(1000) NOT NULL,
    SortOrder INT NOT NULL CONSTRAINT DF_SchemeBenefits_SortOrder DEFAULT (0),
    CONSTRAINT FK_SchemeBenefits_Schemes FOREIGN KEY (SchemeId) REFERENCES dbo.Schemes(SchemeId) ON DELETE CASCADE
  );
  CREATE INDEX IX_SchemeBenefits_Scheme_Sort ON dbo.SchemeBenefits (SchemeId, SortOrder);
END
GO

IF OBJECT_ID(N'dbo.SchemeCardHighlights', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.SchemeCardHighlights (
    SchemeCardHighlightId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SchemeCardHighlights PRIMARY KEY,
    SchemeId INT NOT NULL,
    Text NVARCHAR(1000) NOT NULL,
    SortOrder INT NOT NULL CONSTRAINT DF_SchemeCardHighlights_SortOrder DEFAULT (0),
    CONSTRAINT FK_SchemeCardHighlights_Schemes FOREIGN KEY (SchemeId) REFERENCES dbo.Schemes(SchemeId) ON DELETE CASCADE
  );
  CREATE INDEX IX_SchemeCardHighlights_Scheme_Sort ON dbo.SchemeCardHighlights (SchemeId, SortOrder);
END
GO
