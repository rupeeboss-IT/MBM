using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RB_Website_API.Auth;
using RB_Website_API.Data;
using RB_Website_API.Models;
using System.Security.Claims;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/plans")]
public sealed class PlanController : ControllerBase
{
    private readonly AppDbContext _db;

    public PlanController(AppDbContext db) => _db = db;

    public sealed record PlanFeatureItem(
        int? PlanFeatureId,
        string Text,
        string? Description,
        string? OfferingSlug,
        bool IsIncludesLine,
        int SortOrder
    );

    public sealed record PlanListItem(
        int PlanId,
        string Code,
        string Name,
        string? Tagline,
        string IconEmoji,
        string BadgeClass,
        long BaseAmountPaise,
        decimal GstPercent,
        long GstPaise,
        long TotalAmountPaise,
        int DurationDays,
        bool IsActive,
        bool IsFeatured,
        int SortOrder,
        string? IncludesPlanCode,
        string DisplayPriceSuffix,
        string? CtaLabel,
        string? PopularLabel,
        DateTime UpdatedAt,
        List<PlanFeatureItem> Features
    );

    public sealed record PlanListResponse(
        bool Success,
        string? Message = null,
        List<PlanListItem>? Plans = null,
        int Total = 0
    );

    public sealed record PlanDetailResponse(
        bool Success,
        string? Message = null,
        PlanListItem? Plan = null
    );

    public sealed record UpsertPlanRequest(
        string Name,
        string? Tagline,
        string IconEmoji,
        string BadgeClass,
        long BaseAmountPaise,
        decimal GstPercent,
        int DurationDays,
        bool IsActive,
        bool IsFeatured,
        int SortOrder,
        string? IncludesPlanCode,
        string DisplayPriceSuffix,
        string? CtaLabel,
        string? PopularLabel,
        List<PlanFeatureItem>? Features
    );

    public sealed record SetActiveRequest(bool IsActive);

    public sealed record MutationResponse(bool Success, string? Message = null, int? PlanId = null);

    [HttpGet]
    public async Task<ActionResult<PlanListResponse>> ListPublic(CancellationToken ct)
    {
        var items = await LoadPlansAsync(activeOnly: true, ct);
        return Ok(new PlanListResponse(true, "OK", items, items.Count));
    }

    [Authorize(Policy = "SuperAdminOnly")]
    [HttpGet("admin")]
    public async Task<ActionResult<PlanListResponse>> AdminList(CancellationToken ct)
    {
        var items = await LoadPlansAsync(activeOnly: false, ct);
        return Ok(new PlanListResponse(true, "OK", items, items.Count));
    }

    [Authorize(Policy = "SuperAdminOnly")]
    [HttpGet("admin/{planId:int}")]
    public async Task<ActionResult<PlanDetailResponse>> AdminGet(int planId, CancellationToken ct)
    {
        var plan = await _db.Plans.AsNoTracking()
            .Include(p => p.Features)
            .FirstOrDefaultAsync(p => p.PlanId == planId && PlanCmsCodes.Managed.Contains(p.Code), ct);

        if (plan is null) return NotFound(new PlanDetailResponse(false, "Plan not found."));

        return Ok(new PlanDetailResponse(true, "OK", ToItem(plan)));
    }

    [HttpGet("{code}")]
    public async Task<ActionResult<PlanDetailResponse>> GetPublic(string code, CancellationToken ct)
    {
        if (!PlanCmsCodes.IsManaged(code))
            return NotFound(new PlanDetailResponse(false, "Plan not found."));

        var items = await LoadPlansAsync(activeOnly: true, ct: ct, codeFilter: code);
        var plan = items.FirstOrDefault();
        if (plan is null) return NotFound(new PlanDetailResponse(false, "Plan not found."));

        return Ok(new PlanDetailResponse(true, "OK", plan));
    }

    [Authorize(Policy = "SuperAdminOnly")]
    [HttpPut("admin/{planId:int}")]
    public async Task<ActionResult<MutationResponse>> Update(
        int planId,
        [FromBody] UpsertPlanRequest? req,
        CancellationToken ct)
    {
        if (req is null) return BadRequest(new MutationResponse(false, "Request is required."));
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new MutationResponse(false, "Plan name is required."));
        if (req.BaseAmountPaise < 0)
            return BadRequest(new MutationResponse(false, "Base amount cannot be negative."));
        if (req.GstPercent < 0 || req.GstPercent > 100)
            return BadRequest(new MutationResponse(false, "GST percent must be between 0 and 100."));

        var plan = await _db.Plans
            .Include(p => p.Features)
            .FirstOrDefaultAsync(p => p.PlanId == planId && PlanCmsCodes.Managed.Contains(p.Code), ct);

        if (plan is null) return NotFound(new MutationResponse(false, "Plan not found."));

        plan.Name = req.Name.Trim();
        plan.Tagline = string.IsNullOrWhiteSpace(req.Tagline) ? null : req.Tagline.Trim();
        plan.IconEmoji = string.IsNullOrWhiteSpace(req.IconEmoji)
            ? PlanEmojiDefaults.Get(plan.Code)
            : req.IconEmoji.Trim();
        plan.BadgeClass = string.IsNullOrWhiteSpace(req.BadgeClass) ? "free" : req.BadgeClass.Trim();
        plan.BaseAmountPaise = req.BaseAmountPaise;
        plan.GstPercent = req.GstPercent;
        RecalculateAmounts(plan);
        plan.DurationDays = req.DurationDays > 0 ? req.DurationDays : 365;
        plan.IsActive = req.IsActive;
        plan.IsFeatured = req.IsFeatured;
        plan.SortOrder = req.SortOrder;
        plan.IncludesPlanCode = string.IsNullOrWhiteSpace(req.IncludesPlanCode) ? null : req.IncludesPlanCode.Trim().ToLowerInvariant();
        plan.DisplayPriceSuffix = string.IsNullOrWhiteSpace(req.DisplayPriceSuffix) ? "/ year + 18% GST" : req.DisplayPriceSuffix.Trim();
        plan.CtaLabel = string.IsNullOrWhiteSpace(req.CtaLabel) ? null : req.CtaLabel.Trim();
        plan.PopularLabel = string.IsNullOrWhiteSpace(req.PopularLabel) ? null : req.PopularLabel.Trim();
        plan.UpdatedAt = DateTime.Now;

        _db.PlanFeatures.RemoveRange(plan.Features);
        plan.Features.Clear();
        SyncFeatures(plan, req.Features);
        await _db.SaveChangesAsync(ct);

        return Ok(new MutationResponse(true, "Plan updated.", planId));
    }

    [Authorize(Policy = "SuperAdminOnly")]
    [HttpPatch("admin/{planId:int}/active")]
    public async Task<ActionResult<MutationResponse>> SetActive(
        int planId,
        [FromBody] SetActiveRequest? req,
        CancellationToken ct)
    {
        if (req is null) return BadRequest(new MutationResponse(false, "Request is required."));

        var plan = await _db.Plans.FirstOrDefaultAsync(
            p => p.PlanId == planId && PlanCmsCodes.Managed.Contains(p.Code), ct);

        if (plan is null) return NotFound(new MutationResponse(false, "Plan not found."));

        plan.IsActive = req.IsActive;
        plan.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync(ct);

        return Ok(new MutationResponse(true, req.IsActive ? "Plan activated." : "Plan deactivated.", planId));
    }

    private async Task<List<PlanListItem>> LoadPlansAsync(
        bool activeOnly,
        CancellationToken ct,
        string? codeFilter = null)
    {
        var q = _db.Plans.AsNoTracking()
            .Include(p => p.Features)
            .Where(p => PlanCmsCodes.Managed.Contains(p.Code));

        if (activeOnly) q = q.Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(codeFilter))
            q = q.Where(p => p.Code == codeFilter.Trim());

        var plans = await q
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .ToListAsync(ct);

        return plans.Select(ToItem).ToList();
    }

    private static void SyncFeatures(Plan plan, List<PlanFeatureItem>? features)
    {
        if (features is null || features.Count == 0) return;

        var rows = features
            .Where(f => !string.IsNullOrWhiteSpace(f.Text))
            .Select((f, i) => new PlanFeature
            {
                Text = f.Text.Trim(),
                Description = string.IsNullOrWhiteSpace(f.Description) ? null : f.Description.Trim(),
                OfferingSlug = string.IsNullOrWhiteSpace(f.OfferingSlug) ? null : f.OfferingSlug.Trim(),
                IsIncludesLine = f.IsIncludesLine,
                SortOrder = f.SortOrder >= 0 ? f.SortOrder : i,
            })
            .OrderBy(f => f.SortOrder)
            .ToList();

        for (var i = 0; i < rows.Count; i++)
        {
            rows[i].SortOrder = i + 1;
            plan.Features.Add(rows[i]);
        }
    }

    private static void RecalculateAmounts(Plan plan)
    {
        plan.GstPaise = (long)Math.Round(plan.BaseAmountPaise * plan.GstPercent / 100m, MidpointRounding.AwayFromZero);
        plan.TotalAmountPaise = plan.BaseAmountPaise + plan.GstPaise;
    }

    private static PlanListItem ToItem(Plan p)
    {
        var features = p.Features
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.PlanFeatureId)
            .Select(f => new PlanFeatureItem(
                f.PlanFeatureId,
                f.Text,
                f.Description,
                f.OfferingSlug,
                f.IsIncludesLine,
                f.SortOrder))
            .ToList();

        return new PlanListItem(
            p.PlanId,
            p.Code,
            p.Name,
            p.Tagline,
            PlanEmojiDefaults.Normalize(p.Code, p.IconEmoji),
            p.BadgeClass,
            p.BaseAmountPaise,
            p.GstPercent,
            p.GstPaise,
            p.TotalAmountPaise,
            p.DurationDays,
            p.IsActive,
            p.IsFeatured,
            p.SortOrder,
            p.IncludesPlanCode,
            p.DisplayPriceSuffix,
            p.CtaLabel,
            p.PopularLabel,
            p.UpdatedAt,
            features
        );
    }
}
