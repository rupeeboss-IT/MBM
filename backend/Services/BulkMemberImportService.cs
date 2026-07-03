using Microsoft.EntityFrameworkCore;
using RB_Website_API.Auth;
using RB_Website_API.Data;
using RB_Website_API.DTO;

namespace RB_Website_API.Services;

public interface IBulkMemberImportService
{
    Task<BulkMemberImportBatchResponse> ProcessBatchAsync(
        Guid actorId,
        string actorRole,
        BulkMemberImportBatchRequest req,
        CancellationToken ct);

    Task<BulkMemberImportBatchResponse> SendWelcomeEmailsBatchAsync(
        Guid actorId,
        string actorRole,
        BulkImportEmailBatchRequest req,
        CancellationToken ct);

    Task<BulkMemberImportBatchResponse> SendWelcomeEmailsLookupBatchAsync(
        Guid actorId,
        string actorRole,
        BulkImportEmailLookupBatchRequest req,
        CancellationToken ct);
}

public sealed class BulkMemberImportService : IBulkMemberImportService
{
    private readonly IUserManagementService _users;
    private readonly BulkImportWelcomeEmailService _welcomeEmail;
    private readonly AppDbContext _db;
    private readonly ILogger<BulkMemberImportService> _logger;

    public BulkMemberImportService(
        IUserManagementService users,
        BulkImportWelcomeEmailService welcomeEmail,
        AppDbContext db,
        ILogger<BulkMemberImportService> logger)
    {
        _users = users;
        _welcomeEmail = welcomeEmail;
        _db = db;
        _logger = logger;
    }

    public async Task<BulkMemberImportBatchResponse> ProcessBatchAsync(
        Guid actorId,
        string actorRole,
        BulkMemberImportBatchRequest req,
        CancellationToken ct)
    {
        if (!UserRoles.IsSuperAdmin(actorRole))
            return new BulkMemberImportBatchResponse(false, "Only Super Admin can import members.");

        if (req.Rows is null || req.Rows.Count == 0)
            return new BulkMemberImportBatchResponse(false, "No rows to process.");

        try
        {
            return await ProcessBatchCoreAsync(actorId, actorRole, req, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Bulk member import failed. ImportId={ImportId}, BatchIndex={BatchIndex}",
                req.ImportId ?? "n/a",
                req.BatchIndex);
            return new BulkMemberImportBatchResponse(false, "Import failed. Please try again.");
        }
    }

    private async Task<BulkMemberImportBatchResponse> ProcessBatchCoreAsync(
        Guid actorId,
        string actorRole,
        BulkMemberImportBatchRequest req,
        CancellationToken ct)
    {
        if (req.BatchIndex == 0)
        {
            _logger.LogInformation(
                "Bulk member import started. ImportId={ImportId}, ActorId={ActorId}, TotalRecords={TotalRecords}, TotalBatches={TotalBatches}",
                req.ImportId ?? "n/a",
                actorId,
                req.TotalRecords > 0 ? req.TotalRecords : req.Rows.Count,
                req.TotalBatches);
        }

        var results = new List<BulkImportRowResult>(req.Rows.Count);
        var summary = BulkImportBatchSummary.Empty;

        foreach (var row in req.Rows)
        {
            ct.ThrowIfCancellationRequested();
            var result = await ProcessRowAsync(actorId, actorRole, row, req.SendWelcomeEmails, ct);
            results.Add(result);
            summary = summary.Add(SummarizeRow(result));
        }

        var isLastBatch = req.TotalBatches > 0 && req.BatchIndex >= req.TotalBatches - 1;
        if (isLastBatch)
        {
            _logger.LogInformation(
                "Bulk member import completed. ImportId={ImportId}, BatchImported={Imported}, BatchSkipped={Skipped}, BatchEmailSuccess={EmailSuccess}, BatchEmailFailed={EmailFailed}",
                req.ImportId ?? "n/a",
                summary.Imported,
                summary.Skipped,
                summary.EmailsSent,
                summary.EmailFailed);
        }
        else
        {
            _logger.LogInformation(
                "Bulk member import batch processed. ImportId={ImportId}, BatchIndex={BatchIndex}, Imported={Imported}, Skipped={Skipped}",
                req.ImportId ?? "n/a",
                req.BatchIndex,
                summary.Imported,
                summary.Skipped);
        }

        return new BulkMemberImportBatchResponse(true, "Batch processed.", results, summary);
    }

    public async Task<BulkMemberImportBatchResponse> SendWelcomeEmailsBatchAsync(
        Guid actorId,
        string actorRole,
        BulkImportEmailBatchRequest req,
        CancellationToken ct)
    {
        if (!UserRoles.IsSuperAdmin(actorRole))
            return new BulkMemberImportBatchResponse(false, "Only Super Admin can import members.");

        if (req.Rows is null || req.Rows.Count == 0)
            return new BulkMemberImportBatchResponse(false, "No rows to process.");

        try
        {
            var results = new List<BulkImportRowResult>(req.Rows.Count);
            var summary = BulkImportBatchSummary.Empty;

            foreach (var row in req.Rows)
            {
                ct.ThrowIfCancellationRequested();
                var email = UserManagementService.NormalizeImportEmail(row.Email);
                if (email is null || string.IsNullOrWhiteSpace(row.MemberId))
                {
                    var skipped = Skipped(row.RowNumber, row.CustomerName, row.Email, "", "Invalid Email");
                    results.Add(skipped);
                    summary = summary.Add(SummarizeRow(skipped));
                    continue;
                }

                var emailSent = await _welcomeEmail.TrySendAsync(email, row.CustomerName, row.MemberId, CancellationToken.None);
                var result = new BulkImportRowResult(
                    row.RowNumber,
                    row.CustomerName,
                    row.Email,
                    "",
                    "Imported",
                    null,
                    row.MemberId,
                    emailSent);
                results.Add(result);
                summary = summary.Add(SummarizeRow(result));
            }

            _logger.LogInformation(
                "Bulk member import email batch processed. ImportId={ImportId}, BatchIndex={BatchIndex}, EmailsSent={EmailsSent}, EmailFailed={EmailFailed}",
                req.ImportId ?? "n/a",
                req.BatchIndex,
                summary.EmailsSent,
                summary.EmailFailed);

            return new BulkMemberImportBatchResponse(true, "Email batch processed.", results, summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Bulk member import email batch failed. ImportId={ImportId}, BatchIndex={BatchIndex}",
                req.ImportId ?? "n/a",
                req.BatchIndex);
            return new BulkMemberImportBatchResponse(false, "Email sending failed. Please try again.");
        }
    }

    public async Task<BulkMemberImportBatchResponse> SendWelcomeEmailsLookupBatchAsync(
        Guid actorId,
        string actorRole,
        BulkImportEmailLookupBatchRequest req,
        CancellationToken ct)
    {
        if (!UserRoles.IsSuperAdmin(actorRole))
            return new BulkMemberImportBatchResponse(false, "Only Super Admin can import members.");

        if (req.Rows is null || req.Rows.Count == 0)
            return new BulkMemberImportBatchResponse(false, "No rows to process.");

        try
        {
            var results = new List<BulkImportRowResult>(req.Rows.Count);
            var summary = BulkImportBatchSummary.Empty;

            foreach (var row in req.Rows)
            {
                ct.ThrowIfCancellationRequested();
                var displayName = (row.CustomerName ?? "").Trim();
                var rawEmail = (row.Email ?? "").Trim();
                var rawPhone = (row.Phone ?? "").Trim();
                var email = UserManagementService.NormalizeImportEmail(rawEmail);
                var phone = UserManagementService.NormalizeImportPhone(rawPhone);

                if (email is null && phone is null)
                {
                    var skipped = Skipped(row.RowNumber, displayName, rawEmail, rawPhone, "Email and Mobile both missing");
                    results.Add(skipped);
                    summary = summary.Add(SummarizeRow(skipped));
                    continue;
                }

                var user = await FindImportedMemberAsync(email, phone, ct);
                if (user is null)
                {
                    var skipped = Skipped(row.RowNumber, displayName, rawEmail, rawPhone, "Member not found");
                    results.Add(skipped);
                    summary = summary.Add(SummarizeRow(skipped));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(user.MemberId))
                {
                    var skipped = Skipped(row.RowNumber, displayName, rawEmail, rawPhone, "Member ID not assigned");
                    results.Add(skipped);
                    summary = summary.Add(SummarizeRow(skipped));
                    continue;
                }

                var sendTo = email ?? user.Email;
                if (string.IsNullOrWhiteSpace(sendTo))
                {
                    var skipped = Skipped(row.RowNumber, displayName, rawEmail, rawPhone, "No email available");
                    results.Add(skipped);
                    summary = summary.Add(SummarizeRow(skipped));
                    continue;
                }

                var name = string.IsNullOrWhiteSpace(displayName) ? user.FullName : displayName;
                var emailSent = await _welcomeEmail.TrySendAsync(sendTo, name, user.MemberId, CancellationToken.None);
                var result = new BulkImportRowResult(
                    row.RowNumber,
                    name,
                    sendTo,
                    rawPhone,
                    "Imported",
                    null,
                    user.MemberId,
                    emailSent);
                results.Add(result);
                summary = summary.Add(SummarizeRow(result));
            }

            _logger.LogInformation(
                "Bulk member import lookup email batch processed. ImportId={ImportId}, BatchIndex={BatchIndex}, EmailsSent={EmailsSent}, EmailFailed={EmailFailed}",
                req.ImportId ?? "n/a",
                req.BatchIndex,
                summary.EmailsSent,
                summary.EmailFailed);

            return new BulkMemberImportBatchResponse(true, "Email batch processed.", results, summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Bulk member import lookup email batch failed. ImportId={ImportId}, BatchIndex={BatchIndex}",
                req.ImportId ?? "n/a",
                req.BatchIndex);
            return new BulkMemberImportBatchResponse(false, "Email sending failed. Please try again.");
        }
    }

    private async Task<Models.User?> FindImportedMemberAsync(string? email, string? phone, CancellationToken ct)
    {
        if (email is not null)
        {
            var byEmail = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    !u.IsDeleted
                    && (u.Role ?? "").ToLower() == UserRoles.Member
                    && u.Email == email, ct);
            if (byEmail is not null) return byEmail;
        }

        if (phone is not null)
        {
            return await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    !u.IsDeleted
                    && (u.Role ?? "").ToLower() == UserRoles.Member
                    && u.Phone == phone, ct);
        }

        return null;
    }

    private async Task<BulkImportRowResult> ProcessRowAsync(
        Guid actorId,
        string actorRole,
        BulkImportRowRequest row,
        bool sendWelcomeEmails,
        CancellationToken ct)
    {
        var displayName = (row.FullName ?? "").Trim();
        var rawEmail = (row.Email ?? "").Trim();
        var rawPhone = (row.Phone ?? "").Trim();

        if (string.IsNullOrWhiteSpace(displayName))
        {
            return Skipped(row.RowNumber, displayName, rawEmail, rawPhone, "Name is required");
        }

        var hasEmail = !string.IsNullOrWhiteSpace(rawEmail);
        var hasPhone = !string.IsNullOrWhiteSpace(rawPhone);
        if (!hasEmail && !hasPhone)
        {
            return Skipped(row.RowNumber, displayName, rawEmail, rawPhone, "Email and Mobile both missing");
        }

        var email = UserManagementService.NormalizeImportEmail(rawEmail);
        var phone = UserManagementService.NormalizeImportPhone(rawPhone);
        if (email is null && phone is null)
        {
            var reason = hasEmail && hasPhone
                ? "Invalid Email and Mobile"
                : hasEmail
                    ? "Invalid Email"
                    : "Invalid Mobile";
            return Skipped(row.RowNumber, displayName, rawEmail, rawPhone, reason);
        }

        var create = await _users.CreateImportedMemberAsync(
            actorId,
            actorRole,
            new CreateImportedMemberRequest(displayName, email, phone, row.CreatedAt),
            ct);

        if (!create.Success)
        {
            var reason = create.Message ?? "Import failed";
            if (reason.Contains("Duplicate", StringComparison.OrdinalIgnoreCase))
                return Skipped(row.RowNumber, displayName, rawEmail, rawPhone, "Duplicate Member");

            return Skipped(row.RowNumber, displayName, rawEmail, rawPhone, reason);
        }

        var emailSent = false;
        if (sendWelcomeEmails && email is not null && !string.IsNullOrWhiteSpace(create.MemberId))
        {
            emailSent = await _welcomeEmail.TrySendAsync(email, displayName, create.MemberId, ct);
        }

        return new BulkImportRowResult(
            row.RowNumber,
            displayName,
            rawEmail,
            rawPhone,
            "Imported",
            null,
            create.MemberId,
            emailSent);
    }

    private static BulkImportRowResult Skipped(
        int rowNumber,
        string name,
        string email,
        string phone,
        string reason) =>
        new(rowNumber, name, email, phone, "Skipped", reason, null, false);

    private static BulkImportBatchSummary SummarizeRow(BulkImportRowResult result)
    {
        if (result.Status == "Imported")
        {
            var withoutEmail = string.IsNullOrWhiteSpace(result.Email) ? 1 : 0;
            var withoutMobile = string.IsNullOrWhiteSpace(result.Mobile) ? 1 : 0;
            var emailsSent = !string.IsNullOrWhiteSpace(result.Email) && result.EmailSent ? 1 : 0;
            var emailFailed = !string.IsNullOrWhiteSpace(result.Email) && !result.EmailSent ? 1 : 0;
            return new BulkImportBatchSummary(1, 0, emailsSent, emailFailed, withoutEmail, withoutMobile, 0, 0);
        }

        var reason = result.Reason ?? "";
        var duplicate = reason.Contains("Duplicate", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        var invalid = reason is "Name is required"
            or "Email and Mobile both missing"
            or "Invalid Email"
            or "Invalid Mobile"
            or "Invalid Email and Mobile"
            ? 1
            : 0;

        return new BulkImportBatchSummary(0, 1, 0, 0, 0, 0, duplicate, invalid);
    }
}
