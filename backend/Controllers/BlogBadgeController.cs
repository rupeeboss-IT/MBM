using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RB_Website_API.Data;
using RB_Website_API.Models;
using System.Text.RegularExpressions;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/blog-badges")]
public sealed class BlogBadgeController : ControllerBase
{
    private readonly AppDbContext _db;

    public BlogBadgeController(AppDbContext db) => _db = db;

    public sealed record BlogBadgeItem(
        int BlogBadgeId,
        string Slug,
        string Label,
        string BadgeText,
        string BadgeClass,
        string CardIcon,
        string CardClass,
        int SortOrder,
        bool IsActive
    );

    public sealed record BlogBadgeListResponse(
        bool Success,
        string? Message = null,
        List<BlogBadgeItem>? Badges = null
    );

    public sealed record BlogBadgeDetailResponse(
        bool Success,
        string? Message = null,
        BlogBadgeItem? Badge = null
    );

    public sealed record UpsertBlogBadgeRequest(
        string Slug,
        string Label,
        string BadgeText,
        string BadgeClass,
        string CardIcon,
        string CardClass,
        int SortOrder,
        bool IsActive
    );

    public sealed record BlogBadgeMutationResponse(bool Success, string? Message = null, int? BlogBadgeId = null);

    [HttpGet]
    public async Task<ActionResult<BlogBadgeListResponse>> ListPublic(CancellationToken ct)
    {
        var items = await _db.BlogBadges.AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.SortOrder)
            .ThenBy(b => b.Label)
            .Select(b => ToItem(b))
            .ToListAsync(ct);

        return Ok(new BlogBadgeListResponse(true, "OK", items));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpGet("admin")]
    public async Task<ActionResult<BlogBadgeListResponse>> AdminList(CancellationToken ct)
    {
        var items = await _db.BlogBadges.AsNoTracking()
            .OrderBy(b => b.SortOrder)
            .ThenBy(b => b.Label)
            .Select(b => ToItem(b))
            .ToListAsync(ct);

        return Ok(new BlogBadgeListResponse(true, "OK", items));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpGet("admin/{blogBadgeId:int}")]
    public async Task<ActionResult<BlogBadgeDetailResponse>> AdminGet(int blogBadgeId, CancellationToken ct)
    {
        var badge = await _db.BlogBadges.AsNoTracking()
            .FirstOrDefaultAsync(b => b.BlogBadgeId == blogBadgeId, ct);
        if (badge is null) return NotFound(new BlogBadgeDetailResponse(false, "Card label not found."));
        return Ok(new BlogBadgeDetailResponse(true, "OK", ToItem(badge)));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpPost("admin")]
    public async Task<ActionResult<BlogBadgeMutationResponse>> Create(
        [FromBody] UpsertBlogBadgeRequest? req,
        CancellationToken ct)
    {
        if (req is null) return BadRequest(new BlogBadgeMutationResponse(false, "Request is required."));

        var (ok, err, slug) = ValidateSlug(req.Slug);
        if (!ok) return BadRequest(new BlogBadgeMutationResponse(false, err));
        if (string.IsNullOrWhiteSpace(req.Label))
            return BadRequest(new BlogBadgeMutationResponse(false, "Dropdown name is required."));
        if (string.IsNullOrWhiteSpace(req.BadgeText))
            return BadRequest(new BlogBadgeMutationResponse(false, "Pill text is required."));

        if (await _db.BlogBadges.AnyAsync(b => b.Slug == slug, ct))
            return Conflict(new BlogBadgeMutationResponse(false, "A card label with this slug already exists."));

        var now = DateTime.Now;
        var badge = new BlogBadge
        {
            Slug = slug!,
            Label = req.Label.Trim(),
            BadgeText = req.BadgeText.Trim(),
            BadgeClass = (req.BadgeClass ?? "badge badge-green").Trim(),
            CardIcon = (req.CardIcon ?? "📰").Trim(),
            CardClass = (req.CardClass ?? "news-img cat-blog").Trim(),
            SortOrder = req.SortOrder,
            IsActive = req.IsActive,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.BlogBadges.Add(badge);
        await _db.SaveChangesAsync(ct);
        return Ok(new BlogBadgeMutationResponse(true, "Card label created.", badge.BlogBadgeId));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpPut("admin/{blogBadgeId:int}")]
    public async Task<ActionResult<BlogBadgeMutationResponse>> Update(
        int blogBadgeId,
        [FromBody] UpsertBlogBadgeRequest? req,
        CancellationToken ct)
    {
        if (req is null) return BadRequest(new BlogBadgeMutationResponse(false, "Request is required."));

        var (ok, err, slug) = ValidateSlug(req.Slug);
        if (!ok) return BadRequest(new BlogBadgeMutationResponse(false, err));
        if (string.IsNullOrWhiteSpace(req.Label))
            return BadRequest(new BlogBadgeMutationResponse(false, "Dropdown name is required."));
        if (string.IsNullOrWhiteSpace(req.BadgeText))
            return BadRequest(new BlogBadgeMutationResponse(false, "Pill text is required."));

        var badge = await _db.BlogBadges.FirstOrDefaultAsync(b => b.BlogBadgeId == blogBadgeId, ct);
        if (badge is null) return NotFound(new BlogBadgeMutationResponse(false, "Card label not found."));

        if (await _db.BlogBadges.AnyAsync(b => b.Slug == slug && b.BlogBadgeId != blogBadgeId, ct))
            return Conflict(new BlogBadgeMutationResponse(false, "Another card label already uses this slug."));

        var oldSlug = badge.Slug;
        badge.Slug = slug!;
        badge.Label = req.Label.Trim();
        badge.BadgeText = req.BadgeText.Trim();
        badge.BadgeClass = (req.BadgeClass ?? "badge badge-green").Trim();
        badge.CardIcon = (req.CardIcon ?? "📰").Trim();
        badge.CardClass = (req.CardClass ?? "news-img cat-blog").Trim();
        badge.SortOrder = req.SortOrder;
        badge.IsActive = req.IsActive;
        badge.UpdatedAt = DateTime.Now;

        if (!string.Equals(oldSlug, badge.Slug, StringComparison.OrdinalIgnoreCase))
        {
            var blogsWithOldSlug = await _db.Blogs.Where(b => b.BadgeSlug == oldSlug).ToListAsync(ct);
            foreach (var blog in blogsWithOldSlug)
                blog.BadgeSlug = badge.Slug;
        }

        var linkedBlogs = await _db.Blogs.Where(b => b.BadgeSlug == badge.Slug).ToListAsync(ct);
        foreach (var blog in linkedBlogs)
        {
            blog.BadgeText = badge.BadgeText;
            blog.BadgeClass = badge.BadgeClass;
            blog.CardIcon = badge.CardIcon;
            blog.CardClass = badge.CardClass;
            blog.UpdatedAt = badge.UpdatedAt;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new BlogBadgeMutationResponse(true, "Card label updated.", blogBadgeId));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpDelete("admin/{blogBadgeId:int}")]
    public async Task<ActionResult<BlogBadgeMutationResponse>> Delete(int blogBadgeId, CancellationToken ct)
    {
        var badge = await _db.BlogBadges.FirstOrDefaultAsync(b => b.BlogBadgeId == blogBadgeId, ct);
        if (badge is null) return NotFound(new BlogBadgeMutationResponse(false, "Card label not found."));

        var inUse = await _db.Blogs.CountAsync(b => b.BadgeSlug == badge.Slug, ct);
        if (inUse > 0)
            return Conflict(new BlogBadgeMutationResponse(false, $"Cannot delete: {inUse} article(s) use this card label."));

        _db.BlogBadges.Remove(badge);
        await _db.SaveChangesAsync(ct);
        return Ok(new BlogBadgeMutationResponse(true, "Card label deleted.", blogBadgeId));
    }

    private static BlogBadgeItem ToItem(BlogBadge b) => new(
        b.BlogBadgeId,
        b.Slug,
        b.Label,
        b.BadgeText,
        b.BadgeClass,
        b.CardIcon,
        b.CardClass,
        b.SortOrder,
        b.IsActive);

    private static (bool Ok, string? Error, string? Slug) ValidateSlug(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return (false, "Slug is required.", null);

        var normalized = slug.Trim().ToLowerInvariant();
        if (normalized.Length > 50)
            return (false, "Slug must be 50 characters or fewer.", null);
        if (!Regex.IsMatch(normalized, @"^[a-z0-9]+(?:-[a-z0-9]+)*$"))
            return (false, "Slug may only contain lowercase letters, numbers, and hyphens.", null);

        return (true, null, normalized);
    }
}
