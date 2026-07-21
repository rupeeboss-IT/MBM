-- Team CMS seed — exact copy of former static about.html leadership team.
-- Run AFTER docs/team-cms-create-tables.sql. Safe to re-run (skips existing names).

SET NOCOUNT ON;
DECLARE @Now DATETIME2 = GETDATE();

IF NOT EXISTS (SELECT 1 FROM dbo.TeamMembers WHERE Name = N'Shri. CA. N B Shetty')
  INSERT INTO dbo.TeamMembers (Name, DesignationHtml, PhotoUrl, SortOrder, IsActive, CreatedAt, UpdatedAt)
  VALUES (N'Shri. CA. N B Shetty', N'Chairman, MSME Bharat Manch, RupeeBoss Financial Services Pvt Ltd & Triveni Group', N'/team/NBShetty.jpg', 1, 1, @Now, @Now);

IF NOT EXISTS (SELECT 1 FROM dbo.TeamMembers WHERE Name = N'Mr. P N Shetty')
  INSERT INTO dbo.TeamMembers (Name, DesignationHtml, PhotoUrl, SortOrder, IsActive, CreatedAt, UpdatedAt)
  VALUES (N'Mr. P N Shetty', N'President MSME Bharat Manch<br />Founder & CEO - RupeeBoss Financial Services Pvt Ltd', N'/team/PNShetty.jpg', 2, 1, @Now, @Now);

IF NOT EXISTS (SELECT 1 FROM dbo.TeamMembers WHERE Name = N'Dr. Narendra Mairpady')
  INSERT INTO dbo.TeamMembers (Name, DesignationHtml, PhotoUrl, SortOrder, IsActive, CreatedAt, UpdatedAt)
  VALUES (N'Dr. Narendra Mairpady', N'Advisor - Enqube Collaborations', N'/team/dr.Narendra.jpeg', 3, 1, @Now, @Now);

IF NOT EXISTS (SELECT 1 FROM dbo.TeamMembers WHERE Name = N'Mr. Sathish Shetty')
  INSERT INTO dbo.TeamMembers (Name, DesignationHtml, PhotoUrl, SortOrder, IsActive, CreatedAt, UpdatedAt)
  VALUES (N'Mr. Sathish Shetty', N'Senior Director - (South) MSME Bharat Manch', N'/team/satish.jpg', 4, 1, @Now, @Now);

IF NOT EXISTS (SELECT 1 FROM dbo.TeamMembers WHERE Name = N'Mr. Praveen Chandra')
  INSERT INTO dbo.TeamMembers (Name, DesignationHtml, PhotoUrl, SortOrder, IsActive, CreatedAt, UpdatedAt)
  VALUES (N'Mr. Praveen Chandra', N'Country Director - MSME Bharat Manch', N'/team/praveen.png', 5, 1, @Now, @Now);

IF NOT EXISTS (SELECT 1 FROM dbo.TeamMembers WHERE Name = N'Mr. Rawlo Tuna')
  INSERT INTO dbo.TeamMembers (Name, DesignationHtml, PhotoUrl, SortOrder, IsActive, CreatedAt, UpdatedAt)
  VALUES (N'Mr. Rawlo Tuna', N'Director - (East) MSME Bharat Manch', N'/team/rawlo.jpg', 6, 1, @Now, @Now);

IF NOT EXISTS (SELECT 1 FROM dbo.TeamMembers WHERE Name = N'Mr. Adesh Otari')
  INSERT INTO dbo.TeamMembers (Name, DesignationHtml, PhotoUrl, SortOrder, IsActive, CreatedAt, UpdatedAt)
  VALUES (N'Mr. Adesh Otari', N'Director - (west) MSME Bharat Manch', N'/team/adesh.png', 7, 1, @Now, @Now);

IF NOT EXISTS (SELECT 1 FROM dbo.TeamMembers WHERE Name = N'Mr. Keerthiraja Shetty')
  INSERT INTO dbo.TeamMembers (Name, DesignationHtml, PhotoUrl, SortOrder, IsActive, CreatedAt, UpdatedAt)
  VALUES (N'Mr. Keerthiraja Shetty', N'Deputy Director - MSME Bharat Manch', N'/team/keerthi.jpg', 8, 1, @Now, @Now);

IF NOT EXISTS (SELECT 1 FROM dbo.TeamMembers WHERE Name = N'Mr. Sushanth Shetty')
  INSERT INTO dbo.TeamMembers (Name, DesignationHtml, PhotoUrl, SortOrder, IsActive, CreatedAt, UpdatedAt)
  VALUES (N'Mr. Sushanth Shetty', N'Deputy Director - MSME Bharat Manch', N'/team/sushanth.png', 9, 1, @Now, @Now);

IF NOT EXISTS (SELECT 1 FROM dbo.TeamMembers WHERE Name = N'Mr. Shrinivasa Shetty')
  INSERT INTO dbo.TeamMembers (Name, DesignationHtml, PhotoUrl, SortOrder, IsActive, CreatedAt, UpdatedAt)
  VALUES (N'Mr. Shrinivasa Shetty', N'Deputy Director - MSME Bharat Manch', N'/team/shriniwasa.png', 10, 1, @Now, @Now);

IF NOT EXISTS (SELECT 1 FROM dbo.TeamMembers WHERE Name = N'Mr. Pravin Kamble')
  INSERT INTO dbo.TeamMembers (Name, DesignationHtml, PhotoUrl, SortOrder, IsActive, CreatedAt, UpdatedAt)
  VALUES (N'Mr. Pravin Kamble', N'Associate Director - (Product & Digital) MSME Bharat Manch', N'/team/pravinkamble.png', 11, 1, @Now, @Now);

GO

PRINT N'Team CMS seed complete.';
