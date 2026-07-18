using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RB_Website_API.Data;
using RB_Website_API.Models;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/events")]
public sealed class EventController : ControllerBase
{
    private static readonly HashSet<string> AllowedImageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    private const long MaxImageBytes = 5 * 1024 * 1024;

    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public EventController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public sealed record EventHighlightItem(int? EventHighlightId, string Text, int SortOrder);

    public sealed record EventPartnerItem(
        int? EventPartnerId,
        string Name,
        string? LogoUrl,
        string? WebsiteUrl,
        int SortOrder
    );

    public sealed record EventListItem(
        int EventId,
        string Slug,
        string Name,
        string Crumb,
        string Tagline,
        string ShortDescription,
        string CategorySlug,
        string? CategoryName,
        string? CitySlug,
        string? CityName,
        string? CityBadgeClass,
        string? FeaturedImageUrl,
        string DateDisplayText,
        string TimeDisplayText,
        string LocationDisplayText,
        string VenueName,
        string AttendanceMode,
        DateTime? StartDate,
        DateTime? EndDate,
        string? DateISO,
        bool IsPublished,
        bool IsFeatured,
        int SortOrder,
        DateTime CreatedAt,
        string Lifecycle
    );

    public sealed record EventDetailItem(
        int EventId,
        string Slug,
        string Name,
        string Crumb,
        string Tagline,
        string ShortDescription,
        string AboutHtml,
        string HighlightsHtml,
        string AssociationHtml,
        string CategorySlug,
        string? CategoryName,
        string? CitySlug,
        string? CityName,
        string? CityBadgeClass,
        string? FeaturedImageUrl,
        string? BannerImageUrl,
        string? ThumbnailUrl,
        string DateDisplayText,
        string TimeDisplayText,
        DateTime? StartDate,
        DateTime? EndDate,
        string? DateISO,
        string AttendanceMode,
        string LocationDisplayText,
        string VenueName,
        string VenueAddress,
        string Landmark,
        string CityNameField,
        string State,
        string Country,
        string? MapsUrl,
        decimal? Latitude,
        decimal? Longitude,
        string PriceDisplay,
        string RegNote,
        string? SeoTitle,
        string? MetaDescription,
        bool IsPublished,
        bool IsFeatured,
        int SortOrder,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        string Lifecycle,
        List<EventHighlightItem> Highlights,
        List<EventPartnerItem> Partners
    );

    public sealed record EventListResponse(
        bool Success,
        string? Message = null,
        List<EventListItem>? Events = null,
        int Total = 0,
        int Page = 1,
        int PageSize = 20
    );

    public sealed record EventDetailResponse(bool Success, string? Message = null, EventDetailItem? Event = null);

    public sealed record ImageUploadResponse(bool Success, string? Message = null, string? ImageUrl = null);

    public sealed record UpsertEventRequest(
        string Slug,
        string Name,
        string Crumb,
        string Tagline,
        string ShortDescription,
        string AboutHtml,
        string HighlightsHtml,
        string AssociationHtml,
        string CategorySlug,
        string? CitySlug,
        string? FeaturedImageUrl,
        string? BannerImageUrl,
        string? ThumbnailUrl,
        string DateDisplayText,
        string TimeDisplayText,
        DateTime? StartDate,
        DateTime? EndDate,
        string? DateISO,
        string AttendanceMode,
        string LocationDisplayText,
        string VenueName,
        string VenueAddress,
        string Landmark,
        string CityName,
        string State,
        string Country,
        string? MapsUrl,
        decimal? Latitude,
        decimal? Longitude,
        string PriceDisplay,
        string RegNote,
        string? SeoTitle,
        string? MetaDescription,
        bool IsPublished,
        bool IsFeatured,
        int SortOrder,
        List<EventHighlightItem>? Highlights,
        List<EventPartnerItem>? Partners
    );

    public sealed record SetPublishedRequest(bool IsPublished);
    public sealed record MutationResponse(bool Success, string? Message = null, int? EventId = null);

    [HttpGet]
    public async Task<ActionResult<EventListResponse>> GetPublished(
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] string? city = null,
        [FromQuery] string? lifecycle = null,
        [FromQuery] bool? featured = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var today = DateTime.Today;
        var q = _db.Events.AsNoTracking().Where(e => e.IsPublished);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            q = q.Where(e =>
                e.Name.ToLower().Contains(s) ||
                e.ShortDescription.ToLower().Contains(s) ||
                e.Tagline.ToLower().Contains(s) ||
                e.VenueName.ToLower().Contains(s) ||
                e.CityName.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var cat = category.Trim().ToLowerInvariant();
            q = q.Where(e => e.CategorySlug.ToLower() == cat);
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            var c = city.Trim().ToLowerInvariant();
            q = q.Where(e => e.CitySlug != null && e.CitySlug.ToLower() == c);
        }

        if (featured == true)
            q = q.Where(e => e.IsFeatured);

        if (!string.IsNullOrWhiteSpace(lifecycle))
        {
            var life = lifecycle.Trim().ToLowerInvariant();
            q = life switch
            {
                "upcoming" => q.Where(e =>
                    e.StartDate == null || e.StartDate.Value.Date > today),
                "ongoing" => q.Where(e =>
                    e.StartDate != null &&
                    e.StartDate.Value.Date <= today &&
                    (e.EndDate == null || e.EndDate.Value.Date >= today)),
                "past" => q.Where(e =>
                    (e.EndDate != null && e.EndDate.Value.Date < today) ||
                    (e.EndDate == null && e.StartDate != null && e.StartDate.Value.Date < today)),
                _ => q
            };
        }

        var total = await q.CountAsync(ct);
        var rows = await q
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.EventId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var labels = await ResolveLabelsAsync(rows, ct);
        var items = rows.Select(e => ToListItem(e, labels, today)).ToList();

        return Ok(new EventListResponse(true, "OK", items, total, page, pageSize));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpGet("admin")]
    public async Task<ActionResult<EventListResponse>> AdminList(
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var today = DateTime.Today;
        var q = _db.Events.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            q = q.Where(e =>
                e.Name.ToLower().Contains(s) ||
                e.Slug.ToLower().Contains(s) ||
                e.ShortDescription.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var cat = category.Trim().ToLowerInvariant();
            q = q.Where(e => e.CategorySlug.ToLower() == cat);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var st = status.Trim().ToLowerInvariant();
            if (st == "published") q = q.Where(e => e.IsPublished);
            else if (st == "draft") q = q.Where(e => !e.IsPublished);
        }

        var total = await q.CountAsync(ct);
        var rows = await q
            .OrderByDescending(e => e.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var labels = await ResolveLabelsAsync(rows, ct);
        var items = rows.Select(e => ToListItem(e, labels, today)).ToList();

        return Ok(new EventListResponse(true, "OK", items, total, page, pageSize));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpGet("admin/{eventId:int}")]
    public async Task<ActionResult<EventDetailResponse>> AdminGet(int eventId, CancellationToken ct)
    {
        var ev = await _db.Events.AsNoTracking()
            .Include(e => e.Highlights)
            .Include(e => e.Partners)
            .FirstOrDefaultAsync(e => e.EventId == eventId, ct);

        if (ev is null) return NotFound(new EventDetailResponse(false, "Event not found."));

        var labels = await ResolveLabelsAsync(new[] { ev }, ct);
        return Ok(new EventDetailResponse(true, "OK", ToDetailItem(ev, labels, DateTime.Today)));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpPost("admin/upload-image")]
    [RequestSizeLimit(MaxImageBytes + 1024 * 64)]
    public async Task<ActionResult<ImageUploadResponse>> UploadImage(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new ImageUploadResponse(false, "Please choose an image file."));

        if (file.Length > MaxImageBytes)
            return BadRequest(new ImageUploadResponse(false, "Image must be 5 MB or smaller."));

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedImageExtensions.Contains(ext))
            return BadRequest(new ImageUploadResponse(false, "Allowed formats: JPG, PNG, WEBP, GIF."));

        var contentType = (file.ContentType ?? "").ToLowerInvariant();
        if (!contentType.StartsWith("image/"))
            return BadRequest(new ImageUploadResponse(false, "File must be an image."));

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var relativeDir = Path.Combine("uploads", "events");
        var absoluteDir = Path.Combine(webRoot, relativeDir);
        Directory.CreateDirectory(absoluteDir);

        var fileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var absolutePath = Path.Combine(absoluteDir, fileName);

        await using (var stream = System.IO.File.Create(absolutePath))
        {
            await file.CopyToAsync(stream, ct);
        }

        return Ok(new ImageUploadResponse(true, "Image uploaded.", $"/uploads/events/{fileName}"));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpPost("admin")]
    public async Task<ActionResult<MutationResponse>> Create(
        [FromBody] UpsertEventRequest? req,
        CancellationToken ct)
    {
        if (req is null) return BadRequest(new MutationResponse(false, "Request is required."));

        var (ok, err, slug) = ValidateSlug(req.Slug);
        if (!ok) return BadRequest(new MutationResponse(false, err));
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new MutationResponse(false, "Event name is required."));

        if (await _db.Events.AnyAsync(e => e.Slug == slug, ct))
            return Conflict(new MutationResponse(false, "An event with this slug already exists."));

        var (catOk, catErr, category) = await ResolveCategoryAsync(req.CategorySlug, ct);
        if (!catOk) return BadRequest(new MutationResponse(false, catErr));

        var (cityOk, cityErr, city) = await ResolveCityAsync(req.CitySlug, ct);
        if (!cityOk) return BadRequest(new MutationResponse(false, cityErr));

        var now = DateTime.Now;
        var ev = new Models.Event
        {
            Slug = slug!,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = GetActorId(),
        };

        ApplyRequest(ev, req, category!, city);
        SyncChildren(ev, req.Highlights, req.Partners);

        _db.Events.Add(ev);
        await _db.SaveChangesAsync(ct);
        return Ok(new MutationResponse(true, "Event created.", ev.EventId));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpPut("admin/{eventId:int}")]
    public async Task<ActionResult<MutationResponse>> Update(
        int eventId,
        [FromBody] UpsertEventRequest? req,
        CancellationToken ct)
    {
        if (req is null) return BadRequest(new MutationResponse(false, "Request is required."));
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new MutationResponse(false, "Event name is required."));

        var ev = await _db.Events
            .Include(e => e.Highlights)
            .Include(e => e.Partners)
            .FirstOrDefaultAsync(e => e.EventId == eventId, ct);

        if (ev is null) return NotFound(new MutationResponse(false, "Event not found."));

        var (catOk, catErr, category) = await ResolveCategoryAsync(req.CategorySlug, ct);
        if (!catOk) return BadRequest(new MutationResponse(false, catErr));

        var (cityOk, cityErr, city) = await ResolveCityAsync(req.CitySlug, ct);
        if (!cityOk) return BadRequest(new MutationResponse(false, cityErr));

        // Slug is immutable after create (matches blog pattern); ignore req.Slug on update.
        ApplyRequest(ev, req, category!, city);
        ev.UpdatedAt = DateTime.Now;

        _db.EventHighlights.RemoveRange(ev.Highlights);
        _db.EventPartners.RemoveRange(ev.Partners);
        ev.Highlights.Clear();
        ev.Partners.Clear();
        SyncChildren(ev, req.Highlights, req.Partners);

        await _db.SaveChangesAsync(ct);
        return Ok(new MutationResponse(true, "Event updated.", eventId));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpPatch("admin/{eventId:int}/publish")]
    public async Task<ActionResult<MutationResponse>> SetPublished(
        int eventId,
        [FromBody] SetPublishedRequest? req,
        CancellationToken ct)
    {
        if (req is null) return BadRequest(new MutationResponse(false, "Request is required."));

        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId, ct);
        if (ev is null) return NotFound(new MutationResponse(false, "Event not found."));

        ev.IsPublished = req.IsPublished;
        ev.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync(ct);

        return Ok(new MutationResponse(true, req.IsPublished ? "Event published." : "Event unpublished.", eventId));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpDelete("admin/{eventId:int}")]
    public async Task<ActionResult<MutationResponse>> Delete(int eventId, CancellationToken ct)
    {
        var ev = await _db.Events
            .Include(e => e.Highlights)
            .Include(e => e.Partners)
            .FirstOrDefaultAsync(e => e.EventId == eventId, ct);

        if (ev is null) return NotFound(new MutationResponse(false, "Event not found."));

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync(ct);
        return Ok(new MutationResponse(true, "Event deleted.", eventId));
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<EventDetailResponse>> GetBySlug(string slug, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return BadRequest(new EventDetailResponse(false, "Slug is required."));

        var normalized = slug.Trim().ToLowerInvariant();
        var ev = await _db.Events.AsNoTracking()
            .Include(e => e.Highlights)
            .Include(e => e.Partners)
            .Where(e => e.Slug == normalized && e.IsPublished)
            .FirstOrDefaultAsync(ct);

        if (ev is null)
            return NotFound(new EventDetailResponse(false, "Event not found."));

        var labels = await ResolveLabelsAsync(new[] { ev }, ct);
        return Ok(new EventDetailResponse(true, "OK", ToDetailItem(ev, labels, DateTime.Today)));
    }

    private static void ApplyRequest(Models.Event ev, UpsertEventRequest req, EventCategory category, EventCity? city)
    {
        ev.Name = req.Name.Trim();
        ev.Crumb = (req.Crumb ?? "").Trim();
        ev.Tagline = (req.Tagline ?? "").Trim();
        ev.ShortDescription = (req.ShortDescription ?? "").Trim();
        ev.AboutHtml = req.AboutHtml ?? "";
        ev.HighlightsHtml = req.HighlightsHtml ?? "";
        ev.AssociationHtml = req.AssociationHtml ?? "";
        ev.CategorySlug = category.Slug;
        ev.CitySlug = city?.Slug;
        ev.FeaturedImageUrl = NullIfWhiteSpace(req.FeaturedImageUrl);
        ev.BannerImageUrl = NullIfWhiteSpace(req.BannerImageUrl);
        ev.ThumbnailUrl = NullIfWhiteSpace(req.ThumbnailUrl);
        ev.DateDisplayText = (req.DateDisplayText ?? "").Trim();
        ev.TimeDisplayText = (req.TimeDisplayText ?? "").Trim();
        ev.StartDate = req.StartDate;
        ev.EndDate = req.EndDate;
        ev.DateISO = NullIfWhiteSpace(req.DateISO);
        ev.AttendanceMode = NormalizeAttendanceMode(req.AttendanceMode);
        ev.LocationDisplayText = (req.LocationDisplayText ?? "").Trim();
        ev.VenueName = (req.VenueName ?? "").Trim();
        ev.VenueAddress = (req.VenueAddress ?? "").Trim();
        ev.Landmark = (req.Landmark ?? "").Trim();
        ev.CityName = !string.IsNullOrWhiteSpace(req.CityName)
            ? req.CityName.Trim()
            : (city?.Name ?? "");
        ev.State = (req.State ?? "").Trim();
        ev.Country = string.IsNullOrWhiteSpace(req.Country) ? "India" : req.Country.Trim();
        ev.MapsUrl = NullIfWhiteSpace(req.MapsUrl);
        ev.Latitude = req.Latitude;
        ev.Longitude = req.Longitude;
        ev.PriceDisplay = (req.PriceDisplay ?? "").Trim();
        ev.RegNote = (req.RegNote ?? "").Trim();
        ev.SeoTitle = NullIfWhiteSpace(req.SeoTitle);
        ev.MetaDescription = NullIfWhiteSpace(req.MetaDescription);
        ev.IsPublished = req.IsPublished;
        ev.IsFeatured = req.IsFeatured;
        // Listing order is always latest-created first; SortOrder is unused for events.
        ev.SortOrder = 0;
    }

    private static void SyncChildren(
        Models.Event ev,
        List<EventHighlightItem>? highlights,
        List<EventPartnerItem>? partners)
    {
        var hi = (highlights ?? [])
            .Where(h => !string.IsNullOrWhiteSpace(h.Text))
            .Select((h, i) => new EventHighlight
            {
                Text = h.Text.Trim(),
                SortOrder = h.SortOrder != 0 ? h.SortOrder : i + 1,
            })
            .ToList();

        var partnersList = (partners ?? [])
            .Where(p => !string.IsNullOrWhiteSpace(p.Name))
            .Select((p, i) => new EventPartner
            {
                Name = p.Name.Trim(),
                LogoUrl = NullIfWhiteSpace(p.LogoUrl),
                WebsiteUrl = NullIfWhiteSpace(p.WebsiteUrl),
                SortOrder = p.SortOrder != 0 ? p.SortOrder : i + 1,
            })
            .ToList();

        foreach (var h in hi) ev.Highlights.Add(h);
        foreach (var p in partnersList) ev.Partners.Add(p);
    }

    private async Task<(bool Ok, string? Error, EventCategory? Category)> ResolveCategoryAsync(
        string? categorySlug,
        CancellationToken ct)
    {
        var slug = (categorySlug ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(slug))
            return (false, "Category is required.", null);

        var category = await _db.EventCategories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Slug == slug && c.IsActive, ct);

        if (category is null)
            return (false, "Please choose a valid active category.", null);

        return (true, null, category);
    }

    private async Task<(bool Ok, string? Error, EventCity? City)> ResolveCityAsync(
        string? citySlug,
        CancellationToken ct)
    {
        var slug = (citySlug ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(slug))
            return (true, null, null);

        var city = await _db.EventCities.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Slug == slug && c.IsActive, ct);

        if (city is null)
            return (false, "Please choose a valid active city.", null);

        return (true, null, city);
    }

    private sealed record LabelMaps(
        Dictionary<string, string> CategoryNames,
        Dictionary<string, (string Name, string? BadgeClass)> Cities
    );

    private async Task<LabelMaps> ResolveLabelsAsync(IEnumerable<Models.Event> events, CancellationToken ct)
    {
        var catSlugs = events.Select(e => e.CategorySlug).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
        var citySlugs = events.Select(e => e.CitySlug).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();

        var cats = await _db.EventCategories.AsNoTracking()
            .Where(c => catSlugs.Contains(c.Slug))
            .ToDictionaryAsync(c => c.Slug, c => c.Name, ct);

        var cities = await _db.EventCities.AsNoTracking()
            .Where(c => citySlugs.Contains(c.Slug!))
            .ToDictionaryAsync(c => c.Slug, c => (c.Name, c.BadgeClass), ct);

        return new LabelMaps(cats, cities);
    }

    private static EventListItem ToListItem(Models.Event e, LabelMaps labels, DateTime today)
    {
        labels.CategoryNames.TryGetValue(e.CategorySlug, out var catName);
        string? cityName = null;
        string? badge = null;
        if (!string.IsNullOrWhiteSpace(e.CitySlug) && labels.Cities.TryGetValue(e.CitySlug, out var city))
        {
            cityName = city.Name;
            badge = city.BadgeClass;
        }
        cityName ??= string.IsNullOrWhiteSpace(e.CityName) ? null : e.CityName;

        return new EventListItem(
            e.EventId,
            e.Slug,
            e.Name,
            e.Crumb,
            e.Tagline,
            e.ShortDescription,
            e.CategorySlug,
            catName,
            e.CitySlug,
            cityName,
            badge,
            e.FeaturedImageUrl,
            e.DateDisplayText,
            e.TimeDisplayText,
            ResolveLocation(e),
            e.VenueName,
            e.AttendanceMode,
            e.StartDate,
            e.EndDate,
            e.DateISO,
            e.IsPublished,
            e.IsFeatured,
            e.SortOrder,
            e.CreatedAt,
            ComputeLifecycle(e, today));
    }

    private static EventDetailItem ToDetailItem(Models.Event e, LabelMaps labels, DateTime today)
    {
        labels.CategoryNames.TryGetValue(e.CategorySlug, out var catName);
        string? cityName = null;
        string? badge = null;
        if (!string.IsNullOrWhiteSpace(e.CitySlug) && labels.Cities.TryGetValue(e.CitySlug, out var city))
        {
            cityName = city.Name;
            badge = city.BadgeClass;
        }
        cityName ??= string.IsNullOrWhiteSpace(e.CityName) ? null : e.CityName;

        var highlights = e.Highlights
            .OrderBy(h => h.SortOrder)
            .Select(h => new EventHighlightItem(h.EventHighlightId, h.Text, h.SortOrder))
            .ToList();

        var partners = e.Partners
            .OrderBy(p => p.SortOrder)
            .Select(p => new EventPartnerItem(p.EventPartnerId, p.Name, p.LogoUrl, p.WebsiteUrl, p.SortOrder))
            .ToList();

        return new EventDetailItem(
            e.EventId,
            e.Slug,
            e.Name,
            e.Crumb,
            e.Tagline,
            e.ShortDescription,
            e.AboutHtml,
            e.HighlightsHtml,
            e.AssociationHtml,
            e.CategorySlug,
            catName,
            e.CitySlug,
            cityName,
            badge,
            e.FeaturedImageUrl,
            e.BannerImageUrl,
            e.ThumbnailUrl,
            e.DateDisplayText,
            e.TimeDisplayText,
            e.StartDate,
            e.EndDate,
            e.DateISO,
            e.AttendanceMode,
            ResolveLocation(e),
            e.VenueName,
            e.VenueAddress,
            e.Landmark,
            e.CityName,
            e.State,
            e.Country,
            e.MapsUrl,
            e.Latitude,
            e.Longitude,
            e.PriceDisplay,
            e.RegNote,
            e.SeoTitle,
            e.MetaDescription,
            e.IsPublished,
            e.IsFeatured,
            e.SortOrder,
            e.CreatedAt,
            e.UpdatedAt,
            ComputeLifecycle(e, today),
            highlights,
            partners);
    }

    private static string ResolveLocation(Models.Event e)
    {
        if (!string.IsNullOrWhiteSpace(e.LocationDisplayText))
            return e.LocationDisplayText.Trim();

        var parts = new[] { e.VenueName, e.Landmark, e.CityName, e.State }
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim());
        return string.Join(", ", parts);
    }

    private static string ComputeLifecycle(Models.Event e, DateTime today)
    {
        if (e.StartDate is null && e.EndDate is null)
            return "unscheduled";

        var start = e.StartDate?.Date;
        var end = e.EndDate?.Date ?? start;

        if (start is not null && start > today)
            return "upcoming";
        if (start is not null && end is not null && start <= today && end >= today)
            return "ongoing";
        if (end is not null && end < today)
            return "past";
        if (start is not null && start < today && e.EndDate is null)
            return "past";

        return "unscheduled";
    }

    private static string NormalizeAttendanceMode(string? mode)
    {
        var m = (mode ?? "").Trim();
        if (string.IsNullOrEmpty(m)) return "";
        var lower = m.ToLowerInvariant();
        return lower switch
        {
            "online" => "Online",
            "offline" => "Offline",
            "hybrid" => "Hybrid",
            _ => m.Length > 20 ? m[..20] : m
        };
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static (bool Ok, string? Error, string? Slug) ValidateSlug(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return (false, "Slug is required.", null);

        var slug = raw.Trim().ToLowerInvariant();
        if (!Regex.IsMatch(slug, @"^[a-z0-9]+(?:-[a-z0-9]+)*$"))
            return (false, "Slug must contain only lowercase letters, numbers, and hyphens.", null);

        if (slug.Length > 200)
            return (false, "Slug must be 200 characters or fewer.", null);

        return (true, null, slug);
    }

    private Guid? GetActorId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
