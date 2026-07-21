-- Fix plan icon emojis corrupted by non-UTF-8 sqlcmd runs (shows as ðŸŒ± instead of 🌱).
-- Safe to re-run. UTF-16 LE byte order for CONVERT(NVARCHAR, …).

UPDATE dbo.Plans SET IconEmoji = CONVERT(NVARCHAR(20), 0x3CD831DF) WHERE Code = N'basic';     -- 🌱
UPDATE dbo.Plans SET IconEmoji = CONVERT(NVARCHAR(20), 0x3CD8C6DF) WHERE Code = N'premium';   -- 🏆
UPDATE dbo.Plans SET IconEmoji = CONVERT(NVARCHAR(20), 0x3DD88EDC) WHERE Code = N'pro';       -- 💎
UPDATE dbo.Plans SET IconEmoji = CONVERT(NVARCHAR(20), 0x3CD810DF) WHERE Code = N'standard'; -- 🌐
GO
