using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RB_Website_API.Data;
using RB_Website_API.Models;
using System.Security.Claims;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/team-members")]
public sealed class TeamMemberController : ControllerBase
{
    private const int MaxPhotoBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedPhotoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif",
    };

    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public TeamMemberController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public sealed record TeamMemberItem(
        int TeamMemberId,
        string Name,
        string DesignationHtml,
        string PhotoUrl,
        int SortOrder,
        bool IsActive,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );

    public sealed record TeamMemberListResponse(
        bool Success,
        string? Message = null,
        List<TeamMemberItem>? Members = null,
        int Total = 0
    );

    public sealed record TeamMemberDetailResponse(
        bool Success,
        string? Message = null,
        TeamMemberItem? Member = null
    );

    public sealed record UpsertTeamMemberRequest(
        string Name,
        string DesignationHtml,
        string PhotoUrl,
        int SortOrder,
        bool IsActive
    );

    public sealed record SetActiveRequest(bool IsActive);

    public sealed record ReorderTeamMembersRequest(List<int> OrderedIds);

    public sealed record TeamMemberMutationResponse(bool Success, string? Message = null, int? TeamMemberId = null);

    public sealed record PhotoUploadResponse(bool Success, string? Message = null, string? PhotoUrl = null);

    [HttpGet]
    public async Task<ActionResult<TeamMemberListResponse>> ListPublic(CancellationToken ct)
    {
        var items = await _db.TeamMembers.AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.Name)
            .Select(m => ToItem(m))
            .ToListAsync(ct);

        return Ok(new TeamMemberListResponse(true, "OK", items, items.Count));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpGet("admin")]
    public async Task<ActionResult<TeamMemberListResponse>> AdminList(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var q = _db.TeamMembers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            q = q.Where(m =>
                m.Name.ToLower().Contains(s) ||
                m.DesignationHtml.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var st = status.Trim().ToLowerInvariant();
            if (st == "active") q = q.Where(m => m.IsActive);
            else if (st == "inactive") q = q.Where(m => !m.IsActive);
        }

        var items = await q
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.Name)
            .Select(m => ToItem(m))
            .ToListAsync(ct);

        return Ok(new TeamMemberListResponse(true, "OK", items, items.Count));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpGet("admin/{teamMemberId:int}")]
    public async Task<ActionResult<TeamMemberDetailResponse>> AdminGet(int teamMemberId, CancellationToken ct)
    {
        var member = await _db.TeamMembers.AsNoTracking()
            .FirstOrDefaultAsync(m => m.TeamMemberId == teamMemberId, ct);

        if (member is null) return NotFound(new TeamMemberDetailResponse(false, "Team member not found."));

        return Ok(new TeamMemberDetailResponse(true, "OK", ToItem(member)));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpPost("admin")]
    public async Task<ActionResult<TeamMemberMutationResponse>> Create(
        [FromBody] UpsertTeamMemberRequest? req,
        CancellationToken ct)
    {
        if (req is null) return BadRequest(new TeamMemberMutationResponse(false, "Request is required."));
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new TeamMemberMutationResponse(false, "Name is required."));
        if (string.IsNullOrWhiteSpace(req.PhotoUrl))
            return BadRequest(new TeamMemberMutationResponse(false, "Photo URL is required."));

        var now = DateTime.Now;
        var member = new TeamMember
        {
            Name = req.Name.Trim(),
            DesignationHtml = req.DesignationHtml?.Trim() ?? "",
            PhotoUrl = req.PhotoUrl.Trim(),
            SortOrder = req.SortOrder,
            IsActive = req.IsActive,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = GetActorId(),
        };

        _db.TeamMembers.Add(member);
        await _db.SaveChangesAsync(ct);
        return Ok(new TeamMemberMutationResponse(true, "Team member created.", member.TeamMemberId));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpPut("admin/{teamMemberId:int}")]
    public async Task<ActionResult<TeamMemberMutationResponse>> Update(
        int teamMemberId,
        [FromBody] UpsertTeamMemberRequest? req,
        CancellationToken ct)
    {
        if (req is null) return BadRequest(new TeamMemberMutationResponse(false, "Request is required."));
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new TeamMemberMutationResponse(false, "Name is required."));
        if (string.IsNullOrWhiteSpace(req.PhotoUrl))
            return BadRequest(new TeamMemberMutationResponse(false, "Photo URL is required."));

        var member = await _db.TeamMembers.FirstOrDefaultAsync(m => m.TeamMemberId == teamMemberId, ct);
        if (member is null) return NotFound(new TeamMemberMutationResponse(false, "Team member not found."));

        member.Name = req.Name.Trim();
        member.DesignationHtml = req.DesignationHtml?.Trim() ?? "";
        member.PhotoUrl = req.PhotoUrl.Trim();
        member.SortOrder = req.SortOrder;
        member.IsActive = req.IsActive;
        member.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync(ct);
        return Ok(new TeamMemberMutationResponse(true, "Team member updated.", teamMemberId));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpPut("admin/reorder")]
    public async Task<ActionResult<TeamMemberMutationResponse>> Reorder(
        [FromBody] ReorderTeamMembersRequest? req,
        CancellationToken ct)
    {
        if (req?.OrderedIds is null || req.OrderedIds.Count == 0)
            return BadRequest(new TeamMemberMutationResponse(false, "Ordered member list is required."));

        var ids = req.OrderedIds.Where(id => id > 0).Distinct().ToList();
        if (ids.Count == 0)
            return BadRequest(new TeamMemberMutationResponse(false, "Ordered member list is required."));

        var members = await _db.TeamMembers.Where(m => ids.Contains(m.TeamMemberId)).ToListAsync(ct);
        if (members.Count != ids.Count)
            return BadRequest(new TeamMemberMutationResponse(false, "One or more team members were not found."));

        var now = DateTime.Now;
        for (var i = 0; i < ids.Count; i++)
        {
            var member = members.First(m => m.TeamMemberId == ids[i]);
            member.SortOrder = i + 1;
            member.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new TeamMemberMutationResponse(true, "Display order saved."));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpPatch("admin/{teamMemberId:int}/active")]
    public async Task<ActionResult<TeamMemberMutationResponse>> SetActive(
        int teamMemberId,
        [FromBody] SetActiveRequest? req,
        CancellationToken ct)
    {
        if (req is null) return BadRequest(new TeamMemberMutationResponse(false, "Request is required."));

        var member = await _db.TeamMembers.FirstOrDefaultAsync(m => m.TeamMemberId == teamMemberId, ct);
        if (member is null) return NotFound(new TeamMemberMutationResponse(false, "Team member not found."));

        member.IsActive = req.IsActive;
        member.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync(ct);

        return Ok(new TeamMemberMutationResponse(true, req.IsActive ? "Team member activated." : "Team member deactivated.", teamMemberId));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpDelete("admin/{teamMemberId:int}")]
    public async Task<ActionResult<TeamMemberMutationResponse>> Delete(int teamMemberId, CancellationToken ct)
    {
        var member = await _db.TeamMembers.FirstOrDefaultAsync(m => m.TeamMemberId == teamMemberId, ct);
        if (member is null) return NotFound(new TeamMemberMutationResponse(false, "Team member not found."));

        _db.TeamMembers.Remove(member);
        await _db.SaveChangesAsync(ct);
        return Ok(new TeamMemberMutationResponse(true, "Team member deleted."));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpPost("admin/upload-photo")]
    [RequestSizeLimit(MaxPhotoBytes + 1024 * 64)]
    public async Task<ActionResult<PhotoUploadResponse>> UploadPhoto(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new PhotoUploadResponse(false, "Please choose an image file."));

        if (file.Length > MaxPhotoBytes)
            return BadRequest(new PhotoUploadResponse(false, "Image must be 5 MB or smaller."));

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedPhotoExtensions.Contains(ext))
            return BadRequest(new PhotoUploadResponse(false, "Allowed formats: JPG, PNG, WEBP, GIF."));

        var contentType = (file.ContentType ?? "").ToLowerInvariant();
        if (!contentType.StartsWith("image/"))
            return BadRequest(new PhotoUploadResponse(false, "File must be an image."));

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var relativeDir = Path.Combine("uploads", "team");
        var absoluteDir = Path.Combine(webRoot, relativeDir);
        Directory.CreateDirectory(absoluteDir);

        var fileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var absolutePath = Path.Combine(absoluteDir, fileName);

        await using (var stream = System.IO.File.Create(absolutePath))
        {
            await file.CopyToAsync(stream, ct);
        }

        return Ok(new PhotoUploadResponse(true, "Photo uploaded.", $"/uploads/team/{fileName}"));
    }

    private static TeamMemberItem ToItem(TeamMember m) => new(
        m.TeamMemberId,
        m.Name,
        m.DesignationHtml,
        m.PhotoUrl,
        m.SortOrder,
        m.IsActive,
        m.CreatedAt,
        m.UpdatedAt
    );

    private Guid? GetActorId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
