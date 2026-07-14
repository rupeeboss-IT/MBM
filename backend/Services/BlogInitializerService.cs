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

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
