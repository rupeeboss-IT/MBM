using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RB_Website_API.Auth;
using RB_Website_API.Data;
using RB_Website_API.DTO;
using RB_Website_API.Models;
using RB_Website_API.Services.IRepository;

namespace RB_Website_API.Services;

public sealed class ReportChangeRequestService : IReportChangeRequestService
{
    private readonly AppDbContext _db;
    private readonly ICustomerReportRepository _reports;
    private readonly IReportChangeRequestRepository _requests;
    private readonly IReportAuditRepository _auditRepo;
    private readonly IReportAuditService _audit;
    private readonly IReportEmailService _email;
    private readonly CustomerReportSettings _settings;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ReportChangeRequestService> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    public ReportChangeRequestService(
        AppDbContext db,
        ICustomerReportRepository reports,
        IReportChangeRequestRepository requests,
        IReportAuditRepository auditRepo,
        IReportAuditService audit,
        IReportEmailService email,
        IOptions<CustomerReportSettings> settings,
        IWebHostEnvironment env,
        ILogger<ReportChangeRequestService> logger)
    {
        _db = db;
        _reports = reports;
        _requests = requests;
        _auditRepo = auditRepo;
        _audit = audit;
        _email = email;
        _settings = settings.Value;
        _env = env;
        _logger = logger;
    }

    public async Task<(bool Success, string? Message, Guid? RequestId)> CreateRequestAsync(
        Guid actorUserId,
        bool isSuperAdmin,
        Guid reportId,
        string requestType,
        string reason,
        string? newOriginalFileName,
        IFormFile? replacementFile,
        string? ipAddress,
        CancellationToken ct)
    {
        var type = ReportChangeRequestCatalog.NormalizeType(requestType);
        if (!ReportChangeRequestCatalog.ValidTypes.Contains(type))
            return (false, "Invalid request type.", null);

        if (string.IsNullOrWhiteSpace(reason))
            return (false, "Reason is required.", null);

        var report = await _reports.GetByIdAsync(reportId, ct);
        if (report is null || !report.IsActive)
            return (false, "Report not found.", null);

        if (await _requests.HasPendingForReportAsync(reportId, ct))
            return (false, "A pending request already exists for this report.", null);

        string? pendingPath = null;
        string? pendingFileName = null;
        string? pendingOriginal = null;
        long? pendingSize = null;
        string? previousValues = null;
        string? newValues = null;

        if (type == ReportChangeRequestCatalog.TypeEdit)
        {
            if (string.IsNullOrWhiteSpace(newOriginalFileName))
                return (false, "New file name is required for edit requests.", null);

            previousValues = SerializeValues(new { originalFileName = report.OriginalFileName });
            newValues = SerializeValues(new { originalFileName = newOriginalFileName.Trim() });
        }
        else if (type == ReportChangeRequestCatalog.TypeReplace)
        {
            if (replacementFile is null || replacementFile.Length == 0)
                return (false, "Replacement ZIP file is required.", null);

            var (fileOk, fileErr, stored) = await StorePendingFileAsync(reportId, replacementFile, ct);
            if (!fileOk)
                return (false, fileErr, null);

            pendingPath = stored!.RelativePath;
            pendingFileName = stored.FileName;
            pendingOriginal = stored.OriginalFileName;
            pendingSize = stored.FileSize;
            previousValues = SerializeValues(new
            {
                filePath = report.FilePath,
                originalFileName = report.OriginalFileName,
                fileSize = report.FileSize,
            });
        }

        var request = new ReportChangeRequest
        {
            Id = Guid.NewGuid(),
            ReportId = reportId,
            RequestType = type,
            RequestedBy = actorUserId,
            RequestedOn = DateTime.Now,
            Reason = reason.Trim(),
            Status = ReportChangeRequestCatalog.StatusPending,
            PreviousReportPath = report.FilePath,
            NewReportPath = pendingPath,
            PreviousValues = previousValues,
            NewValues = newValues,
            PendingFileName = pendingFileName,
            PendingOriginalFileName = pendingOriginal ?? (type == ReportChangeRequestCatalog.TypeEdit ? newOriginalFileName?.Trim() : null),
            PendingFileSize = pendingSize,
        };

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await _requests.AddAsync(request, ct);
            await _audit.StageChangeLogAsync(
                ReportAuditService.ActionRequestCreated,
                actorUserId,
                reportId,
                report.CustomerId,
                request.Id,
                reason.Trim(),
                report.FilePath,
                pendingPath,
                previousValues,
                newValues,
                ipAddress,
                ct);
            await _requests.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            _logger.LogError(ex, "Failed to create report change request for report {ReportId}.", reportId);
            if (pendingPath is not null)
                TryDeletePhysicalFile(pendingPath);
            return (false, "Unable to create request. Please try again.", null);
        }

        return (true, isSuperAdmin ? "Request recorded." : "Request submitted for approval.", request.Id);
    }

    public async Task<PagedResultDto<ReportChangeRequestListItemDto>> ListRequestsAsync(
        Guid actorUserId,
        bool isSuperAdmin,
        string? status,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var (pageNum, size) = AdminListQuery.Normalize(page, pageSize, export: false);
        var filterBy = isSuperAdmin ? (Guid?)null : actorUserId;
        var normalizedStatus = string.IsNullOrWhiteSpace(status)
            ? null
            : ReportChangeRequestCatalog.NormalizeStatus(status);

        var (items, total) = await _requests.ListAsync(filterBy, normalizedStatus, pageNum, size, ct);
        if (items.Count == 0)
            return new PagedResultDto<ReportChangeRequestListItemDto>(true, "OK", Array.Empty<ReportChangeRequestListItemDto>(), total, pageNum, size);

        var dtos = await MapListItemsAsync(items, ct);
        return new PagedResultDto<ReportChangeRequestListItemDto>(true, "OK", dtos, total, pageNum, size);
    }

    public Task<int> GetPendingCountAsync(bool isSuperAdmin, CancellationToken ct) =>
        isSuperAdmin ? _requests.CountPendingAsync(ct) : Task.FromResult(0);

    public async Task<(bool Success, string? Message, ReportChangeRequestDetailDto? Detail)> GetRequestDetailAsync(
        Guid actorUserId,
        bool isSuperAdmin,
        Guid requestId,
        CancellationToken ct)
    {
        var request = await _requests.GetByIdAsync(requestId, ct);
        if (request is null)
            return (false, "Request not found.", null);

        if (!isSuperAdmin && request.RequestedBy != actorUserId)
            return (false, "You do not have permission to view this request.", null);

        var detail = await MapDetailAsync(request, ct);
        return (true, "OK", detail);
    }

    public async Task<(bool Success, string? Message)> ApproveRequestAsync(
        Guid superAdminUserId,
        Guid requestId,
        string? remarks,
        string? ipAddress,
        CancellationToken ct)
    {
        var request = await _requests.GetByIdAsync(requestId, ct);
        if (request is null)
            return (false, "Request not found.");

        if (request.Status != ReportChangeRequestCatalog.StatusPending)
            return (false, "Only pending requests can be approved.");

        var report = await _reports.GetByIdAsync(request.ReportId, ct);
        if (report is null)
            return (false, "Report not found.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var now = DateTime.Now;
            request.Status = ReportChangeRequestCatalog.StatusApproved;
            request.ApprovedBy = superAdminUserId;
            request.ApprovedOn = now;
            request.Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim();

            switch (request.RequestType)
            {
                case var t when t == ReportChangeRequestCatalog.TypeDelete:
                    await ApplySoftDeleteAsync(report, request, superAdminUserId, ipAddress, ct);
                    break;
                case var t when t == ReportChangeRequestCatalog.TypeReplace:
                    await ApplyReplaceAsync(report, request, superAdminUserId, ipAddress, ct);
                    break;
                case var t when t == ReportChangeRequestCatalog.TypeEdit:
                    await ApplyEditAsync(report, request, superAdminUserId, ipAddress, ct);
                    break;
                default:
                    await tx.RollbackAsync(ct);
                    return (false, "Unsupported request type.");
            }

            await _audit.StageChangeLogAsync(
                ReportAuditService.ActionRequestApproved,
                superAdminUserId,
                report.Id,
                report.CustomerId,
                request.Id,
                request.Remarks,
                request.PreviousReportPath,
                request.NewReportPath ?? report.FilePath,
                request.PreviousValues,
                request.NewValues,
                ipAddress,
                ct);

            await _requests.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            _logger.LogError(ex, "Failed to approve report change request {RequestId}.", requestId);
            return (false, "Unable to approve request. Please try again.");
        }

        if (request.RequestType == ReportChangeRequestCatalog.TypeReplace)
            await TryNotifyReportReadyAsync(report, superAdminUserId, ipAddress, ct);

        return (true, "Request approved.");
    }

    public async Task<(bool Success, string? Message)> RejectRequestAsync(
        Guid superAdminUserId,
        Guid requestId,
        string? remarks,
        string? ipAddress,
        CancellationToken ct)
    {
        var request = await _requests.GetByIdAsync(requestId, ct);
        if (request is null)
            return (false, "Request not found.");

        if (request.Status != ReportChangeRequestCatalog.StatusPending)
            return (false, "Only pending requests can be rejected.");

        var report = await _reports.GetByIdAsync(request.ReportId, ct);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            request.Status = ReportChangeRequestCatalog.StatusRejected;
            request.RejectedBy = superAdminUserId;
            request.RejectedOn = DateTime.Now;
            request.Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim();

            if (!string.IsNullOrWhiteSpace(request.NewReportPath))
                TryDeletePhysicalFile(request.NewReportPath);

            await _audit.StageChangeLogAsync(
                ReportAuditService.ActionRequestRejected,
                superAdminUserId,
                request.ReportId,
                report?.CustomerId,
                request.Id,
                request.Remarks,
                request.PreviousReportPath,
                request.NewReportPath,
                request.PreviousValues,
                request.NewValues,
                ipAddress,
                ct);

            await _requests.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            _logger.LogError(ex, "Failed to reject report change request {RequestId}.", requestId);
            return (false, "Unable to reject request. Please try again.");
        }

        return (true, "Request rejected.");
    }

    public async Task<(bool Success, string? Message)> DirectSoftDeleteAsync(
        Guid superAdminUserId,
        Guid reportId,
        string reason,
        string? ipAddress,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return (false, "Reason is required.");

        var report = await _reports.GetByIdAsync(reportId, ct);
        if (report is null || !report.IsActive)
            return (false, "Report not found.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var previousPath = report.FilePath;
            report.IsActive = false;

            await _audit.StageChangeLogAsync(
                ReportAuditService.ActionDirectDelete,
                superAdminUserId,
                report.Id,
                report.CustomerId,
                null,
                reason.Trim(),
                previousPath,
                null,
                SerializeValues(new { isActive = true }),
                SerializeValues(new { isActive = false }),
                ipAddress,
                ct);

            await _requests.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            _logger.LogError(ex, "Failed to soft-delete report {ReportId}.", reportId);
            return (false, "Unable to delete report. Please try again.");
        }

        return (true, "Report deleted.");
    }

    public async Task<(bool Success, string? Message)> DirectEditAsync(
        Guid superAdminUserId,
        Guid reportId,
        string? originalFileName,
        string reason,
        string? ipAddress,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return (false, "Reason is required.");
        if (string.IsNullOrWhiteSpace(originalFileName))
            return (false, "File name is required.");

        var report = await _reports.GetByIdAsync(reportId, ct);
        if (report is null || !report.IsActive)
            return (false, "Report not found.");

        var previousValues = SerializeValues(new { originalFileName = report.OriginalFileName });
        var newValues = SerializeValues(new { originalFileName = originalFileName.Trim() });

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            report.OriginalFileName = originalFileName.Trim();

            await _audit.StageChangeLogAsync(
                ReportAuditService.ActionDirectEdit,
                superAdminUserId,
                report.Id,
                report.CustomerId,
                null,
                reason.Trim(),
                report.FilePath,
                report.FilePath,
                previousValues,
                newValues,
                ipAddress,
                ct);

            await _requests.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            _logger.LogError(ex, "Failed to edit report {ReportId}.", reportId);
            return (false, "Unable to update report. Please try again.");
        }

        return (true, "Report updated.");
    }

    public async Task<(bool Success, string? Message)> DirectReplaceAsync(
        Guid superAdminUserId,
        Guid reportId,
        IFormFile file,
        string reason,
        string? ipAddress,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return (false, "Reason is required.");
        if (file is null || file.Length == 0)
            return (false, "Replacement ZIP file is required.");

        var report = await _reports.GetByIdAsync(reportId, ct);
        if (report is null || !report.IsActive)
            return (false, "Report not found.");

        var (fileOk, fileErr, stored) = await StoreActiveFileAsync(report.CustomerId, file, ct);
        if (!fileOk)
            return (false, fileErr);

        var previousPath = report.FilePath;
        var previousValues = SerializeValues(new
        {
            filePath = report.FilePath,
            originalFileName = report.OriginalFileName,
            fileSize = report.FileSize,
        });

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            report.FilePath = stored!.RelativePath;
            report.ReportFileName = stored.FileName;
            report.OriginalFileName = stored.OriginalFileName;
            report.FileSize = stored.FileSize;
            report.UploadDate = DateTime.Now;
            report.UploadedBy = superAdminUserId;

            var newValues = SerializeValues(new
            {
                filePath = report.FilePath,
                originalFileName = report.OriginalFileName,
                fileSize = report.FileSize,
            });

            await _audit.StageChangeLogAsync(
                ReportAuditService.ActionDirectReplace,
                superAdminUserId,
                report.Id,
                report.CustomerId,
                null,
                reason.Trim(),
                previousPath,
                report.FilePath,
                previousValues,
                newValues,
                ipAddress,
                ct);

            await _requests.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            TryDeletePhysicalFile(stored!.RelativePath);
            _logger.LogError(ex, "Failed to replace report {ReportId}.", reportId);
            return (false, "Unable to replace report. Please try again.");
        }

        await TryNotifyReportReadyAsync(report, superAdminUserId, ipAddress, ct);

        return (true, "Report replaced. The customer has been notified by email.");
    }

    public async Task<IReadOnlyList<ReportAuditLogItemDto>> ListAuditHistoryAsync(
        Guid actorUserId,
        bool isSuperAdmin,
        Guid reportId,
        CancellationToken ct)
    {
        _ = actorUserId;
        if (!isSuperAdmin)
            return Array.Empty<ReportAuditLogItemDto>();

        var report = await _reports.GetByIdAsync(reportId, ct);
        if (report is null)
            return Array.Empty<ReportAuditLogItemDto>();

        var logs = await _auditRepo.ListByReportIdAsync(reportId, ct);
        if (logs.Count == 0)
            return Array.Empty<ReportAuditLogItemDto>();

        var userIds = logs.Where(l => l.UserId.HasValue).Select(l => l.UserId!.Value).Distinct().ToList();
        var users = await _db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.UserId))
            .ToDictionaryAsync(u => u.UserId, u => u.FullName, ct);

        return logs.Select(l => new ReportAuditLogItemDto(
            l.AuditId,
            l.Action,
            l.CreatedAt,
            l.UserId.HasValue && users.TryGetValue(l.UserId.Value, out var name) ? name : null,
            l.RequestId,
            l.Remarks,
            l.PreviousReportPath,
            l.NewReportPath,
            l.PreviousValues,
            l.NewValues)).ToList();
    }

    private async Task ApplySoftDeleteAsync(
        CustomerReport report,
        ReportChangeRequest request,
        Guid superAdminUserId,
        string? ipAddress,
        CancellationToken ct)
    {
        report.IsActive = false;
        await _audit.StageChangeLogAsync(
            ReportAuditService.ActionSoftDelete,
            superAdminUserId,
            report.Id,
            report.CustomerId,
            request.Id,
            request.Reason,
            request.PreviousReportPath,
            null,
            SerializeValues(new { isActive = true }),
            SerializeValues(new { isActive = false }),
            ipAddress,
            ct);
    }

    private async Task ApplyReplaceAsync(
        CustomerReport report,
        ReportChangeRequest request,
        Guid superAdminUserId,
        string? ipAddress,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.NewReportPath))
            throw new InvalidOperationException("Pending replacement file is missing.");

        var wwwroot = Path.Combine(_env.ContentRootPath, "wwwroot");
        var pendingPhysical = Path.Combine(wwwroot, request.NewReportPath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(pendingPhysical))
            throw new InvalidOperationException("Pending replacement file not found on disk.");

        var dateFolder = DateTime.Now.ToString("yyyyMMdd");
        var uniqueName = $"{Guid.NewGuid():N}.zip";
        var relativeDir = Path.Combine(_settings.UploadRoot, report.CustomerId.ToString(), dateFolder);
        var absoluteDir = Path.Combine(wwwroot, relativeDir);
        Directory.CreateDirectory(absoluteDir);

        var absolutePath = Path.Combine(absoluteDir, uniqueName);
        var relativePath = Path.Combine(relativeDir, uniqueName).Replace('\\', '/');
        File.Copy(pendingPhysical, absolutePath, overwrite: false);

        var previousPath = report.FilePath;
        var pendingStagingPath = request.NewReportPath;
        report.FilePath = relativePath;
        report.ReportFileName = uniqueName;
        report.OriginalFileName = request.PendingOriginalFileName ?? report.OriginalFileName;
        report.FileSize = request.PendingFileSize ?? report.FileSize;
        report.UploadDate = DateTime.Now;
        report.UploadedBy = superAdminUserId;
        request.NewReportPath = relativePath;

        await _audit.StageChangeLogAsync(
            ReportAuditService.ActionReplace,
            superAdminUserId,
            report.Id,
            report.CustomerId,
            request.Id,
            request.Reason,
            previousPath,
            relativePath,
            request.PreviousValues,
            SerializeValues(new
            {
                filePath = relativePath,
                originalFileName = report.OriginalFileName,
                fileSize = report.FileSize,
            }),
            ipAddress,
            ct);

        TryDeletePhysicalFile(pendingStagingPath);
    }

    private async Task ApplyEditAsync(
        CustomerReport report,
        ReportChangeRequest request,
        Guid superAdminUserId,
        string? ipAddress,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.PendingOriginalFileName))
            throw new InvalidOperationException("Edit request is missing new file name.");

        report.OriginalFileName = request.PendingOriginalFileName.Trim();

        await _audit.StageChangeLogAsync(
            ReportAuditService.ActionEdit,
            superAdminUserId,
            report.Id,
            report.CustomerId,
            request.Id,
            request.Reason,
            report.FilePath,
            report.FilePath,
            request.PreviousValues,
            request.NewValues,
            ipAddress,
            ct);
    }

    private async Task<IReadOnlyList<ReportChangeRequestListItemDto>> MapListItemsAsync(
        IReadOnlyList<ReportChangeRequest> items,
        CancellationToken ct)
    {
        var reportIds = items.Select(i => i.ReportId).Distinct().ToList();
        var requesterIds = items.Select(i => i.RequestedBy).Distinct().ToList();

        var reports = await _db.CustomerReports.AsNoTracking()
            .Where(r => reportIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, ct);

        var users = await _db.Users.AsNoTracking()
            .Where(u => requesterIds.Contains(u.UserId))
            .ToDictionaryAsync(u => u.UserId, u => u.FullName, ct);

        var customerIds = reports.Values.Select(r => r.CustomerId).Distinct().ToList();
        var customers = await _db.Users.AsNoTracking()
            .Where(u => customerIds.Contains(u.UserId))
            .ToDictionaryAsync(u => u.UserId, u => u.FullName, ct);

        return items.Select(i =>
        {
            reports.TryGetValue(i.ReportId, out var report);
            customers.TryGetValue(report?.CustomerId ?? Guid.Empty, out var customerName);
            users.TryGetValue(i.RequestedBy, out var requesterName);
            return new ReportChangeRequestListItemDto(
                i.Id,
                i.ReportId,
                i.RequestType,
                i.Status,
                i.Reason,
                i.RequestedOn,
                requesterName ?? "—",
                customerName,
                report?.MemberId,
                report?.OriginalFileName,
                report?.UploadDate);
        }).ToList();
    }

    private async Task<ReportChangeRequestDetailDto> MapDetailAsync(ReportChangeRequest request, CancellationToken ct)
    {
        var report = await _reports.GetByIdAsync(request.ReportId, ct);
        ReportSummaryDto? summary = null;

        if (report is not null)
        {
            var customer = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == report.CustomerId, ct);
            var planName = await (
                from up in _db.UserPlans.AsNoTracking()
                join p in _db.Plans.AsNoTracking() on up.PlanId equals p.PlanId
                where up.UserPlanId == report.SubscriptionId
                select p.Name
            ).FirstOrDefaultAsync(ct);

            summary = new ReportSummaryDto(
                report.Id,
                customer?.FullName ?? "—",
                report.MemberId,
                customer?.Email ?? "—",
                report.OriginalFileName,
                report.FileSize,
                report.UploadDate,
                report.DownloadCount,
                report.LastDownloadDate,
                planName,
                report.ReportType);
        }

        var actorIds = new[] { request.RequestedBy, request.ApprovedBy, request.RejectedBy }
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var actors = await _db.Users.AsNoTracking()
            .Where(u => actorIds.Contains(u.UserId))
            .ToDictionaryAsync(u => u.UserId, u => u.FullName, ct);

        return new ReportChangeRequestDetailDto(
            request.Id,
            request.ReportId,
            request.RequestType,
            request.Status,
            request.Reason,
            request.Remarks,
            request.RequestedOn,
            request.RequestedBy,
            actors.GetValueOrDefault(request.RequestedBy, "—"),
            request.ApprovedOn,
            request.ApprovedBy.HasValue ? actors.GetValueOrDefault(request.ApprovedBy.Value) : null,
            request.RejectedOn,
            request.RejectedBy.HasValue ? actors.GetValueOrDefault(request.RejectedBy.Value) : null,
            request.PreviousReportPath,
            request.NewReportPath,
            request.PreviousValues,
            request.NewValues,
            request.PendingOriginalFileName,
            request.PendingFileSize,
            summary);
    }

    private async Task<(bool Ok, string? Error, StoredFile? File)> StorePendingFileAsync(
        Guid reportId,
        IFormFile file,
        CancellationToken ct)
    {
        var validation = ValidateZipFile(file);
        if (!validation.Ok)
            return (false, validation.Error, null);

        var uniqueName = $"{Guid.NewGuid():N}.zip";
        var relativeDir = Path.Combine(_settings.UploadRoot, "pending", reportId.ToString());
        return await SaveFileAsync(relativeDir, uniqueName, file, ct);
    }

    private async Task<(bool Ok, string? Error, StoredFile? File)> StoreActiveFileAsync(
        Guid customerId,
        IFormFile file,
        CancellationToken ct)
    {
        var validation = ValidateZipFile(file);
        if (!validation.Ok)
            return (false, validation.Error, null);

        var dateFolder = DateTime.Now.ToString("yyyyMMdd");
        var uniqueName = $"{Guid.NewGuid():N}.zip";
        var relativeDir = Path.Combine(_settings.UploadRoot, customerId.ToString(), dateFolder);
        return await SaveFileAsync(relativeDir, uniqueName, file, ct);
    }

    private (bool Ok, string? Error) ValidateZipFile(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".zip")
            return (false, "Only ZIP files are allowed.");

        var maxBytes = _settings.MaxUploadSizeMb * 1024L * 1024L;
        if (file.Length > maxBytes)
            return (false, $"File exceeds maximum size of {_settings.MaxUploadSizeMb} MB.");

        return (true, null);
    }

    private async Task<(bool Ok, string? Error, StoredFile? File)> SaveFileAsync(
        string relativeDir,
        string uniqueName,
        IFormFile file,
        CancellationToken ct)
    {
        var wwwroot = Path.Combine(_env.ContentRootPath, "wwwroot");
        var absoluteDir = Path.Combine(wwwroot, relativeDir);
        Directory.CreateDirectory(absoluteDir);

        var absolutePath = Path.Combine(absoluteDir, uniqueName);
        var relativePath = Path.Combine(relativeDir, uniqueName).Replace('\\', '/');

        await using (var stream = new FileStream(absolutePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(stream, ct);
        }

        return (true, null, new StoredFile(
            relativePath,
            uniqueName,
            Path.GetFileName(file.FileName),
            file.Length));
    }

    private void TryDeletePhysicalFile(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;
        try
        {
            var wwwroot = Path.Combine(_env.ContentRootPath, "wwwroot");
            var physical = Path.Combine(wwwroot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(physical))
                File.Delete(physical);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete file at {Path}.", relativePath);
        }
    }

    private async Task TryNotifyReportReadyAsync(
        CustomerReport report,
        Guid actorUserId,
        string? ipAddress,
        CancellationToken ct)
    {
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == report.CustomerId && !u.IsDeleted, ct);
        if (user is null)
            return;

        try
        {
            await _email.SendReportReadyEmailAsync(user, report, ct);
            await _audit.LogAsync(
                ReportAuditService.ActionEmailSent,
                actorUserId,
                report.Id,
                report.CustomerId,
                ipAddress,
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Report ready email failed for report {ReportId}.", report.Id);
        }
    }

    private static string SerializeValues(object value) =>
        JsonSerializer.Serialize(value, JsonOpts);

    private sealed record StoredFile(
        string RelativePath,
        string FileName,
        string OriginalFileName,
        long FileSize);
}
