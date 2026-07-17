using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RB_Website_API.Data;
using RB_Website_API.Models;
using System.Text.RegularExpressions;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/blog-categories")]
public sealed class BlogCategoryController : ControllerBase
{
    private readonly AppDbContext _db;

    public BlogCategoryController(AppDbContext db) => _db = db;

    public sealed record BlogCategoryItem(
        int BlogCategoryId,
        string Slug,
        string Label,
        int SortOrder,
        bool IsActive,
        bool ShowInFilter
    );

    public sealed record BlogCategoryListResponse(
        bool Success,
        string? Message = null,
        List<BlogCategoryItem>? Categories = null
    );

    public sealed record BlogCategoryDetailResponse(
        bool Success,
        string? Message = null,
        BlogCategoryItem? Category = null
    );

    public sealed record UpsertBlogCategoryRequest(
        string Slug,
        string Label,
        int SortOrder,
        bool IsActive,
        bool ShowInFilter
    );

    public sealed record BlogCategoryMutationResponse(bool Success, string? Message = null, int? BlogCategoryId = null);

    [HttpGet]
    public async Task<ActionResult<BlogCategoryListResponse>> ListPublic(
        [FromQuery] bool filtersOnly = false,
        CancellationToken ct = default)
    {
        var q = _db.BlogCategories.AsNoTracking().Where(c => c.IsActive);
        if (filtersOnly) q = q.Where(c => c.ShowInFilter);

        var items = await q
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Label)
            .Select(c => ToItem(c))
            .ToListAsync(ct);

        return Ok(new BlogCategoryListResponse(true, "OK", items));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpGet("admin")]
    public async Task<ActionResult<BlogCategoryListResponse>> AdminList(CancellationToken ct)
    {
        var items = await _db.BlogCategories.AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Label)
            .Select(c => ToItem(c))
            .ToListAsync(ct);

        return Ok(new BlogCategoryListResponse(true, "OK", items));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpGet("admin/{blogCategoryId:int}")]
    public async Task<ActionResult<BlogCategoryDetailResponse>> AdminGet(int blogCategoryId, CancellationToken ct)
    {
        var cat = await _db.BlogCategories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.BlogCategoryId == blogCategoryId, ct);
        if (cat is null) return NotFound(new BlogCategoryDetailResponse(false, "Category not found."));
        return Ok(new BlogCategoryDetailResponse(true, "OK", ToItem(cat)));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpPost("admin")]
    public async Task<ActionResult<BlogCategoryMutationResponse>> Create(
        [FromBody] UpsertBlogCategoryRequest? req,
        CancellationToken ct)
    {
        if (req is null) return BadRequest(new BlogCategoryMutationResponse(false, "Request is required."));

        var (ok, err, slug) = ValidateSlug(req.Slug);
        if (!ok) return BadRequest(new BlogCategoryMutationResponse(false, err));
        if (string.IsNullOrWhiteSpace(req.Label))
            return BadRequest(new BlogCategoryMutationResponse(false, "Filter name is required."));

        if (await _db.BlogCategories.AnyAsync(c => c.Slug == slug, ct))
            return Conflict(new BlogCategoryMutationResponse(false, "A category with this slug already exists."));

        var now = DateTime.Now;
        var cat = new BlogCategory
        {
            Slug = slug!,
            Label = req.Label.Trim(),
            SortOrder = req.SortOrder,
            IsActive = req.IsActive,
            ShowInFilter = req.ShowInFilter,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.BlogCategories.Add(cat);
        await _db.SaveChangesAsync(ct);
        return Ok(new BlogCategoryMutationResponse(true, "Category created.", cat.BlogCategoryId));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpPut("admin/{blogCategoryId:int}")]
    public async Task<ActionResult<BlogCategoryMutationResponse>> Update(
        int blogCategoryId,
        [FromBody] UpsertBlogCategoryRequest? req,
        CancellationToken ct)
    {
        if (req is null) return BadRequest(new BlogCategoryMutationResponse(false, "Request is required."));

        var (ok, err, slug) = ValidateSlug(req.Slug);
        if (!ok) return BadRequest(new BlogCategoryMutationResponse(false, err));
        if (string.IsNullOrWhiteSpace(req.Label))
            return BadRequest(new BlogCategoryMutationResponse(false, "Filter name is required."));

        var cat = await _db.BlogCategories.FirstOrDefaultAsync(c => c.BlogCategoryId == blogCategoryId, ct);
        if (cat is null) return NotFound(new BlogCategoryMutationResponse(false, "Category not found."));

        if (await _db.BlogCategories.AnyAsync(c => c.Slug == slug && c.BlogCategoryId != blogCategoryId, ct))
            return Conflict(new BlogCategoryMutationResponse(false, "Another category already uses this slug."));

        var oldSlug = cat.Slug;
        cat.Slug = slug!;
        cat.Label = req.Label.Trim();
        cat.SortOrder = req.SortOrder;
        cat.IsActive = req.IsActive;
        cat.ShowInFilter = req.ShowInFilter;
        cat.UpdatedAt = DateTime.Now;

        if (!string.Equals(oldSlug, cat.Slug, StringComparison.OrdinalIgnoreCase))
        {
            var blogs = await _db.Blogs.Where(b => b.Category == oldSlug).ToListAsync(ct);
            foreach (var blog in blogs)
                blog.Category = cat.Slug;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new BlogCategoryMutationResponse(true, "Category updated.", blogCategoryId));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpDelete("admin/{blogCategoryId:int}")]
    public async Task<ActionResult<BlogCategoryMutationResponse>> Delete(int blogCategoryId, CancellationToken ct)
    {
        var cat = await _db.BlogCategories.FirstOrDefaultAsync(c => c.BlogCategoryId == blogCategoryId, ct);
        if (cat is null) return NotFound(new BlogCategoryMutationResponse(false, "Category not found."));

        var inUse = await _db.Blogs.CountAsync(b => b.Category == cat.Slug, ct);
        if (inUse > 0)
            return Conflict(new BlogCategoryMutationResponse(false, $"Cannot delete: {inUse} article(s) use this category."));

        _db.BlogCategories.Remove(cat);
        await _db.SaveChangesAsync(ct);
        return Ok(new BlogCategoryMutationResponse(true, "Category deleted.", blogCategoryId));
    }

    private static BlogCategoryItem ToItem(BlogCategory c) => new(
        c.BlogCategoryId,
        c.Slug,
        c.Label,
        c.SortOrder,
        c.IsActive,
        c.ShowInFilter);

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
