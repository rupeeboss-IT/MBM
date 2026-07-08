using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RB_Website_API.Auth;
using RB_Website_API.Data;
using RB_Website_API.DTO;
using RB_Website_API.Models;
using RB_Website_API.Services.IRepository;

namespace RB_Website_API.Services;

public sealed class SdrReportService : ISdrReportService
{
    private readonly AppDbContext _db;
    private readonly ISdrReportRepository _reports;
    private readonly ISaarthiApiClient _saarthi;
    private readonly IWebHostEnvironment _env;
    private readonly SdrReportSettings _settings;
    private readonly IMemberIdGeneratorService _memberIds;
    private readonly IReportEmailService _email;
    private readonly ILogger<SdrReportService> _logger;

    public SdrReportService(
        AppDbContext db,
        ISdrReportRepository reports,
        ISaarthiApiClient saarthi,
        IWebHostEnvironment env,
        IOptions<SdrReportSettings> settings,
        IMemberIdGeneratorService memberIds,
        IReportEmailService email,
        ILogger<SdrReportService> logger)
    {
        _db = db;
        _reports = reports;
        _saarthi = saarthi;
        _env = env;
        _settings = settings.Value;
        _memberIds = memberIds;
        _email = email;
        _logger = logger;
    }

    public Task<CustomerReport?> HasValidGeneratedReportAsync(Guid userId, CancellationToken ct) =>
        _reports.GetLatestValidSdrReportAsync(userId, DateTime.Now, ct);

    public async Task<SdrGenerationResult> GenerateAfterOneTimePaymentAsync(
        Guid userId,
        SchemeDiscoveryRequest request,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "[SDR Service] START UserId={UserId} RequestId={RequestId} PaymentId={PaymentId} Udyam={UdyamMasked} Entitlement={Entitlement}",
            userId,
            request.Id,
            request.PaymentId,
            MaskUdyam(request.UdyamNumber),
            request.EntitlementType);

        if (request.CustomerReportId is Guid linkedId)
        {
            var linked = await _reports.GetByIdForCustomerAsync(linkedId, userId, ct);
            if (linked is not null)
            {
                _logger.LogInformation(
                    "[SDR Service] SKIP idempotent — report already linked RequestId={RequestId} ReportId={ReportId}",
                    request.Id,
                    linked.Id);
                return BuildDuplicateResult(linked, request);
            }
        }

        var existing = await HasValidGeneratedReportAsync(userId, ct);
        if (existing is not null)
        {
            _logger.LogInformation(
                "[SDR Service] SKIP duplicate valid report UserId={UserId} ExistingReportId={ReportId} Expiry={ExpiryDate}",
                userId,
                existing.Id,
                existing.ExpiryDate);

            await LinkRequestToReportAsync(request, existing, ct);
            return BuildDuplicateResult(existing, request);
        }

        _logger.LogInformation("[SDR Service] DB update RequestId={RequestId} Status=Processing", request.Id);
        request.Status = SchemeDiscoveryCatalog.StatusProcessing;
        request.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "[SDR Service] Calling Saarthi pipeline RequestId={RequestId} CorrelationId={CorrelationId}",
            request.Id,
            request.Id);

        var apiResult = await _saarthi.GenerateSdrAsync(
            request.UdyamNumber,
            request.Id.ToString(),
            ct);

        if (!apiResult.Success || apiResult.PdfBytes is null)
        {
            _logger.LogError(
                "[SDR Service] API pipeline FAILED UserId={UserId} RequestId={RequestId} Udyam={UdyamMasked} Error={Error} ExternalReference={ExternalReference}",
                userId,
                request.Id,
                MaskUdyam(request.UdyamNumber),
                apiResult.ErrorMessage,
                SaarthiApiLogHelper.Truncate(apiResult.ExternalReference, 500));

            _logger.LogInformation("[SDR Service] DB update RequestId={RequestId} Status=Failed", request.Id);
            request.Status = SchemeDiscoveryCatalog.StatusFailed;
            request.ErrorMessage = apiResult.ErrorMessage ?? "SDR API generation failed.";
            request.ExternalReference = apiResult.ExternalReference;
            request.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync(ct);

            return new SdrGenerationResult(
                true,
                SdrReportCatalog.OutcomeFailed,
                "We received your payment successfully, but report generation is currently unavailable. Our team has been notified and will assist you shortly.",
                null,
                null,
                request.Status);
        }

        try
        {
            _logger.LogInformation(
                "[SDR Service] Saving PDF RequestId={RequestId} PdfBytes={PdfBytes}",
                request.Id,
                apiResult.PdfBytes?.Length ?? 0);

            var (relativePath, fileName, fileSize) = await SaveReportFileAsync(userId, apiResult.PdfBytes, ct);

            _logger.LogInformation(
                "[SDR Service] PDF saved RequestId={RequestId} FileName={FileName} RelativePath={RelativePath} FileSize={FileSize}",
                request.Id,
                fileName,
                relativePath,
                fileSize);

            var report = await SaveGeneratedReportRecordAsync(
                userId,
                request,
                relativePath,
                fileName,
                fileSize,
                userId,
                ct);

            _logger.LogInformation(
                "[SDR Service] DB CustomerReport created ReportId={ReportId} RequestId={RequestId}",
                report.Id,
                request.Id);

            request.Status = SchemeDiscoveryCatalog.StatusCompleted;
            request.CustomerReportId = report.Id;
            request.CompletedAt = DateTime.Now;
            request.ExternalReference = apiResult.ExternalReference;
            request.ErrorMessage = null;
            request.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "[SDR Service] COMPLETE UserId={UserId} RequestId={RequestId} ReportId={ReportId} Path={Path} Expiry={Expiry}",
                userId,
                request.Id,
                report.Id,
                relativePath,
                report.ExpiryDate);

            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId, ct);
            if (user is not null)
            {
                try
                {
                    _logger.LogInformation("[SDR Service] Sending report email ReportId={ReportId} Email={Email}", report.Id, user.Email);
                    await _email.SendReportReadyEmailAsync(user, report, ct);
                    _logger.LogInformation("[SDR Service] Report email sent ReportId={ReportId}", report.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[SDR Service] Report email FAILED ReportId={ReportId}", report.Id);
                }
            }

            return new SdrGenerationResult(
                true,
                SdrReportCatalog.OutcomeGenerated,
                "Your Government Scheme Discovery Report has been generated successfully.",
                report.Id,
                report.ExpiryDate,
                request.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SDR Service] Save FAILED RequestId={RequestId}", request.Id);

            request.Status = SchemeDiscoveryCatalog.StatusFailed;
            request.ErrorMessage = "Failed to save generated report.";
            request.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync(ct);

            return new SdrGenerationResult(
                true,
                SdrReportCatalog.OutcomeFailed,
                "We received your payment successfully, but report generation is currently unavailable. Our team has been notified and will assist you shortly.",
                null,
                null,
                request.Status);
        }
    }

    public async Task<SdrGenerationResult> GenerateForAdminAsync(
        Guid adminUserId,
        Guid customerId,
        string udyamNumber,
        CancellationToken ct)
    {
        var udyamErr = ValidateUdyam(udyamNumber);
        if (udyamErr is not null)
            return new SdrGenerationResult(false, SdrReportCatalog.OutcomeFailed, udyamErr, null, null, null);

        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == customerId && !u.IsDeleted, ct);
        if (user is null)
            return new SdrGenerationResult(false, SdrReportCatalog.OutcomeFailed, "Customer not found.", null, null, null);

        var role = (user.Role ?? "").Trim().ToLowerInvariant();
        if (role is "admin" or "superadmin")
            return new SdrGenerationResult(false, SdrReportCatalog.OutcomeFailed, "Invalid customer.", null, null, null);

        var normalizedUdyam = udyamNumber.Trim().ToUpperInvariant();
        var now = DateTime.Now;

        try
        {
        var request = new SchemeDiscoveryRequest
        {
            Id = Guid.NewGuid(),
            UserId = customerId,
            MemberId = _memberIds.GetDisplayMemberId(user),
            // Unique per admin run — UX_SchemeDiscoveryRequests_UserPlan is (UserId, UserPlanId).
            // Member one-time flows use the real plan id; admin must not reuse it.
            UserPlanId = Guid.NewGuid(),
            UdyamNumber = normalizedUdyam,
            PaymentId = null,
            EntitlementType = SchemeDiscoveryCatalog.EntitlementAdmin,
            Status = SchemeDiscoveryCatalog.StatusProcessing,
            RequestedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.SchemeDiscoveryRequests.Add(request);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "[SDR Service] Admin generation START AdminId={AdminId} UserId={UserId} RequestId={RequestId} Udyam={UdyamMasked}",
            adminUserId,
            customerId,
            request.Id,
            MaskUdyam(normalizedUdyam));

        var apiResult = await _saarthi.GenerateSdrAsync(
            normalizedUdyam,
            request.Id.ToString(),
            ct);

        if (!apiResult.Success || apiResult.PdfBytes is null)
        {
            _logger.LogError(
                "[SDR Service] Admin generation API FAILED AdminId={AdminId} UserId={UserId} RequestId={RequestId} Error={Error}",
                adminUserId,
                customerId,
                request.Id,
                apiResult.ErrorMessage);

            request.Status = SchemeDiscoveryCatalog.StatusFailed;
            request.ErrorMessage = apiResult.ErrorMessage ?? "SDR API generation failed.";
            request.ExternalReference = apiResult.ExternalReference;
            request.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync(ct);

            return new SdrGenerationResult(
                false,
                SdrReportCatalog.OutcomeFailed,
                apiResult.ErrorMessage ?? "Report generation failed. Please try again.",
                null,
                null,
                request.Status);
        }

        try
        {
            var (relativePath, fileName, fileSize) = await SaveReportFileAsync(customerId, apiResult.PdfBytes, ct);
            var report = await SaveGeneratedReportRecordAsync(
                customerId,
                request,
                relativePath,
                fileName,
                fileSize,
                adminUserId,
                ct);

            request.Status = SchemeDiscoveryCatalog.StatusCompleted;
            request.CustomerReportId = report.Id;
            request.CompletedAt = DateTime.Now;
            request.ExternalReference = apiResult.ExternalReference;
            request.ErrorMessage = null;
            request.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "[SDR Service] Admin generation COMPLETE AdminId={AdminId} UserId={UserId} RequestId={RequestId} ReportId={ReportId}",
                adminUserId,
                customerId,
                request.Id,
                report.Id);

            try
            {
                _logger.LogInformation("[SDR Service] Sending report email ReportId={ReportId} Email={Email}", report.Id, user.Email);
                await _email.SendReportReadyEmailAsync(user, report, ct);
                _logger.LogInformation("[SDR Service] Report email sent ReportId={ReportId}", report.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SDR Service] Report email FAILED ReportId={ReportId}", report.Id);
            }

            return new SdrGenerationResult(
                true,
                SdrReportCatalog.OutcomeGenerated,
                "Government Scheme Discovery Report generated successfully.",
                report.Id,
                report.ExpiryDate,
                request.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SDR Service] Admin generation save FAILED RequestId={RequestId}", request.Id);

            request.Status = SchemeDiscoveryCatalog.StatusFailed;
            request.ErrorMessage = "Failed to save generated report.";
            request.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync(ct);

            return new SdrGenerationResult(
                false,
                SdrReportCatalog.OutcomeFailed,
                "Failed to save generated report.",
                null,
                null,
                request.Status);
        }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "[SDR Service] Admin generation UNHANDLED AdminId={AdminId} UserId={UserId}",
                adminUserId,
                customerId);

            return new SdrGenerationResult(
                false,
                SdrReportCatalog.OutcomeFailed,
                "Report generation failed. Please try again.",
                null,
                null,
                null);
        }
    }

    private async Task LinkRequestToReportAsync(
        SchemeDiscoveryRequest request,
        CustomerReport report,
        CancellationToken ct)
    {
        request.Status = SchemeDiscoveryCatalog.StatusCompleted;
        request.CustomerReportId = report.Id;
        request.CompletedAt ??= DateTime.Now;
        request.UpdatedAt = DateTime.Now;
        request.ErrorMessage = null;
        await _db.SaveChangesAsync(ct);
    }

    private static SdrGenerationResult BuildDuplicateResult(CustomerReport report, SchemeDiscoveryRequest request)
    {
        return new SdrGenerationResult(
            true,
            SdrReportCatalog.OutcomeDuplicate,
            $"Your Government Scheme Discovery Report has already been generated and remains valid for one year from the date of generation. Please contact support if you need assistance.",
            report.Id,
            report.ExpiryDate,
            request.Status);
    }

    private async Task<(string RelativePath, string FileName, long FileSize)> SaveReportFileAsync(
        Guid userId,
        byte[] pdfBytes,
        CancellationToken ct)
    {
        var root = (_settings.StorageRoot ?? "reports/sdr").Trim().Trim('/').Replace('\\', '/');
        var wwwroot = Path.Combine(_env.ContentRootPath, "wwwroot");
        var absoluteDir = Path.Combine(wwwroot, root.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(absoluteDir);

        var stamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var fileName = $"SDR_{userId:N}_{stamp}.pdf";
        var absolutePath = Path.Combine(absoluteDir, fileName);

        var attempt = 0;
        while (File.Exists(absolutePath))
        {
            attempt++;
            fileName = $"SDR_{userId:N}_{stamp}_{attempt}.pdf";
            absolutePath = Path.Combine(absoluteDir, fileName);
        }

        await File.WriteAllBytesAsync(absolutePath, pdfBytes, ct);

        if (!File.Exists(absolutePath) || new FileInfo(absolutePath).Length == 0)
            throw new InvalidOperationException("PDF file was not saved successfully.");

        var relativePath = Path.Combine(root, fileName).Replace('\\', '/');
        return (relativePath, fileName, pdfBytes.LongLength);
    }

    private async Task<CustomerReport> SaveGeneratedReportRecordAsync(
        Guid userId,
        SchemeDiscoveryRequest request,
        string relativePath,
        string fileName,
        long fileSize,
        Guid uploadedBy,
        CancellationToken ct)
    {
        var user = await _db.Users.AsNoTracking().FirstAsync(u => u.UserId == userId, ct);
        var now = DateTime.Now;
        var expiry = now.Date.AddDays(Math.Max(1, _settings.ValidityDays));

        var subscriptionId = string.Equals(request.EntitlementType, SchemeDiscoveryCatalog.EntitlementAdmin, StringComparison.OrdinalIgnoreCase)
            ? Guid.Empty
            : request.UserPlanId == Guid.Empty
                ? await ResolveSubscriptionIdAsync(userId, ct)
                : request.UserPlanId;

        var report = new CustomerReport
        {
            Id = Guid.NewGuid(),
            CustomerId = userId,
            MemberId = _memberIds.GetDisplayMemberId(user),
            ReportFileName = fileName,
            OriginalFileName = "Government Scheme Discovery Report.pdf",
            FilePath = relativePath,
            FileSize = fileSize,
            UploadDate = now,
            UploadedBy = uploadedBy,
            SubscriptionId = subscriptionId,
            IsActive = true,
            DownloadCount = 0,
            ReportType = SdrReportCatalog.ReportType,
            SchemeDiscoveryRequestId = request.Id,
            ExpiryDate = expiry,
        };

        await _reports.AddReportAsync(report, ct);
        await _reports.SaveChangesAsync(ct);
        return report;
    }

    private static string MaskUdyam(string udyam)
    {
        if (string.IsNullOrWhiteSpace(udyam) || udyam.Length < 8) return "****";
        return $"{udyam[..7]}****";
    }

    private async Task<Guid> ResolveSubscriptionIdAsync(Guid userId, CancellationToken ct)
    {
        var now = DateTime.Now;
        var plan = await _db.UserPlans.AsNoTracking()
            .Where(up => up.UserId == userId
                         && up.Status == "Active"
                         && up.PlanCode == SchemeDiscoveryCatalog.OneTimePlanCode
                         && (up.ActiveTo == null || up.ActiveTo > now))
            .OrderByDescending(up => up.ActiveFrom)
            .Select(up => up.UserPlanId)
            .FirstOrDefaultAsync(ct);

        return plan == Guid.Empty
            ? throw new InvalidOperationException("Active one-time subscription not found for report.")
            : plan;
    }

    private static string? ValidateUdyam(string? raw)
    {
        var u = (raw ?? "").Trim();
        if (string.IsNullOrEmpty(u)) return "Udyam Registration Number is required.";
        if (!System.Text.RegularExpressions.Regex.IsMatch(u, "^UDYAM-[A-Z]{2}-\\d{2}-\\d{7}$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            return "Enter a valid Udyam number (e.g. UDYAM-XX-00-0000000).";
        return null;
    }
}
