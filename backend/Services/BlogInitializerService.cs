using Microsoft.EntityFrameworkCore;
using RB_Website_API.Data;
using RB_Website_API.Models;

namespace RB_Website_API.Services;

/// <summary>
/// Creates the Blogs table on startup and keeps the three legacy articles
/// in sync with Data/BlogContent/*.html (full bodies from the original static site).
/// </summary>
public sealed class BlogInitializerService : IHostedService
{
    private readonly IServiceProvider _sp;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<BlogInitializerService> _log;

    public BlogInitializerService(
        IServiceProvider sp,
        IWebHostEnvironment env,
        ILogger<BlogInitializerService> log)
    {
        _sp = sp;
        _env = env;
        _log = log;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        await using var scope = _sp.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            await db.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Blogs'
                )
                BEGIN
                    CREATE TABLE [dbo].[Blogs] (
                        [BlogId]          INT IDENTITY(1,1) NOT NULL,
                        [Slug]            NVARCHAR(200)     NOT NULL,
                        [Title]           NVARCHAR(500)     NOT NULL,
                        [Crumb]           NVARCHAR(200)     NOT NULL CONSTRAINT [DF_Blogs_Crumb]          DEFAULT '',
                        [Meta]            NVARCHAR(500)     NOT NULL CONSTRAINT [DF_Blogs_Meta]           DEFAULT '',
                        [Content]         NVARCHAR(MAX)     NOT NULL CONSTRAINT [DF_Blogs_Content]        DEFAULT '',
                        [Category]        NVARCHAR(50)      NOT NULL CONSTRAINT [DF_Blogs_Category]       DEFAULT 'blog',
                        [BadgeSlug]       NVARCHAR(50)      NOT NULL CONSTRAINT [DF_Blogs_BadgeSlug]      DEFAULT 'msme-green',
                        [DateLabel]       NVARCHAR(100)     NOT NULL CONSTRAINT [DF_Blogs_DateLabel]      DEFAULT '',
                        [Summary]         NVARCHAR(2000)    NOT NULL CONSTRAINT [DF_Blogs_Summary]        DEFAULT '',
                        [BadgeText]       NVARCHAR(100)     NOT NULL CONSTRAINT [DF_Blogs_BadgeText]      DEFAULT '',
                        [BadgeClass]      NVARCHAR(100)     NOT NULL CONSTRAINT [DF_Blogs_BadgeClass]     DEFAULT '',
                        [CardIcon]        NVARCHAR(50)      NOT NULL CONSTRAINT [DF_Blogs_CardIcon]       DEFAULT '',
                        [CardClass]       NVARCHAR(100)     NOT NULL CONSTRAINT [DF_Blogs_CardClass]      DEFAULT '',
                        [ImageUrl]        NVARCHAR(500)     NULL,
                        [SeoTitle]        NVARCHAR(500)     NULL,
                        [MetaDescription] NVARCHAR(1000)    NULL,
                        [IsPublished]     BIT               NOT NULL CONSTRAINT [DF_Blogs_IsPublished]    DEFAULT 0,
                        [CreatedAt]       DATETIME          NOT NULL CONSTRAINT [DF_Blogs_CreatedAt]      DEFAULT GETDATE(),
                        [UpdatedAt]       DATETIME          NOT NULL CONSTRAINT [DF_Blogs_UpdatedAt]      DEFAULT GETDATE(),
                        [CreatedByUserId] UNIQUEIDENTIFIER  NULL,
                        CONSTRAINT [PK_Blogs]      PRIMARY KEY ([BlogId]),
                        CONSTRAINT [UQ_Blogs_Slug] UNIQUE      ([Slug])
                    )
                END", ct);

            _log.LogInformation("Blogs table verified.");

            await db.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Blogs' AND COLUMN_NAME = 'BadgeSlug'
                )
                BEGIN
                    ALTER TABLE [dbo].[Blogs]
                    ADD [BadgeSlug] NVARCHAR(50) NOT NULL CONSTRAINT [DF_Blogs_BadgeSlug_Alt] DEFAULT 'msme-green'
                END", ct);

            await db.Database.ExecuteSqlRawAsync(@"
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'BlogCategories'
                )
                AND NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'BlogCategories' AND COLUMN_NAME = 'BlogCategoryId'
                )
                BEGIN
                    DROP TABLE [dbo].[BlogCategories]
                END

                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'BlogCategories'
                )
                BEGIN
                    CREATE TABLE [dbo].[BlogCategories] (
                        [BlogCategoryId] INT IDENTITY(1,1) NOT NULL,
                        [Slug]           NVARCHAR(50)  NOT NULL,
                        [Label]          NVARCHAR(100) NOT NULL,
                        [SortOrder]      INT           NOT NULL CONSTRAINT [DF_BlogCategories_SortOrder] DEFAULT 0,
                        [IsActive]       BIT           NOT NULL CONSTRAINT [DF_BlogCategories_IsActive] DEFAULT 1,
                        [ShowInFilter]   BIT           NOT NULL CONSTRAINT [DF_BlogCategories_ShowInFilter] DEFAULT 1,
                        [CreatedAt]      DATETIME      NOT NULL CONSTRAINT [DF_BlogCategories_CreatedAt] DEFAULT GETDATE(),
                        [UpdatedAt]      DATETIME      NOT NULL CONSTRAINT [DF_BlogCategories_UpdatedAt] DEFAULT GETDATE(),
                        CONSTRAINT [PK_BlogCategories] PRIMARY KEY ([BlogCategoryId]),
                        CONSTRAINT [UQ_BlogCategories_Slug] UNIQUE ([Slug])
                    )
                END", ct);

            _log.LogInformation("BlogCategories table verified.");

            await db.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'BlogBadges'
                )
                BEGIN
                    CREATE TABLE [dbo].[BlogBadges] (
                        [BlogBadgeId] INT IDENTITY(1,1) NOT NULL,
                        [Slug]        NVARCHAR(50)  NOT NULL,
                        [Label]       NVARCHAR(100) NOT NULL,
                        [BadgeText]   NVARCHAR(100) NOT NULL,
                        [BadgeClass]  NVARCHAR(100) NOT NULL,
                        [CardIcon]    NVARCHAR(50)  NOT NULL,
                        [CardClass]   NVARCHAR(100) NOT NULL,
                        [SortOrder]   INT           NOT NULL CONSTRAINT [DF_BlogBadges_SortOrder] DEFAULT 0,
                        [IsActive]    BIT           NOT NULL CONSTRAINT [DF_BlogBadges_IsActive] DEFAULT 1,
                        [CreatedAt]   DATETIME      NOT NULL CONSTRAINT [DF_BlogBadges_CreatedAt] DEFAULT GETDATE(),
                        [UpdatedAt]   DATETIME      NOT NULL CONSTRAINT [DF_BlogBadges_UpdatedAt] DEFAULT GETDATE(),
                        CONSTRAINT [PK_BlogBadges] PRIMARY KEY ([BlogBadgeId]),
                        CONSTRAINT [UQ_BlogBadges_Slug] UNIQUE ([Slug])
                    )
                END", ct);

            _log.LogInformation("BlogBadges table verified.");

            var seededBadges = await SyncSeedBadges(db, ct);
            if (seededBadges > 0)
                _log.LogInformation("Seeded {Count} blog badge(s).", seededBadges);

            var seededCategories = await SyncSeedCategories(db, ct);
            if (seededCategories > 0)
                _log.LogInformation("Seeded {Count} blog categor(ies).", seededCategories);

            var synced = await SyncSeedArticles(db, ct);
            if (synced > 0)
                _log.LogInformation("Synced {Count} blog seed article(s).", synced);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to initialise Blogs table.");
        }
    }

    private async Task<int> SyncSeedArticles(AppDbContext db, CancellationToken ct)
    {
        var now = DateTime.Now;
        var synced = 0;

        foreach (var seed in BlogSeedCatalog.Articles)
        {
            var contentPath = Path.Combine(_env.ContentRootPath, "Data", "BlogContent", $"{seed.Slug}.html");
            if (!File.Exists(contentPath))
            {
                _log.LogWarning("Blog seed content file missing: {Path}", contentPath);
                continue;
            }

            var content = (await File.ReadAllTextAsync(contentPath, ct)).Trim();
            if (content.StartsWith('`'))
                content = content.TrimStart('`').TrimStart();
            if (string.IsNullOrWhiteSpace(content))
            {
                _log.LogWarning("Blog seed content empty for slug {Slug}.", seed.Slug);
                continue;
            }

            var existing = await db.Blogs.FirstOrDefaultAsync(b => b.Slug == seed.Slug, ct);
            if (existing is null)
            {
                db.Blogs.Add(new Blog
                {
                    Slug = seed.Slug,
                    Title = seed.Title,
                    Crumb = seed.Crumb,
                    Meta = seed.Meta,
                    Content = content,
                    Category = seed.Category,
                    BadgeSlug = "msme-green",
                    DateLabel = seed.DateLabel,
                    Summary = seed.Summary,
                    BadgeText = seed.BadgeText,
                    BadgeClass = seed.BadgeClass,
                    CardIcon = seed.CardIcon,
                    CardClass = seed.CardClass,
                    ImageUrl = seed.ImageUrl,
                    SeoTitle = seed.SeoTitle,
                    MetaDescription = seed.MetaDescription,
                    IsPublished = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
                synced++;
                continue;
            }

            if (!BlogSeedCatalog.IsPlaceholderContent(existing.Content))
                continue;

            existing.Title = seed.Title;
            existing.Crumb = seed.Crumb;
            existing.Meta = seed.Meta;
            existing.Content = content;
            existing.Category = seed.Category;
            existing.DateLabel = seed.DateLabel;
            existing.Summary = seed.Summary;
            existing.BadgeText = seed.BadgeText;
            existing.BadgeClass = seed.BadgeClass;
            existing.CardIcon = seed.CardIcon;
            existing.CardClass = seed.CardClass;
            existing.ImageUrl = seed.ImageUrl;
            existing.SeoTitle = seed.SeoTitle;
            existing.MetaDescription = seed.MetaDescription;
            existing.IsPublished = true;
            existing.UpdatedAt = now;
            synced++;
        }

        if (synced > 0)
            await db.SaveChangesAsync(ct);

        return synced;
    }

    private static async Task<int> SyncSeedCategories(AppDbContext db, CancellationToken ct)
    {
        var now = DateTime.Now;
        var seeded = 0;

        foreach (var seed in BlogCategorySeedCatalog.Categories)
        {
            var existing = await db.BlogCategories.FirstOrDefaultAsync(c => c.Slug == seed.Slug, ct);
            if (existing is not null) continue;

            db.BlogCategories.Add(new BlogCategory
            {
                Slug = seed.Slug,
                Label = seed.Label,
                SortOrder = seed.SortOrder,
                IsActive = true,
                ShowInFilter = seed.ShowInFilter,
                CreatedAt = now,
                UpdatedAt = now,
            });
            seeded++;
        }

        if (seeded > 0)
            await db.SaveChangesAsync(ct);

        return seeded;
    }

    private static async Task<int> SyncSeedBadges(AppDbContext db, CancellationToken ct)
    {
        var now = DateTime.Now;
        var seeded = 0;
        var updated = 0;

        foreach (var seed in BlogBadgeSeedCatalog.Badges)
        {
            var existing = await db.BlogBadges.FirstOrDefaultAsync(b => b.Slug == seed.Slug, ct);
            if (existing is null)
            {
                db.BlogBadges.Add(new BlogBadge
                {
                    Slug = seed.Slug,
                    Label = seed.Label,
                    BadgeText = seed.BadgeText,
                    BadgeClass = seed.BadgeClass,
                    CardIcon = seed.CardIcon,
                    CardClass = seed.CardClass,
                    SortOrder = seed.SortOrder,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
                seeded++;
                continue;
            }

            // Keep seed naming/appearance aligned without changing custom badges.
            if (!string.Equals(existing.Label, seed.Label, StringComparison.Ordinal)
                || !string.Equals(existing.BadgeText, seed.BadgeText, StringComparison.Ordinal)
                || !string.Equals(existing.BadgeClass, seed.BadgeClass, StringComparison.Ordinal)
                || !string.Equals(existing.CardIcon, seed.CardIcon, StringComparison.Ordinal)
                || !string.Equals(existing.CardClass, seed.CardClass, StringComparison.Ordinal)
                || existing.SortOrder != seed.SortOrder)
            {
                existing.Label = seed.Label;
                existing.BadgeText = seed.BadgeText;
                existing.BadgeClass = seed.BadgeClass;
                existing.CardIcon = seed.CardIcon;
                existing.CardClass = seed.CardClass;
                existing.SortOrder = seed.SortOrder;
                existing.UpdatedAt = now;
                updated++;

                var linkedBlogs = await db.Blogs.Where(b => b.BadgeSlug == seed.Slug).ToListAsync(ct);
                foreach (var blog in linkedBlogs)
                {
                    blog.BadgeText = seed.BadgeText;
                    blog.BadgeClass = seed.BadgeClass;
                    blog.CardIcon = seed.CardIcon;
                    blog.CardClass = seed.CardClass;
                    blog.UpdatedAt = now;
                }
            }
        }

        if (seeded > 0 || updated > 0)
            await db.SaveChangesAsync(ct);

        return seeded + updated;
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
