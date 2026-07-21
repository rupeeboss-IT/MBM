-- Team CMS tables (run manually). Do not use EF migrations for this module.
-- See docs/team-cms-schema.md

IF OBJECT_ID(N'dbo.TeamMembers', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.TeamMembers (
    TeamMemberId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TeamMembers PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    DesignationHtml NVARCHAR(2000) NOT NULL CONSTRAINT DF_TeamMembers_DesignationHtml DEFAULT (N''),
    PhotoUrl NVARCHAR(500) NOT NULL,
    SortOrder INT NOT NULL CONSTRAINT DF_TeamMembers_SortOrder DEFAULT (0),
    IsActive BIT NOT NULL CONSTRAINT DF_TeamMembers_IsActive DEFAULT (1),
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    CreatedByUserId UNIQUEIDENTIFIER NULL
  );
  CREATE INDEX IX_TeamMembers_Active_Sort ON dbo.TeamMembers (IsActive, SortOrder);
END
GO
