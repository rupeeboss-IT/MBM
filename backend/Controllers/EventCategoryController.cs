using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RB_Website_API.Data;
using RB_Website_API.Models;
using System.Text.RegularExpressions;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/event-categories")]
public sealed class EventCategoryController : ControllerBase
{
    private readonly AppDbContext _db;

    public EventCategoryController(AppDbContext db) => _db = db;

    public sealed record EventCategoryItem(
        int EventCategoryId,
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

    public sealed record EventCategoryListResponse(
        bool Success,
        string? Message = null,
        List<EventCategoryItem>? Categories = null
    );

    public sealed record EventCategoryDetailResponse(
        bool Success,
        string? Message = null,
        EventCategoryItem? Category = null
    );

    public sealed record UpsertEventCategoryRequest(
        string Slug,
        string Name,
        string? ShortDescription,
        string? IconUrl,
        int SortOrder,
        bool IsActive,
        bool ShowInFilter
    );

    public sealed record EventCategoryMutationResponse(bool Success, string? Message = null, int? EventCategoryId = null);

    [HttpGet]
    public async Task<ActionResult<EventCategoryListResponse>> ListPublic(
        [FromQuery] bool filtersOnly = false,
        CancellationToken ct = default)
    {
        var q = _db.EventCategories.AsNoTracking().Where(c => c.IsActive);
        if (filtersOnly) q = q.Where(c => c.ShowInFilter);

        var items = await q
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => ToItem(c))
            .ToListAsync(ct);

        return Ok(new EventCategoryListResponse(true, "OK", items));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpGet("admin")]
    public async Task<ActionResult<EventCategoryListResponse>> AdminList(CancellationToken ct)
    {
        var items = await _db.EventCategories.AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => ToItem(c))
            .ToListAsync(ct);

        return Ok(new EventCategoryListResponse(true, "OK", items));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpGet("admin/{eventCategoryId:int}")]
    public async Task<ActionResult<EventCategoryDetailResponse>> AdminGet(int eventCategoryId, CancellationToken ct)
    {
        var cat = await _db.EventCategories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.EventCategoryId == eventCategoryId, ct);
        if (cat is null) return NotFound(new EventCategoryDetailResponse(false, "Category not found."));
        return Ok(new EventCategoryDetailResponse(true, "OK", ToItem(cat)));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpPost("admin")]
    public async Task<ActionResult<EventCategoryMutationResponse>> Create(
        [FromBody] UpsertEventCategoryRequest? req,
        CancellationToken ct)
    {
        if (req is null) return BadRequest(new EventCategoryMutationResponse(false, "Request is required."));

        var (ok, err, slug) = ValidateSlug(req.Slug);
        if (!ok) return BadRequest(new EventCategoryMutationResponse(false, err));
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new EventCategoryMutationResponse(false, "Category name is required."));

        if (await _db.EventCategories.AnyAsync(c => c.Slug == slug, ct))
            return Conflict(new EventCategoryMutationResponse(false, "A category with this slug already exists."));

        var now = DateTime.Now;
        var cat = new EventCategory
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

        _db.EventCategories.Add(cat);
        await _db.SaveChangesAsync(ct);
        return Ok(new EventCategoryMutationResponse(true, "Category created.", cat.EventCategoryId));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpPut("admin/{eventCategoryId:int}")]
    public async Task<ActionResult<EventCategoryMutationResponse>> Update(
        int eventCategoryId,
        [FromBody] UpsertEventCategoryRequest? req,
        CancellationToken ct)
    {
        if (req is null) return BadRequest(new EventCategoryMutationResponse(false, "Request is required."));

        var (ok, err, slug) = ValidateSlug(req.Slug);
        if (!ok) return BadRequest(new EventCategoryMutationResponse(false, err));
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new EventCategoryMutationResponse(false, "Category name is required."));

        var cat = await _db.EventCategories.FirstOrDefaultAsync(c => c.EventCategoryId == eventCategoryId, ct);
        if (cat is null) return NotFound(new EventCategoryMutationResponse(false, "Category not found."));

        if (await _db.EventCategories.AnyAsync(c => c.Slug == slug && c.EventCategoryId != eventCategoryId, ct))
            return Conflict(new EventCategoryMutationResponse(false, "Another category already uses this slug."));

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
            var events = await _db.Events.Where(e => e.CategorySlug == oldSlug).ToListAsync(ct);
            foreach (var ev in events)
                ev.CategorySlug = cat.Slug;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new EventCategoryMutationResponse(true, "Category updated.", eventCategoryId));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpDelete("admin/{eventCategoryId:int}")]
    public async Task<ActionResult<EventCategoryMutationResponse>> Delete(int eventCategoryId, CancellationToken ct)
    {
        var cat = await _db.EventCategories.FirstOrDefaultAsync(c => c.EventCategoryId == eventCategoryId, ct);
        if (cat is null) return NotFound(new EventCategoryMutationResponse(false, "Category not found."));

        var inUse = await _db.Events.CountAsync(e => e.CategorySlug == cat.Slug, ct);
        if (inUse > 0)
            return Conflict(new EventCategoryMutationResponse(false, $"Cannot delete: {inUse} event(s) use this category. Deactivate it instead."));

        _db.EventCategories.Remove(cat);
        await _db.SaveChangesAsync(ct);
        return Ok(new EventCategoryMutationResponse(true, "Category deleted.", eventCategoryId));
    }

    private static EventCategoryItem ToItem(EventCategory c) => new(
        c.EventCategoryId,
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
