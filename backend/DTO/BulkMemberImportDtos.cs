namespace RB_Website_API.DTO;

public sealed record CreateImportedMemberRequest(
    string FullName,
    string? Email,
    string? Phone,
    DateTime? CreatedAt);

public sealed record ImportedMemberCreateResponse(
    bool Success,
    string? Message,
    Guid? UserId = null,
    string? MemberId = null);

public sealed record BulkImportRowRequest(
    int RowNumber,
    string? FullName,
    string? Email,
    string? Phone,
    DateTime? CreatedAt);

public sealed record BulkMemberImportBatchRequest(
    IReadOnlyList<BulkImportRowRequest> Rows,
    string? ImportId = null,
    int BatchIndex = 0,
    int TotalBatches = 1,
    int TotalRecords = 0,
    bool SendWelcomeEmails = false);

public sealed record BulkImportEmailRowRequest(
    int RowNumber,
    string CustomerName,
    string Email,
    string MemberId);

public sealed record BulkImportEmailBatchRequest(
    IReadOnlyList<BulkImportEmailRowRequest> Rows,
    string? ImportId = null,
    int BatchIndex = 0,
    int TotalBatches = 1);

public sealed record BulkImportEmailLookupRowRequest(
    int RowNumber,
    string? CustomerName,
    string? Email,
    string? Phone);

public sealed record BulkImportEmailLookupBatchRequest(
    IReadOnlyList<BulkImportEmailLookupRowRequest> Rows,
    string? ImportId = null,
    int BatchIndex = 0,
    int TotalBatches = 1);

public sealed record BulkImportRowResult(
    int RowNumber,
    string? CustomerName,
    string? Email,
    string? Mobile,
    string Status,
    string? Reason,
    string? MemberId,
    bool EmailSent);

public sealed record BulkImportBatchSummary(
    int Imported,
    int Skipped,
    int EmailsSent,
    int EmailFailed,
    int WithoutEmail,
    int WithoutMobile,
    int DuplicateMembers,
    int InvalidRows)
{
    public static BulkImportBatchSummary Empty => new(0, 0, 0, 0, 0, 0, 0, 0);

    public BulkImportBatchSummary Add(BulkImportBatchSummary other) =>
        new(
            Imported + other.Imported,
            Skipped + other.Skipped,
            EmailsSent + other.EmailsSent,
            EmailFailed + other.EmailFailed,
            WithoutEmail + other.WithoutEmail,
            WithoutMobile + other.WithoutMobile,
            DuplicateMembers + other.DuplicateMembers,
            InvalidRows + other.InvalidRows);
}

public sealed record BulkMemberImportBatchResponse(
    bool Success,
    string? Message,
    IReadOnlyList<BulkImportRowResult>? Results = null,
    BulkImportBatchSummary? Summary = null);
