using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RB_Website_API.Data;
using RB_Website_API.Models;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/schemes")]
public sealed class SchemeController : ControllerBase
{
    private readonly AppDbContext _db;

    public SchemeController(AppDbContext db) => _db = db;

    public sealed record SchemeBenefitItem(int? SchemeBenefitId, string Text, int SortOrder);

    public sealed record SchemeCardHighlightItem(int? SchemeCardHighlightId, string Text, int SortOrder);

    public sealed record SchemeListItem(
        int SchemeId,
        string Slug,
        string Name,
        string Crumb,
        string Tagline,
        string ShortDescription,
        string CategorySlug,
        string? CategoryName,
        string PrimaryBadgeText,
        string PrimaryBadgeClass,
        string SecondaryBadgeText,
        string SecondaryBadgeClass,
        string HomeTitle,
        string HomeBadgeText,
        string HomeBadgeClass,
        string HomeDescription,
        bool IsPublished,
        bool IsFeatured,
        int SortOrder,
        DateTime CreatedAt,
        List<SchemeCardHighlightItem> CardHighlights
    );

    public sealed record SchemeDetailItem(
        int SchemeId,
        string Slug,
        string Name,
        string Crumb,
        string Tagline,
        string ShortDescription,
        string ContentHtml,
        string CategorySlug,
        string? CategoryName,
        string PrimaryBadgeText,
        string PrimaryBadgeClass,
        string SecondaryBadgeText,
        string SecondaryBadgeClass,
        string HomeTitle,
        string HomeBadgeText,
        string HomeBadgeClass,
        string HomeDescription,
        string? SeoTitle,
        string? MetaDescription,
        bool IsPublished,
        bool IsFeatured,
        int SortOrder,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        List<SchemeBenefitItem> Benefits,
        List<SchemeCardHighlightItem> CardHighlights
    );

    public sealed record SchemeListResponse(
        bool Success,
        string? Message = null,
        List<SchemeListItem>? Schemes = null,
        int Total = 0,
        int Page = 1,
        int PageSize = 20
    );

    public sealed record SchemeDetailResponse(bool Success, string? Message = null, SchemeDetailItem? Scheme = null);

    public sealed record UpsertSchemeRequest(
        string Slug,
        string Name,
        string Crumb,
        string Tagline,
        string ShortDescription,
        string ContentHtml,
        string CategorySlug,
        string PrimaryBadgeText,
        string PrimaryBadgeClass,
        string SecondaryBadgeText,
        string SecondaryBadgeClass,
        string HomeTitle,
        string HomeBadgeText,
        string HomeBadgeClass,
        string HomeDescription,
        string? SeoTitle,
        string? MetaDescription,
        bool IsPublished,
        bool IsFeatured,
        int SortOrder,
        List<SchemeBenefitItem>? Benefits,
        List<SchemeCardHighlightItem>? CardHighlights
    );

    public sealed record SetPublishedRequest(bool IsPublished);
    public sealed record MutationResponse(bool Success, string? Message = null, int? SchemeId = null);

    [HttpGet]
    public async Task<ActionResult<SchemeListResponse>> GetPublished(
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] bool? featured = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = _db.Schemes.AsNoTracking()
            .Include(s => s.CardHighlights)
            .Where(s => s.IsPublished);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            q = q.Where(x =>
                x.Name.ToLower().Contains(s) ||
                x.ShortDescription.ToLower().Contains(s) ||
                x.Tagline.ToLower().Contains(s) ||
                x.Crumb.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var cat = category.Trim().ToLowerInvariant();
            q = q.Where(x => x.CategorySlug.ToLower() == cat);
        }

        if (featured == true)
            q = q.Where(x => x.IsFeatured);

        var total = await q.CountAsync(ct);
        var rows = await q
            .OrderBy(x => x.SortOrder)
            .ThenByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.SchemeId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var labels = await ResolveCategoryNamesAsync(rows, ct);
        var items = rows.Select(s => ToListItem(s, labels)).ToList();

        return Ok(new SchemeListResponse(true, "OK", items, total, page, pageSize));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpGet("admin")]
    public async Task<ActionResult<SchemeListResponse>> AdminList(
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var q = _db.Schemes.AsNoTracking()
            .Include(s => s.CardHighlights)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            q = q.Where(x =>
                x.Name.ToLower().Contains(s) ||
                x.Slug.ToLower().Contains(s) ||
                x.ShortDescription.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var cat = category.Trim().ToLowerInvariant();
            q = q.Where(x => x.CategorySlug.ToLower() == cat);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var st = status.Trim().ToLowerInvariant();
            if (st == "published") q = q.Where(x => x.IsPublished);
            else if (st == "draft") q = q.Where(x => !x.IsPublished);
        }

        var total = await q.CountAsync(ct);
        var rows = await q
            .OrderByDescending(x => x.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var labels = await ResolveCategoryNamesAsync(rows, ct);
        var items = rows.Select(s => ToListItem(s, labels)).ToList();

        return Ok(new SchemeListResponse(true, "OK", items, total, page, pageSize));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpGet("admin/{schemeId:int}")]
    public async Task<ActionResult<SchemeDetailResponse>> AdminGet(int schemeId, CancellationToken ct)
    {
        var scheme = await _db.Schemes.AsNoTracking()
            .Include(s => s.Benefits)
            .Include(s => s.CardHighlights)
            .FirstOrDefaultAsync(s => s.SchemeId == schemeId, ct);

        if (scheme is null) return NotFound(new SchemeDetailResponse(false, "Scheme not found."));

        var labels = await ResolveCategoryNamesAsync(new[] { scheme }, ct);
        return Ok(new SchemeDetailResponse(true, "OK", ToDetailItem(scheme, labels)));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpPost("admin")]
    public async Task<ActionResult<MutationResponse>> Create(
        [FromBody] UpsertSchemeRequest? req,
        CancellationToken ct)
    {
        if (req is null) return BadRequest(new MutationResponse(false, "Request is required."));

        var (ok, err, slug) = ValidateSlug(req.Slug);
        if (!ok) return BadRequest(new MutationResponse(false, err));
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new MutationResponse(false, "Scheme name is required."));

        if (await _db.Schemes.AnyAsync(s => s.Slug == slug, ct))
            return Conflict(new MutationResponse(false, "A scheme with this slug already exists."));

        var (catOk, catErr, category) = await ResolveCategoryAsync(req.CategorySlug, ct);
        if (!catOk) return BadRequest(new MutationResponse(false, catErr));

        var now = DateTime.Now;
        var scheme = new Models.Scheme
        {
            Slug = slug!,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = GetActorId(),
        };

        ApplyRequest(scheme, req, category!);
        SyncChildren(scheme, req.Benefits, req.CardHighlights);

        _db.Schemes.Add(scheme);
        await _db.SaveChangesAsync(ct);
        return Ok(new MutationResponse(true, "Scheme created.", scheme.SchemeId));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpPut("admin/{schemeId:int}")]
    public async Task<ActionResult<MutationResponse>> Update(
        int schemeId,
        [FromBody] UpsertSchemeRequest? req,
        CancellationToken ct)
    {
        if (req is null) return BadRequest(new MutationResponse(false, "Request is required."));
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new MutationResponse(false, "Scheme name is required."));

        var scheme = await _db.Schemes
            .Include(s => s.Benefits)
            .Include(s => s.CardHighlights)
            .FirstOrDefaultAsync(s => s.SchemeId == schemeId, ct);

        if (scheme is null) return NotFound(new MutationResponse(false, "Scheme not found."));

        var (catOk, catErr, category) = await ResolveCategoryAsync(req.CategorySlug, ct);
        if (!catOk) return BadRequest(new MutationResponse(false, catErr));

        ApplyRequest(scheme, req, category!);
        scheme.UpdatedAt = DateTime.Now;

        _db.SchemeBenefits.RemoveRange(scheme.Benefits);
        _db.SchemeCardHighlights.RemoveRange(scheme.CardHighlights);
        scheme.Benefits.Clear();
        scheme.CardHighlights.Clear();
        SyncChildren(scheme, req.Benefits, req.CardHighlights);

        await _db.SaveChangesAsync(ct);
        return Ok(new MutationResponse(true, "Scheme updated.", schemeId));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpPatch("admin/{schemeId:int}/publish")]
    public async Task<ActionResult<MutationResponse>> SetPublished(
        int schemeId,
        [FromBody] SetPublishedRequest? req,
        CancellationToken ct)
    {
        if (req is null) return BadRequest(new MutationResponse(false, "Request is required."));

        var scheme = await _db.Schemes.FirstOrDefaultAsync(s => s.SchemeId == schemeId, ct);
        if (scheme is null) return NotFound(new MutationResponse(false, "Scheme not found."));

        scheme.IsPublished = req.IsPublished;
        scheme.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync(ct);

        return Ok(new MutationResponse(true, req.IsPublished ? "Scheme published." : "Scheme unpublished.", schemeId));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpDelete("admin/{schemeId:int}")]
    public async Task<ActionResult<MutationResponse>> Delete(int schemeId, CancellationToken ct)
    {
        var scheme = await _db.Schemes
            .Include(s => s.Benefits)
            .Include(s => s.CardHighlights)
            .FirstOrDefaultAsync(s => s.SchemeId == schemeId, ct);

        if (scheme is null) return NotFound(new MutationResponse(false, "Scheme not found."));

        _db.Schemes.Remove(scheme);
        await _db.SaveChangesAsync(ct);
        return Ok(new MutationResponse(true, "Scheme deleted.", schemeId));
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<SchemeDetailResponse>> GetBySlug(string slug, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return BadRequest(new SchemeDetailResponse(false, "Slug is required."));

        var normalized = slug.Trim().ToLowerInvariant();
        var scheme = await _db.Schemes.AsNoTracking()
            .Include(s => s.Benefits)
            .Include(s => s.CardHighlights)
            .Where(s => s.Slug == normalized && s.IsPublished)
            .FirstOrDefaultAsync(ct);

        if (scheme is null)
            return NotFound(new SchemeDetailResponse(false, "Scheme not found."));

        var labels = await ResolveCategoryNamesAsync(new[] { scheme }, ct);
        return Ok(new SchemeDetailResponse(true, "OK", ToDetailItem(scheme, labels)));
    }

    private static void ApplyRequest(Models.Scheme scheme, UpsertSchemeRequest req, SchemeCategory category)
    {
        scheme.Name = req.Name.Trim();
        scheme.Crumb = (req.Crumb ?? "").Trim();
        scheme.Tagline = (req.Tagline ?? "").Trim();
        scheme.ShortDescription = (req.ShortDescription ?? "").Trim();
        scheme.ContentHtml = req.ContentHtml ?? "";
        scheme.CategorySlug = category.Slug;
        scheme.PrimaryBadgeText = category.Name;
        scheme.PrimaryBadgeClass = NormalizeBadgeClass(req.PrimaryBadgeClass, "badge-green");
        scheme.SecondaryBadgeText = (req.SecondaryBadgeText ?? "").Trim();
        scheme.SecondaryBadgeClass = NormalizeBadgeClass(req.SecondaryBadgeClass, "badge-orange");
        scheme.HomeTitle = (req.HomeTitle ?? "").Trim();
        scheme.HomeBadgeText = (req.HomeBadgeText ?? "").Trim();
        scheme.HomeBadgeClass = string.IsNullOrWhiteSpace(req.HomeBadgeClass)
            ? ""
            : NormalizeBadgeClass(req.HomeBadgeClass, "");
        scheme.HomeDescription = (req.HomeDescription ?? "").Trim();
        scheme.SeoTitle = NullIfWhiteSpace(req.SeoTitle);
        scheme.MetaDescription = NullIfWhiteSpace(req.MetaDescription);
        scheme.IsPublished = req.IsPublished;
        scheme.IsFeatured = req.IsFeatured;
        scheme.SortOrder = req.SortOrder;
    }

    private static void SyncChildren(
        Models.Scheme scheme,
        List<SchemeBenefitItem>? benefits,
        List<SchemeCardHighlightItem>? cardHighlights)
    {
        var benefitRows = (benefits ?? [])
            .Where(b => !string.IsNullOrWhiteSpace(b.Text))
            .Select((b, i) => new SchemeBenefit
            {
                Text = b.Text.Trim(),
                SortOrder = b.SortOrder != 0 ? b.SortOrder : i + 1,
            })
            .ToList();

        var cardRows = (cardHighlights ?? [])
            .Where(h => !string.IsNullOrWhiteSpace(h.Text))
            .Select((h, i) => new SchemeCardHighlight
            {
                Text = h.Text.Trim(),
                SortOrder = h.SortOrder != 0 ? h.SortOrder : i + 1,
            })
            .ToList();

        foreach (var b in benefitRows) scheme.Benefits.Add(b);
        foreach (var h in cardRows) scheme.CardHighlights.Add(h);
    }

    private async Task<(bool Ok, string? Error, SchemeCategory? Category)> ResolveCategoryAsync(
        string? categorySlug,
        CancellationToken ct)
    {
        var slug = (categorySlug ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(slug))
            return (false, "Category is required.", null);

        var category = await _db.SchemeCategories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Slug == slug && c.IsActive, ct);

        if (category is null)
            return (false, "Please choose a valid active category.", null);

        return (true, null, category);
    }

    private async Task<Dictionary<string, string>> ResolveCategoryNamesAsync(
        IEnumerable<Models.Scheme> schemes,
        CancellationToken ct)
    {
        var catSlugs = schemes.Select(s => s.CategorySlug).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
        return await _db.SchemeCategories.AsNoTracking()
            .Where(c => catSlugs.Contains(c.Slug))
            .ToDictionaryAsync(c => c.Slug, c => c.Name, ct);
    }

    private static SchemeListItem ToListItem(Models.Scheme s, Dictionary<string, string> categoryNames)
    {
        categoryNames.TryGetValue(s.CategorySlug, out var catName);
        var highlights = s.CardHighlights
            .OrderBy(h => h.SortOrder)
            .ThenBy(h => h.SchemeCardHighlightId)
            .Select(h => new SchemeCardHighlightItem(h.SchemeCardHighlightId, h.Text, h.SortOrder))
            .ToList();

        return new SchemeListItem(
            s.SchemeId,
            s.Slug,
            s.Name,
            s.Crumb,
            s.Tagline,
            s.ShortDescription,
            s.CategorySlug,
            catName,
            catName ?? s.PrimaryBadgeText,
            s.PrimaryBadgeClass,
            s.SecondaryBadgeText,
            s.SecondaryBadgeClass,
            s.HomeTitle,
            s.HomeBadgeText,
            s.HomeBadgeClass,
            s.HomeDescription,
            s.IsPublished,
            s.IsFeatured,
            s.SortOrder,
            s.CreatedAt,
            highlights);
    }

    private static SchemeDetailItem ToDetailItem(Models.Scheme s, Dictionary<string, string> categoryNames)
    {
        categoryNames.TryGetValue(s.CategorySlug, out var catName);
        var benefits = s.Benefits
            .OrderBy(b => b.SortOrder)
            .ThenBy(b => b.SchemeBenefitId)
            .Select(b => new SchemeBenefitItem(b.SchemeBenefitId, b.Text, b.SortOrder))
            .ToList();
        var highlights = s.CardHighlights
            .OrderBy(h => h.SortOrder)
            .ThenBy(h => h.SchemeCardHighlightId)
            .Select(h => new SchemeCardHighlightItem(h.SchemeCardHighlightId, h.Text, h.SortOrder))
            .ToList();

        return new SchemeDetailItem(
            s.SchemeId,
            s.Slug,
            s.Name,
            s.Crumb,
            s.Tagline,
            s.ShortDescription,
            s.ContentHtml,
            s.CategorySlug,
            catName,
            catName ?? s.PrimaryBadgeText,
            s.PrimaryBadgeClass,
            s.SecondaryBadgeText,
            s.SecondaryBadgeClass,
            s.HomeTitle,
            s.HomeBadgeText,
            s.HomeBadgeClass,
            s.HomeDescription,
            s.SeoTitle,
            s.MetaDescription,
            s.IsPublished,
            s.IsFeatured,
            s.SortOrder,
            s.CreatedAt,
            s.UpdatedAt,
            benefits,
            highlights);
    }

    private Guid? GetActorId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeBadgeClass(string? value, string fallback)
    {
        var v = (value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(v)) return fallback;
        if (!v.StartsWith("badge-", StringComparison.OrdinalIgnoreCase))
            v = "badge-" + v.TrimStart('-');
        return v.ToLowerInvariant();
    }

    private static (bool Ok, string? Error, string? Slug) ValidateSlug(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return (false, "Slug is required.", null);

        var normalized = slug.Trim().ToLowerInvariant();
        if (normalized.Length > 200)
            return (false, "Slug must be 200 characters or fewer.", null);
        if (!Regex.IsMatch(normalized, @"^[a-z0-9]+(?:-[a-z0-9]+)*$"))
            return (false, "Slug may only contain lowercase letters, numbers, and hyphens.", null);

        return (true, null, normalized);
    }
}
