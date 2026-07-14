using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RB_Website_API.Auth;
using RB_Website_API.Data;
using RB_Website_API.Models;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/blogs")]
public sealed class BlogController : ControllerBase
{
    private static readonly HashSet<string> AllowedImageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    private const long MaxImageBytes = 5 * 1024 * 1024; // 5 MB

    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public BlogController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // ── DTOs ────────────────────────────────────────────────────────────────────

    public sealed record BlogListItem(
        int BlogId,
        string Slug,
        string Title,
        string Crumb,
        string Meta,
        string Category,
        string DateLabel,
        string Summary,
        string BadgeText,
        string BadgeClass,
        string CardIcon,
        string CardClass,
        string? ImageUrl,
        string? SeoTitle,
        string? MetaDescription,
        bool IsPublished,
        DateTime CreatedAt
    );

    public sealed record BlogDetailItem(
        int BlogId,
        string Slug,
        string Title,
        string Crumb,
        string Meta,
        string Category,
        string DateLabel,
        string Summary,
        string BadgeText,
        string BadgeClass,
        string CardIcon,
        string CardClass,
        string? ImageUrl,
        string? SeoTitle,
        string? MetaDescription,
        bool IsPublished,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        string Content
    );

    public sealed record BlogListResponse(
        bool Success,
        string? Message = null,
        List<BlogListItem>? Blogs = null,
        int Total = 0,
        int Page = 1,
        int PageSize = 20
    );

    public sealed record BlogDetailResponse(bool Success, string? Message = null, BlogDetailItem? Blog = null);

    public sealed record ImageUploadResponse(bool Success, string? Message = null, string? ImageUrl = null);

    public sealed record CreateBlogRequest(
        string Slug,
        string Title,
        string Crumb,
        string Meta,
        string Content,
        string Category,
        string DateLabel,
        string Summary,
        string BadgeText,
        string BadgeClass,
        string CardIcon,
        string CardClass,
        string? ImageUrl,
        string? SeoTitle,
        string? MetaDescription,
        bool IsPublished
    );

    public sealed record UpdateBlogRequest(
        string Title,
        string Crumb,
        string Meta,
        string Content,
        string Category,
        string DateLabel,
        string Summary,
        string BadgeText,
        string BadgeClass,
        string CardIcon,
        string CardClass,
        string? ImageUrl,
        string? SeoTitle,
        string? MetaDescription,
        bool IsPublished
    );

    public sealed record SetPublishedRequest(bool IsPublished);
    public sealed record MutationResponse(bool Success, string? Message = null, int? BlogId = null);

    // ── Public endpoints ─────────────────────────────────────────────────────────

    /// <summary>GET /api/blogs — published blogs, newest first.</summary>
    [HttpGet]
    public async Task<ActionResult<BlogListResponse>> GetPublished(
        [FromQuery] string? category = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page     = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = _db.Blogs.AsNoTracking().Where(b => b.IsPublished);

        if (!string.IsNullOrWhiteSpace(category))
        {
            var cat = category.Trim().ToLowerInvariant();
            q = q.Where(b => b.Category.ToLower() == cat);
        }

        var total = await q.CountAsync(ct);
        var blogs = await q
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => ToListItem(b))
            .ToListAsync(ct);

        return Ok(new BlogListResponse(true, "OK", blogs, total, page, pageSize));
    }

    // ── Admin endpoints (literal "admin" routes before {slug}) ───────────────────

    /// <summary>GET /api/blogs/admin — all blogs (published + drafts), newest first.</summary>
    [Authorize(Policy = "AdminAccess")]
    [HttpGet("admin")]
    public async Task<ActionResult<BlogListResponse>> AdminList(
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page     = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var q = _db.Blogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            q = q.Where(b => b.Title.ToLower().Contains(s) || b.Slug.ToLower().Contains(s) || b.Summary.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var cat = category.Trim().ToLowerInvariant();
            q = q.Where(b => b.Category.ToLower() == cat);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var st = status.Trim().ToLowerInvariant();
            if (st == "published") q = q.Where(b => b.IsPublished);
            else if (st == "draft") q = q.Where(b => !b.IsPublished);
        }

        var total = await q.CountAsync(ct);
        var blogs = await q
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => ToListItem(b))
            .ToListAsync(ct);

        return Ok(new BlogListResponse(true, "OK", blogs, total, page, pageSize));
    }

    /// <summary>GET /api/blogs/admin/{id} — single blog by id (admin, any status).</summary>
    [Authorize(Policy = "AdminAccess")]
    [HttpGet("admin/{blogId:int}")]
    public async Task<ActionResult<BlogDetailResponse>> AdminGet(int blogId, CancellationToken ct)
    {
        var blog = await _db.Blogs.AsNoTracking().FirstOrDefaultAsync(b => b.BlogId == blogId, ct);
        if (blog is null) return NotFound(new BlogDetailResponse(false, "Blog not found."));
        return Ok(new BlogDetailResponse(true, "OK", ToDetailItem(blog)));
    }

    /// <summary>POST /api/blogs/admin/upload-image — upload a cover image (jpg/png/webp/gif, max 5 MB).</summary>
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

        var webRoot = _env.WebRootPath
                      ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var relativeDir = Path.Combine("uploads", "blogs");
        var absoluteDir = Path.Combine(webRoot, relativeDir);
        Directory.CreateDirectory(absoluteDir);

        var fileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var absolutePath = Path.Combine(absoluteDir, fileName);

        await using (var stream = System.IO.File.Create(absolutePath))
        {
            await file.CopyToAsync(stream, ct);
        }

        var publicUrl = $"/uploads/blogs/{fileName}";
        return Ok(new ImageUploadResponse(true, "Image uploaded.", publicUrl));
    }

    /// <summary>POST /api/blogs/admin — create a new blog post.</summary>
    [Authorize(Policy = "AdminAccess")]
    [HttpPost("admin")]
    public async Task<ActionResult<MutationResponse>> Create(
        [FromBody] CreateBlogRequest? req,
        CancellationToken ct)
    {
        if (req is null) return BadRequest(new MutationResponse(false, "Request is required."));

        var (ok, err, slug) = ValidateSlug(req.Slug);
        if (!ok) return BadRequest(new MutationResponse(false, err));

        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest(new MutationResponse(false, "Title is required."));

        var exists = await _db.Blogs.AnyAsync(b => b.Slug == slug, ct);
        if (exists) return Conflict(new MutationResponse(false, "A blog with this slug already exists."));

        var actorId = GetActorId();
        var now = DateTime.Now;

        var blog = new Blog
        {
            Slug            = slug!,
            Title           = req.Title.Trim(),
            Crumb           = (req.Crumb ?? "").Trim(),
            Meta            = (req.Meta ?? "").Trim(),
            Content         = req.Content ?? "",
            Category        = (req.Category ?? "blog").Trim().ToLowerInvariant(),
            DateLabel       = (req.DateLabel ?? "").Trim(),
            Summary         = (req.Summary ?? "").Trim(),
            BadgeText       = (req.BadgeText ?? "").Trim(),
            BadgeClass      = (req.BadgeClass ?? "").Trim(),
            CardIcon        = (req.CardIcon ?? "").Trim(),
            CardClass       = (req.CardClass ?? "").Trim(),
            ImageUrl        = string.IsNullOrWhiteSpace(req.ImageUrl) ? null : req.ImageUrl.Trim(),
            SeoTitle        = string.IsNullOrWhiteSpace(req.SeoTitle) ? null : req.SeoTitle.Trim(),
            MetaDescription = string.IsNullOrWhiteSpace(req.MetaDescription) ? null : req.MetaDescription.Trim(),
            IsPublished     = req.IsPublished,
            CreatedAt       = now,
            UpdatedAt       = now,
            CreatedByUserId = actorId,
        };

        _db.Blogs.Add(blog);
        await _db.SaveChangesAsync(ct);

        return Ok(new MutationResponse(true, "Blog created.", blog.BlogId));
    }

    /// <summary>PUT /api/blogs/admin/{id} — update an existing blog post.</summary>
    [Authorize(Policy = "AdminAccess")]
    [HttpPut("admin/{blogId:int}")]
    public async Task<ActionResult<MutationResponse>> Update(
        int blogId,
        [FromBody] UpdateBlogRequest? req,
        CancellationToken ct)
    {
        if (req is null) return BadRequest(new MutationResponse(false, "Request is required."));
        if (string.IsNullOrWhiteSpace(req.Title)) return BadRequest(new MutationResponse(false, "Title is required."));

        var blog = await _db.Blogs.FirstOrDefaultAsync(b => b.BlogId == blogId, ct);
        if (blog is null) return NotFound(new MutationResponse(false, "Blog not found."));

        blog.Title           = req.Title.Trim();
        blog.Crumb           = (req.Crumb ?? "").Trim();
        blog.Meta            = (req.Meta ?? "").Trim();
        blog.Content         = req.Content ?? "";
        blog.Category        = (req.Category ?? "blog").Trim().ToLowerInvariant();
        blog.DateLabel       = (req.DateLabel ?? "").Trim();
        blog.Summary         = (req.Summary ?? "").Trim();
        blog.BadgeText       = (req.BadgeText ?? "").Trim();
        blog.BadgeClass      = (req.BadgeClass ?? "").Trim();
        blog.CardIcon        = (req.CardIcon ?? "").Trim();
        blog.CardClass       = (req.CardClass ?? "").Trim();
        blog.ImageUrl        = string.IsNullOrWhiteSpace(req.ImageUrl) ? null : req.ImageUrl.Trim();
        blog.SeoTitle        = string.IsNullOrWhiteSpace(req.SeoTitle) ? null : req.SeoTitle.Trim();
        blog.MetaDescription = string.IsNullOrWhiteSpace(req.MetaDescription) ? null : req.MetaDescription.Trim();
        blog.IsPublished     = req.IsPublished;
        blog.UpdatedAt       = DateTime.Now;

        await _db.SaveChangesAsync(ct);
        return Ok(new MutationResponse(true, "Blog updated.", blogId));
    }

    /// <summary>PATCH /api/blogs/admin/{id}/publish — toggle publish status.</summary>
    [Authorize(Policy = "AdminAccess")]
    [HttpPatch("admin/{blogId:int}/publish")]
    public async Task<ActionResult<MutationResponse>> SetPublished(
        int blogId,
        [FromBody] SetPublishedRequest? req,
        CancellationToken ct)
    {
        if (req is null) return BadRequest(new MutationResponse(false, "Request is required."));

        var blog = await _db.Blogs.FirstOrDefaultAsync(b => b.BlogId == blogId, ct);
        if (blog is null) return NotFound(new MutationResponse(false, "Blog not found."));

        blog.IsPublished = req.IsPublished;
        blog.UpdatedAt   = DateTime.Now;
        await _db.SaveChangesAsync(ct);

        return Ok(new MutationResponse(true, req.IsPublished ? "Blog published." : "Blog unpublished.", blogId));
    }

    /// <summary>DELETE /api/blogs/admin/{id} — permanently delete a blog post.</summary>
    [Authorize(Policy = "AdminAccess")]
    [HttpDelete("admin/{blogId:int}")]
    public async Task<ActionResult<MutationResponse>> Delete(int blogId, CancellationToken ct)
    {
        var blog = await _db.Blogs.FirstOrDefaultAsync(b => b.BlogId == blogId, ct);
        if (blog is null) return NotFound(new MutationResponse(false, "Blog not found."));

        _db.Blogs.Remove(blog);
        await _db.SaveChangesAsync(ct);
        return Ok(new MutationResponse(true, "Blog deleted.", blogId));
    }

    /// <summary>GET /api/blogs/{slug} — single published blog by slug.</summary>
    [HttpGet("{slug}")]
    public async Task<ActionResult<BlogDetailResponse>> GetBySlug(string slug, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return BadRequest(new BlogDetailResponse(false, "Slug is required."));

        var blog = await _db.Blogs.AsNoTracking()
            .Where(b => b.Slug == slug.Trim().ToLowerInvariant() && b.IsPublished)
            .FirstOrDefaultAsync(ct);

        if (blog is null)
            return NotFound(new BlogDetailResponse(false, "Article not found."));

        return Ok(new BlogDetailResponse(true, "OK", ToDetailItem(blog)));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private static BlogListItem ToListItem(Blog b) => new(
        b.BlogId, b.Slug, b.Title, b.Crumb, b.Meta, b.Category, b.DateLabel,
        b.Summary, b.BadgeText, b.BadgeClass, b.CardIcon, b.CardClass,
        b.ImageUrl, b.SeoTitle, b.MetaDescription, b.IsPublished, b.CreatedAt);

    private static BlogDetailItem ToDetailItem(Blog b) => new(
        b.BlogId, b.Slug, b.Title, b.Crumb, b.Meta, b.Category, b.DateLabel,
        b.Summary, b.BadgeText, b.BadgeClass, b.CardIcon, b.CardClass,
        b.ImageUrl, b.SeoTitle, b.MetaDescription, b.IsPublished, b.CreatedAt, b.UpdatedAt,
        b.Content);

    private static (bool Ok, string? Error, string? Slug) ValidateSlug(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return (false, "Slug is required.", null);

        var slug = raw.Trim().ToLowerInvariant();
        if (!Regex.IsMatch(slug, @"^[a-z0-9]+(?:-[a-z0-9]+)*$"))
            return (false, "Slug must contain only lowercase letters, numbers, and hyphens (e.g. my-article-title).", null);

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
