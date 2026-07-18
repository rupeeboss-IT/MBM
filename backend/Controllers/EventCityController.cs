using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RB_Website_API.Data;
using RB_Website_API.Models;
using System.Text.RegularExpressions;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/event-cities")]
public sealed class EventCityController : ControllerBase
{
    private readonly AppDbContext _db;

    public EventCityController(AppDbContext db) => _db = db;

    public sealed record EventCityItem(
        int EventCityId,
        string Slug,
        string Name,
        string? BadgeClass,
        int SortOrder,
        bool IsActive,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );

    public sealed record EventCityListResponse(
        bool Success,
        string? Message = null,
        List<EventCityItem>? Cities = null
    );

    public sealed record EventCityDetailResponse(
        bool Success,
        string? Message = null,
        EventCityItem? City = null
    );

    public sealed record UpsertEventCityRequest(
        string Slug,
        string Name,
        string? BadgeClass,
        int SortOrder,
        bool IsActive
    );

    public sealed record EventCityMutationResponse(bool Success, string? Message = null, int? EventCityId = null);

    [HttpGet]
    public async Task<ActionResult<EventCityListResponse>> ListPublic(CancellationToken ct = default)
    {
        var items = await _db.EventCities.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => ToItem(c))
            .ToListAsync(ct);

        return Ok(new EventCityListResponse(true, "OK", items));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpGet("admin")]
    public async Task<ActionResult<EventCityListResponse>> AdminList(CancellationToken ct)
    {
        var items = await _db.EventCities.AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => ToItem(c))
            .ToListAsync(ct);

        return Ok(new EventCityListResponse(true, "OK", items));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpGet("admin/{eventCityId:int}")]
    public async Task<ActionResult<EventCityDetailResponse>> AdminGet(int eventCityId, CancellationToken ct)
    {
        var city = await _db.EventCities.AsNoTracking()
            .FirstOrDefaultAsync(c => c.EventCityId == eventCityId, ct);
        if (city is null) return NotFound(new EventCityDetailResponse(false, "City not found."));
        return Ok(new EventCityDetailResponse(true, "OK", ToItem(city)));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpPost("admin")]
    public async Task<ActionResult<EventCityMutationResponse>> Create(
        [FromBody] UpsertEventCityRequest? req,
        CancellationToken ct)
    {
        if (req is null) return BadRequest(new EventCityMutationResponse(false, "Request is required."));

        var (ok, err, slug) = ValidateSlug(req.Slug);
        if (!ok) return BadRequest(new EventCityMutationResponse(false, err));
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new EventCityMutationResponse(false, "City name is required."));

        if (await _db.EventCities.AnyAsync(c => c.Slug == slug, ct))
            return Conflict(new EventCityMutationResponse(false, "A city with this slug already exists."));

        var now = DateTime.Now;
        var city = new EventCity
        {
            Slug = slug!,
            Name = req.Name.Trim(),
            BadgeClass = string.IsNullOrWhiteSpace(req.BadgeClass) ? null : req.BadgeClass.Trim(),
            SortOrder = req.SortOrder,
            IsActive = req.IsActive,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.EventCities.Add(city);
        await _db.SaveChangesAsync(ct);
        return Ok(new EventCityMutationResponse(true, "City created.", city.EventCityId));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpPut("admin/{eventCityId:int}")]
    public async Task<ActionResult<EventCityMutationResponse>> Update(
        int eventCityId,
        [FromBody] UpsertEventCityRequest? req,
        CancellationToken ct)
    {
        if (req is null) return BadRequest(new EventCityMutationResponse(false, "Request is required."));

        var (ok, err, slug) = ValidateSlug(req.Slug);
        if (!ok) return BadRequest(new EventCityMutationResponse(false, err));
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new EventCityMutationResponse(false, "City name is required."));

        var city = await _db.EventCities.FirstOrDefaultAsync(c => c.EventCityId == eventCityId, ct);
        if (city is null) return NotFound(new EventCityMutationResponse(false, "City not found."));

        if (await _db.EventCities.AnyAsync(c => c.Slug == slug && c.EventCityId != eventCityId, ct))
            return Conflict(new EventCityMutationResponse(false, "Another city already uses this slug."));

        var oldSlug = city.Slug;
        city.Slug = slug!;
        city.Name = req.Name.Trim();
        city.BadgeClass = string.IsNullOrWhiteSpace(req.BadgeClass) ? null : req.BadgeClass.Trim();
        city.SortOrder = req.SortOrder;
        city.IsActive = req.IsActive;
        city.UpdatedAt = DateTime.Now;

        if (!string.Equals(oldSlug, city.Slug, StringComparison.OrdinalIgnoreCase))
        {
            var events = await _db.Events.Where(e => e.CitySlug == oldSlug).ToListAsync(ct);
            foreach (var ev in events)
            {
                ev.CitySlug = city.Slug;
                if (string.IsNullOrWhiteSpace(ev.CityName) ||
                    string.Equals(ev.CityName, oldSlug, StringComparison.OrdinalIgnoreCase))
                {
                    ev.CityName = city.Name;
                }
            }
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new EventCityMutationResponse(true, "City updated.", eventCityId));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpDelete("admin/{eventCityId:int}")]
    public async Task<ActionResult<EventCityMutationResponse>> Delete(int eventCityId, CancellationToken ct)
    {
        var city = await _db.EventCities.FirstOrDefaultAsync(c => c.EventCityId == eventCityId, ct);
        if (city is null) return NotFound(new EventCityMutationResponse(false, "City not found."));

        var inUse = await _db.Events.CountAsync(e => e.CitySlug == city.Slug, ct);
        if (inUse > 0)
            return Conflict(new EventCityMutationResponse(false, $"Cannot delete: {inUse} event(s) use this city. Deactivate it instead."));

        _db.EventCities.Remove(city);
        await _db.SaveChangesAsync(ct);
        return Ok(new EventCityMutationResponse(true, "City deleted.", eventCityId));
    }

    private static EventCityItem ToItem(EventCity c) => new(
        c.EventCityId,
        c.Slug,
        c.Name,
        c.BadgeClass,
        c.SortOrder,
        c.IsActive,
        c.CreatedAt,
        c.UpdatedAt);

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
