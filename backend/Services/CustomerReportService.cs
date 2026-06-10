using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RB_Website_API.Auth;
using RB_Website_API.Data;
using RB_Website_API.DTO;
using RB_Website_API.Models;
using RB_Website_API.Services.IRepository;

namespace RB_Website_API.Services;

public sealed class CustomerReportService : ICustomerReportService
{
    private readonly AppDbContext _db;
    private readonly ICustomerReportRepository _reports;
    private readonly IReportAuditService _audit;
    private readonly IReportEmailService _email;
    private readonly CustomerReportSettings _settings;
    private readonly IWebHostEnvironment _env;
    private readonly IMemberIdGeneratorService _memberIds;
    private readonly ILogger<CustomerReportService> _logger;

    public CustomerReportService(
        AppDbContext db,
        ICustomerReportRepository reports,
        IReportAuditService audit,
        IReportEmailService email,
        IOptions<CustomerReportSettings> settings,
        IWebHostEnvironment env,
        IMemberIdGeneratorService memberIds,
        ILogger<CustomerReportService> logger)
    {
        _db = db;
        _reports = reports;
        _audit = audit;
        _email = email;
        _settings = settings.Value;
        _env = env;
        _memberIds = memberIds;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CustomerSearchResultDto>> SearchCustomersAsync(
        string? memberId,
        string? mobile,
        string? email,
        string? customerName,
        CancellationToken ct)
    {
        var q = _db.Users.AsNoTracking().Where(u =>
            u.Role != "admin" && u.Role != "superadmin");

        if (!string.IsNullOrWhiteSpace(memberId))
        {
            var term = memberId.Trim();
            if (MemberIdHelper.TryParseLegacyGuid(term, out var legacyUid))
            {
                q = q.Where(u => u.UserId == legacyUid);
            }
            else if (MemberIdHelper.IsNewFormat(term))
            {
                var normalized = MemberIdHelper.NormalizeNewFormat(term)!;
                q = q.Where(u => u.MemberId != null && u.MemberId.ToUpper() == normalized);
            }
            else
            {
                var upper = term.ToUpperInvariant();
                q = q.Where(u =>
                    (u.MemberId != null && u.MemberId.ToUpper().Contains(upper))
                    || u.FullName.ToLower().Contains(term.ToLowerInvariant()));
            }
        }

        if (!string.IsNullOrWhiteSpace(mobile))
        {
            var phone = IndianPhone.Digits(mobile);
            if (!string.IsNullOrEmpty(phone))
                q = q.Where(u => u.Phone == phone);
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var em = email.Trim().ToLowerInvariant();
            q = q.Where(u => u.Email.ToLower() == em);
        }

        if (!string.IsNullOrWhiteSpace(customerName))
        {
            var name = customerName.Trim().ToLowerInvariant();
            q = q.Where(u => u.FullName.ToLower().Contains(name));
        }

        var users = await q.OrderBy(u => u.FullName).Take(25).ToListAsync(ct);
        if (users.Count == 0) return Array.Empty<CustomerSearchResultDto>();

        var now = DateTime.Now;
        var userIds = users.Select(u => u.UserId).ToList();
        var activePlans = await (
            from up in _db.UserPlans.AsNoTracking()
            join p in _db.Plans.AsNoTracking() on up.PlanId equals p.PlanId
            where userIds.Contains(up.UserId)
                  && up.Status == "Active"
                  && (up.ActiveTo == null || up.ActiveTo > now)
            select new { up.UserId, up.UserPlanId, p.Name, up.ActiveTo }
        ).ToListAsync(ct);

        var planByUser = activePlans
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.ActiveTo).First());

        return users.Select(u =>
        {
            planByUser.TryGetValue(u.UserId, out var plan);
            return new CustomerSearchResultDto(
                u.UserId,
                _memberIds.GetDisplayMemberId(u),
                u.FullName,
                u.Email,
                u.Phone,
                plan is not null,
                plan?.Name,
                plan?.ActiveTo);
        }).ToList();
    }

    public async Task<(bool Ok, string? Error, Guid? SubscriptionId)> ValidateActiveSubscriptionAsync(
        Guid customerId,
        CancellationToken ct)
    {
        var now = DateTime.Now;
        var up = await _db.UserPlans.AsNoTracking()
            .Where(p => p.UserId == customerId
                        && p.Status == "Active"
                        && (p.ActiveTo == null || p.ActiveTo > now))
            .OrderByDescending(p => p.ActiveTo)
            .FirstOrDefaultAsync(ct);

        if (up is null)
            return (false, "Customer does not have an active subscription.", null);

        if (up.ActiveTo is not null && up.ActiveTo.Value.Date < now.Date)
            return (false, "Customer does not have an active subscription.", null);

        return (true, null, up.UserPlanId);
    }

    public async Task<(bool Success, string? Message, Guid? ReportId)> UploadReportAsync(
        Guid adminUserId,
        Guid customerId,
        IFormFile file,
        string? ipAddress,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return (false, "Please select a ZIP file to upload.", null);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".zip")
            return (false, "Only ZIP files are allowed.", null);

        var maxBytes = _settings.MaxUploadSizeMb * 1024L * 1024L;
        if (file.Length > maxBytes)
            return (false, $"File exceeds maximum size of {_settings.MaxUploadSizeMb} MB.", null);

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == customerId, ct);
        if (user is null)
            return (false, "Customer not found.", null);

        var role = (user.Role ?? "").Trim().ToLowerInvariant();
        if (role is "admin" or "superadmin")
            return (false, "Invalid customer.", null);

        var (subOk, subErr, subscriptionId) = await ValidateActiveSubscriptionAsync(customerId, ct);
        if (!subOk)
            return (false, subErr, null);

        var dateFolder = DateTime.Now.ToString("yyyyMMdd");
        var uniqueName = $"{Guid.NewGuid():N}.zip";
        var relativeDir = Path.Combine(_settings.UploadRoot, customerId.ToString(), dateFolder);
        var wwwroot = Path.Combine(_env.ContentRootPath, "wwwroot");
        var absoluteDir = Path.Combine(wwwroot, relativeDir);
        Directory.CreateDirectory(absoluteDir);

        var absolutePath = Path.Combine(absoluteDir, uniqueName);
        var relativePath = Path.Combine(relativeDir, uniqueName).Replace('\\', '/');

        await using (var stream = new FileStream(absolutePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(stream, ct);
        }

        var report = new CustomerReport
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            MemberId = _memberIds.GetDisplayMemberId(user),
            ReportFileName = uniqueName,
            OriginalFileName = Path.GetFileName(file.FileName),
            FilePath = relativePath,
            FileSize = file.Length,
            UploadDate = DateTime.Now,
            UploadedBy = adminUserId,
            SubscriptionId = subscriptionId!.Value,
            IsActive = true,
            DownloadCount = 0,
        };

        await _reports.AddAsync(report, ct);
        await _reports.SaveChangesAsync(ct);

        await _audit.LogAsync(
            ReportAuditService.ActionUpload,
            adminUserId,
            report.Id,
            customerId,
            ipAddress,
            ct);

        try
        {
            await _email.SendReportReadyEmailAsync(user, report, ct);
            await _audit.LogAsync(
                ReportAuditService.ActionEmailSent,
                adminUserId,
                report.Id,
                customerId,
                ipAddress,
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Report uploaded but email failed for report {ReportId}.", report.Id);
        }

        return (true, "Report uploaded successfully.", report.Id);
    }

    public async Task<IReadOnlyList<CustomerReportListItemDto>> ListCustomerReportsAsync(
        Guid customerId,
        CancellationToken ct)
    {
        var rows = await _reports.ListActiveByCustomerAsync(customerId, ct);
        if (rows.Count == 0) return Array.Empty<CustomerReportListItemDto>();

        var subIds = rows.Select(r => r.SubscriptionId).Distinct().ToList();
        var plans = await (
            from up in _db.UserPlans.AsNoTracking()
            join p in _db.Plans.AsNoTracking() on up.PlanId equals p.PlanId
            where subIds.Contains(up.UserPlanId)
            select new { up.UserPlanId, p.Name }
        ).ToDictionaryAsync(x => x.UserPlanId, x => x.Name, ct);

        return rows.Select(r => new CustomerReportListItemDto(
            r.Id,
            r.OriginalFileName,
            r.UploadDate,
            plans.GetValueOrDefault(r.SubscriptionId, "—"),
            r.FileSize,
            r.MemberId)).ToList();
    }

    public async Task<PagedResultDto<AdminReportHistoryItemDto>> ListAdminHistoryAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var (items, total) = await _reports.ListHistoryAsync(search, page, pageSize, ct);
        if (items.Count == 0)
            return new PagedResultDto<AdminReportHistoryItemDto>(true, "OK", Array.Empty<AdminReportHistoryItemDto>(), total, page, pageSize);

        var customerIds = items.Select(i => i.CustomerId).Distinct().ToList();
        var users = await _db.Users.AsNoTracking()
            .Where(u => customerIds.Contains(u.UserId))
            .ToDictionaryAsync(u => u.UserId, ct);

        var subIds = items.Select(i => i.SubscriptionId).Distinct().ToList();
        var planNames = await (
            from up in _db.UserPlans.AsNoTracking()
            join p in _db.Plans.AsNoTracking() on up.PlanId equals p.PlanId
            where subIds.Contains(up.UserPlanId)
            select new { up.UserPlanId, p.Name }
        ).ToDictionaryAsync(x => x.UserPlanId, x => x.Name, ct);

        var dtos = items.Select(r =>
        {
            users.TryGetValue(r.CustomerId, out var u);
            return new AdminReportHistoryItemDto(
                r.Id,
                u?.FullName ?? "—",
                r.MemberId,
                u?.Email ?? "—",
                r.UploadDate,
                r.DownloadCount,
                r.LastDownloadDate,
                r.OriginalFileName,
                r.FileSize,
                planNames.GetValueOrDefault(r.SubscriptionId));
        }).ToList();

        return new PagedResultDto<AdminReportHistoryItemDto>(true, "OK", dtos, total, page, pageSize);
    }

    public async Task<(bool Allowed, string? Error, string? PhysicalPath, string? DownloadName)> GetDownloadForCustomerAsync(
        Guid customerId,
        Guid reportId,
        string? ipAddress,
        CancellationToken ct)
    {
        var (subOk, subErr, _) = await ValidateActiveSubscriptionAsync(customerId, ct);
        if (!subOk)
            return (false, "Access Denied.", null, null);

        var report = await _reports.GetByIdForCustomerAsync(reportId, customerId, ct);
        if (report is null)
            return (false, "Access Denied.", null, null);

        var wwwroot = Path.Combine(_env.ContentRootPath, "wwwroot");
        var physical = Path.Combine(wwwroot, report.FilePath.Replace('/', Path.DirectorySeparatorChar));
        if (!System.IO.File.Exists(physical))
        {
            _logger.LogWarning("Report file missing on disk: {Path}", physical);
            return (false, "Report file not found.", null, null);
        }

        await _reports.IncrementDownloadAsync(report, ct);

        await _audit.LogAsync(
            ReportAuditService.ActionDownload,
            customerId,
            report.Id,
            customerId,
            ipAddress,
            ct);

        var downloadName = string.IsNullOrWhiteSpace(report.OriginalFileName)
            ? "Report.zip"
            : report.OriginalFileName;

        return (true, null, physical, downloadName);
    }
}
