-- Fix ₹ (rupee) corrupted by non-UTF-8 inserts/edits.
-- Shows as â‚¹ instead of ₹ on plan cards ("Choose Your Plan & Start Growing").
-- Safe to re-run. Uses NCHAR so the script itself is ASCII-safe.

DECLARE @mojibake NVARCHAR(10) = NCHAR(0x00E2) + NCHAR(0x201A) + NCHAR(0x00B9); -- â‚¹
DECLARE @rupee    NVARCHAR(1)  = NCHAR(0x20B9); -- ₹

UPDATE dbo.PlanFeatures
SET Text = REPLACE(Text, @mojibake, @rupee)
WHERE Text LIKE N'%' + @mojibake + N'%';

UPDATE dbo.PlanFeatures
SET Description = REPLACE(Description, @mojibake, @rupee)
WHERE Description LIKE N'%' + @mojibake + N'%';

UPDATE dbo.Plans
SET Name = REPLACE(Name, @mojibake, @rupee)
WHERE Name LIKE N'%' + @mojibake + N'%';

UPDATE dbo.Plans
SET Tagline = REPLACE(Tagline, @mojibake, @rupee)
WHERE Tagline LIKE N'%' + @mojibake + N'%';

GO
