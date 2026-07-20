using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RB_Website_API.Data;
using RB_Website_API.Models;
using System.Text.RegularExpressions;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/scheme-categories")]
public sealed class SchemeCategoryController : ControllerBase
{
    private readonly AppDbContext _db;

    public SchemeCategoryController(AppDbContext db) => _db = db;

    public sealed record SchemeCategoryItem(
        int SchemeCategoryId,
        string Slug,
        string Name,
        string? ShortDescription,
        string? IconUrl,
        int SortOrder,
        bool IsActive,
        bool ShowInFilter,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );

    public sealed record SchemeCategoryListResponse(
        bool Success,
        string? Message = null,
        List<SchemeCategoryItem>? Categories = null
    );

    public sealed record SchemeCategoryDetailResponse(
        bool Success,
        string? Message = null,
        SchemeCategoryItem? Category = null
    );

    public sealed record UpsertSchemeCategoryRequest(
        string Slug,
        string Name,
        string? ShortDescription,
        string? IconUrl,
        int SortOrder,
        bool IsActive,
        bool ShowInFilter
    );

    public sealed record SchemeCategoryMutationResponse(bool Success, string? Message = null, int? SchemeCategoryId = null);

    [HttpGet]
    public async Task<ActionResult<SchemeCategoryListResponse>> ListPublic(
        [FromQuery] bool filtersOnly = false,
        CancellationToken ct = default)
    {
        var q = _db.SchemeCategories.AsNoTracking().Where(c => c.IsActive);
        if (filtersOnly) q = q.Where(c => c.ShowInFilter);

        var items = await q
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => ToItem(c))
            .ToListAsync(ct);

        return Ok(new SchemeCategoryListResponse(true, "OK", items));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpGet("admin")]
    public async Task<ActionResult<SchemeCategoryListResponse>> AdminList(CancellationToken ct)
    {
        var items = await _db.SchemeCategories.AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => ToItem(c))
            .ToListAsync(ct);

        return Ok(new SchemeCategoryListResponse(true, "OK", items));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpGet("admin/{schemeCategoryId:int}")]
    public async Task<ActionResult<SchemeCategoryDetailResponse>> AdminGet(int schemeCategoryId, CancellationToken ct)
    {
        var cat = await _db.SchemeCategories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.SchemeCategoryId == schemeCategoryId, ct);
        if (cat is null) return NotFound(new SchemeCategoryDetailResponse(false, "Category not found."));
        return Ok(new SchemeCategoryDetailResponse(true, "OK", ToItem(cat)));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpPost("admin")]
    public async Task<ActionResult<SchemeCategoryMutationResponse>> Create(
        [FromBody] UpsertSchemeCategoryRequest? req,
        CancellationToken ct)
    {
        if (req is null) return BadRequest(new SchemeCategoryMutationResponse(false, "Request is required."));

        var (ok, err, slug) = ValidateSlug(req.Slug);
        if (!ok) return BadRequest(new SchemeCategoryMutationResponse(false, err));
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new SchemeCategoryMutationResponse(false, "Category name is required."));

        if (await _db.SchemeCategories.AnyAsync(c => c.Slug == slug, ct))
            return Conflict(new SchemeCategoryMutationResponse(false, "A category with this slug already exists."));

        var now = DateTime.Now;
        var cat = new SchemeCategory
        {
            Slug = slug!,
            Name = req.Name.Trim(),
            ShortDescription = NullIfWhiteSpace(req.ShortDescription),
            IconUrl = NullIfWhiteSpace(req.IconUrl),
            SortOrder = req.SortOrder,
            IsActive = req.IsActive,
            ShowInFilter = req.ShowInFilter,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.SchemeCategories.Add(cat);
        await _db.SaveChangesAsync(ct);
        return Ok(new SchemeCategoryMutationResponse(true, "Category created.", cat.SchemeCategoryId));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpPut("admin/{schemeCategoryId:int}")]
    public async Task<ActionResult<SchemeCategoryMutationResponse>> Update(
        int schemeCategoryId,
        [FromBody] UpsertSchemeCategoryRequest? req,
        CancellationToken ct)
    {
        if (req is null) return BadRequest(new SchemeCategoryMutationResponse(false, "Request is required."));

        var (ok, err, slug) = ValidateSlug(req.Slug);
        if (!ok) return BadRequest(new SchemeCategoryMutationResponse(false, err));
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new SchemeCategoryMutationResponse(false, "Category name is required."));

        var cat = await _db.SchemeCategories.FirstOrDefaultAsync(c => c.SchemeCategoryId == schemeCategoryId, ct);
        if (cat is null) return NotFound(new SchemeCategoryMutationResponse(false, "Category not found."));

        if (await _db.SchemeCategories.AnyAsync(c => c.Slug == slug && c.SchemeCategoryId != schemeCategoryId, ct))
            return Conflict(new SchemeCategoryMutationResponse(false, "Another category already uses this slug."));

        var oldSlug = cat.Slug;
        cat.Slug = slug!;
        cat.Name = req.Name.Trim();
        cat.ShortDescription = NullIfWhiteSpace(req.ShortDescription);
        cat.IconUrl = NullIfWhiteSpace(req.IconUrl);
        cat.SortOrder = req.SortOrder;
        cat.IsActive = req.IsActive;
        cat.ShowInFilter = req.ShowInFilter;
        cat.UpdatedAt = DateTime.Now;

        if (!string.Equals(oldSlug, cat.Slug, StringComparison.OrdinalIgnoreCase))
        {
            var schemes = await _db.Schemes.Where(s => s.CategorySlug == oldSlug).ToListAsync(ct);
            foreach (var scheme in schemes)
                scheme.CategorySlug = cat.Slug;
        }

        var linkedSchemes = await _db.Schemes.Where(s => s.CategorySlug == cat.Slug).ToListAsync(ct);
        foreach (var scheme in linkedSchemes)
            scheme.PrimaryBadgeText = cat.Name;

        await _db.SaveChangesAsync(ct);
        return Ok(new SchemeCategoryMutationResponse(true, "Category updated.", schemeCategoryId));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpDelete("admin/{schemeCategoryId:int}")]
    public async Task<ActionResult<SchemeCategoryMutationResponse>> Delete(int schemeCategoryId, CancellationToken ct)
    {
        var cat = await _db.SchemeCategories.FirstOrDefaultAsync(c => c.SchemeCategoryId == schemeCategoryId, ct);
        if (cat is null) return NotFound(new SchemeCategoryMutationResponse(false, "Category not found."));

        var inUse = await _db.Schemes.CountAsync(s => s.CategorySlug == cat.Slug, ct);
        if (inUse > 0)
            return Conflict(new SchemeCategoryMutationResponse(false, $"Cannot delete: {inUse} scheme(s) use this category. Deactivate it instead."));

        _db.SchemeCategories.Remove(cat);
        await _db.SaveChangesAsync(ct);
        return Ok(new SchemeCategoryMutationResponse(true, "Category deleted.", schemeCategoryId));
    }

    private static SchemeCategoryItem ToItem(SchemeCategory c) => new(
        c.SchemeCategoryId,
        c.Slug,
        c.Name,
        c.ShortDescription,
        c.IconUrl,
        c.SortOrder,
        c.IsActive,
        c.ShowInFilter,
        c.CreatedAt,
        c.UpdatedAt);

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
